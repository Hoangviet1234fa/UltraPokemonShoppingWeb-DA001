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

            // 4. Sắp xếp
            // [ĐÃ FIX] Bổ sung "name_desc" và "newest" khớp với dropdown trên View
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
        // [ĐÃ FIX] Bổ sung "ImageUrl" vào [Bind] và bỏ qua lỗi required nếu để trống ảnh (dùng ảnh mặc định hoặc file upload)
        public async Task<IActionResult> Create([Bind("Name,ElementType,Power,Price,Stock,ImageUrl,Description")] Product product, IFormFile? imageFile, string? imageUrlInput)
        {
            ModelState.Remove("ImageUrl");
            product.ImageUrl ??= string.Empty;

            if (ModelState.IsValid)
            {
                // Ưu tiên lưu file ảnh upload từ máy tính
                if (imageFile != null && imageFile.Length > 0)
                {
                    product.ImageUrl = await SaveImageAsync(imageFile);
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

            // [ĐÃ FIX] Bỏ qua validation khi ImageUrl rỗng để cho phép dùng ảnh mặc định theo hệ
            ModelState.Remove("ImageUrl");
            product.ImageUrl ??= string.Empty;

            if (ModelState.IsValid)
            {
                try
                {
                    // Nếu người dùng upload ảnh mới -> lưu file mới
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        product.ImageUrl = await SaveImageAsync(imageFile);
                    }
                    // Hoặc nếu người dùng nhập URL mới
                    else if (!string.IsNullOrWhiteSpace(imageUrlInput))
                    {
                        product.ImageUrl = imageUrlInput.Trim();
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Đã cập nhật thẻ bài '{product.Name}'!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
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

        // HÀM HỖ TRỢ LƯU FILE ẢNH VÀO WWWROOT/IMAGES/CARDS
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "cards");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/cards/" + uniqueFileName;
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}