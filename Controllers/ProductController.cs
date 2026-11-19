using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

    
        // 1. DANH SÁCH THẺ BÀI (TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG)
        
        public async Task<IActionResult> Index(string? search, string? element, decimal? minPrice, decimal? maxPrice, string? sortOrder, int page = 1)
        {
            var query = _context.Products.AsQueryable();

            // 1. Tìm kiếm theo tên thẻ hoặc mô tả
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword)
                                      || (p.Description != null && p.Description.ToLower().Contains(keyword)));
            }

            // 2. Lọc theo Hệ (ElementType)
            if (!string.IsNullOrWhiteSpace(element))
            {
                query = query.Where(p => p.ElementType == element);
            }

            // 3. Lọc theo khoảng giá
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // 4. Sắp xếp (khớp đầy đủ với các tùy chọn trên dropdown View)
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "power_desc" => query.OrderByDescending(p => p.Power),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "newest" => query.OrderByDescending(p => p.Id),
                _ => query.OrderBy(p => p.Name) // Mặc định sắp xếp A -> Z
            };

            // 5. Phân trang với LINQ Skip / Take
            int pageSize = 8;
            if (page < 1) page = 1;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền lại các tham số để giữ trạng thái trên thanh lọc & phân trang
            ViewBag.Search = search;
            ViewBag.Element = element;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Lấy danh sách các hệ hiện có từ bảng Categories (fallback sang Distinct từ Products nếu Categories trống)
            var categories = await _context.Categories.Select(c => c.Name).ToListAsync();
            if (!categories.Any())
            {
                categories = await _context.Products
                    .Select(p => p.ElementType)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();
            }
            ViewBag.Elements = categories;

            return View(products);
        }

      
        // 2. CHI TIẾT THẺ BÀI
        
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Lấy thêm tối đa 4 thẻ bài cùng hệ để gợi ý bên dưới
            var relatedProducts = await _context.Products
                .Where(p => p.ElementType == product.ElementType && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

       
        // 3. THÊM THẺ BÀI MỚI (CHỈ ADMIN & STAFF)
        
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        // Bổ sung "ImageUrl" vào [Bind] và bỏ qua lỗi required nếu để trống ảnh (dùng ảnh mặc định hoặc file upload)
        public async Task<IActionResult> Create([Bind("Name,ElementType,Power,Price,Stock,ImageUrl,Description")] Product product, IFormFile? imageFile, string? imageUrlInput)
        {
            ModelState.Remove("ImageUrl");
            product.ImageUrl ??= string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    // Ưu tiên lưu file ảnh upload từ máy tính (có kiểm tra định dạng & dung lượng)
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var savedPath = await SaveImageAsync(imageFile);
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            product.ImageUrl = savedPath;
                        }
                    }
                    // Nếu không upload file thì lấy link URL người dùng nhập
                    else if (!string.IsNullOrWhiteSpace(imageUrlInput))
                    {
                        product.ImageUrl = imageUrlInput.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(product.ImageUrl))
                    {
                        product.ImageUrl = product.ImageUrl.Trim();
                    }

                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã thêm thẻ bài '{product.Name}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    // Hiển thị thông báo lỗi file ảnh (quá 5MB hoặc sai định dạng) ngay trên form
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

       
        // 4. CHỈNH SỬA THẺ BÀI (CHỈ ADMIN & STAFF)
        
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ElementType,Power,Price,Stock,ImageUrl,Description")] Product product, IFormFile? imageFile, string? imageUrlInput)
        {
            if (id != product.Id) return NotFound();

            // Bỏ qua validation khi ImageUrl rỗng để cho phép giữ ảnh cũ hoặc dùng ảnh mặc định theo hệ
            ModelState.Remove("ImageUrl");
            product.ImageUrl ??= string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu người dùng upload ảnh mới -> kiểm tra và lưu file mới
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var savedPath = await SaveImageAsync(imageFile);
                        if (!string.IsNullOrEmpty(savedPath))
                        {
                            product.ImageUrl = savedPath;
                        }
                    }
                    // Hoặc nếu người dùng nhập URL mới
                    else if (!string.IsNullOrWhiteSpace(imageUrlInput))
                    {
                        product.ImageUrl = imageUrlInput.Trim();
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã cập nhật thẻ bài '{product.Name}'!";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    // Hiển thị thông báo lỗi file ảnh ngay trên form chỉnh sửa
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        
        // 5. XÓA THẺ BÀI (CHỈ ADMIN)
    
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thẻ bài '{product.Name}' khỏi kho!";
            }

            return RedirectToAction(nameof(Index));
        }

        // HÀM HỖ TRỢ KIỂM TRA & LƯU FILE ẢNH VÀO WWWROOT/IMAGES/CARDS
  
        private async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // 1. Giới hạn dung lượng tối đa 5MB
            const long maxFileSize = 5 * 1024 * 1024;
            if (imageFile.Length > maxFileSize)
                throw new InvalidOperationException("Dung lượng ảnh không được vượt quá 5MB.");

            // 2. Chỉ cho phép các định dạng ảnh phổ biến
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                throw new InvalidOperationException("Chỉ chấp nhận tệp ảnh .jpg, .jpeg, .png, .webp hoặc .gif.");

            // 3. Kiểm tra MIME type
            if (!imageFile.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Tệp tải lên không đúng định dạng hình ảnh.");

            // Dùng đúng _webHostEnvironment đã inject ở Constructor
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "cards");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/cards/{fileName}";
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}