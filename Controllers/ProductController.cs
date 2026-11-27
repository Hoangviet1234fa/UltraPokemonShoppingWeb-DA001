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

            // 1. Tìm kiếm theo tên thẻ hoặc mô tả (truy vấn SQL)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword)
                                      || (p.Description != null && p.Description.ToLower().Contains(keyword)));
            }

            // 2. Lọc theo Hệ (khớp cả dữ liệu tiếng Việt lẫn tiếng Anh cũ trong DB)
            if (!string.IsNullOrWhiteSpace(element))
            {
                var elementVariants = GetElementVariants(element);
                query = query.Where(p => p.ElementType != null && elementVariants.Contains(p.ElementType));
            }

            // Tải danh sách đã lọc từ SQLite sang bộ nhớ để xử lý decimal (lọc giá & sắp xếp) không bị lỗi NotSupportedException
            var filteredList = await query.AsNoTracking().ToListAsync();

            // 3. Lọc theo khoảng giá (LINQ to Objects)
            if (minPrice.HasValue)
            {
                filteredList = filteredList.Where(p => p.Price >= minPrice.Value).ToList();
            }
            if (maxPrice.HasValue)
            {
                filteredList = filteredList.Where(p => p.Price <= maxPrice.Value).ToList();
            }

            // 4. Sắp xếp (khớp đầy đủ với các tùy chọn trên dropdown View)
            filteredList = sortOrder switch
            {
                "price_asc" => filteredList.OrderBy(p => p.Price).ToList(),
                "price_desc" => filteredList.OrderByDescending(p => p.Price).ToList(),
                "power_desc" => filteredList.OrderByDescending(p => p.Power).ToList(),
                "name_asc" => filteredList.OrderBy(p => p.Name).ToList(),
                "name_desc" => filteredList.OrderByDescending(p => p.Name).ToList(),
                "newest" => filteredList.OrderByDescending(p => p.Id).ToList(),
                _ => filteredList.OrderBy(p => p.Name).ToList() // Mặc định sắp xếp A -> Z
            };

            // 5. Phân trang với LINQ Skip / Take
            int pageSize = 8;
            if (page < 1) page = 1;
            int totalItems = filteredList.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var products = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền lại các tham số để giữ trạng thái trên thanh lọc & phân trang (chuẩn hóa tên hệ sang tiếng Việt)
            ViewBag.Search = search;
            ViewBag.Element = string.IsNullOrWhiteSpace(element) ? string.Empty : ToVietnameseElement(element);
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Chuẩn hóa danh sách các hệ sang tiếng Việt đồng bộ & loại bỏ nút trùng (Psychic / Tâm linh)
            var standardElements = new List<string> { "Lửa", "Nước", "Điện", "Đá", "Cỏ", "Tâm linh" };
            var dbCategories = await _context.Categories.Select(c => c.Name).ToListAsync();
            if (!dbCategories.Any())
            {
                dbCategories = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.ElementType))
                    .Select(p => p.ElementType)
                    .Distinct()
                    .ToListAsync();
            }

            // Gộp các hệ chuẩn tiếng Việt với các hệ mở rộng khác (nếu Admin có tạo thêm)
            foreach (var cat in dbCategories)
            {
                string vnName = ToVietnameseElement(cat);
                if (!string.IsNullOrWhiteSpace(vnName) && !standardElements.Contains(vnName, StringComparer.OrdinalIgnoreCase))
                {
                    standardElements.Add(vnName);
                }
            }
            ViewBag.Elements = standardElements;

            return View(products);
        }


        // 2. CHI TIẾT THẺ BÀI & ĐÁNH GIÁ SAU MUA

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Nạp kèm danh sách đánh giá (Reviews) của thẻ bài
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            // Sắp xếp bình luận mới nhất lên đầu và tính điểm sao trung bình
            var reviews = product.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Any()
                ? Math.Round(reviews.Average(r => r.Rating), 1)
                : 0.0;

            // Lấy thêm tối đa 4 thẻ bài cùng hệ (hỗ trợ cả tên hệ tiếng Việt & tiếng Anh) để gợi ý bên dưới
            var sameElementVariants = GetElementVariants(product.ElementType);
            var relatedProducts = await _context.Products
                .Where(p => p.ElementType != null && sameElementVariants.Contains(p.ElementType) && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // Xử lý lưu đánh giá sản phẩm sau khi đơn hàng đạt trạng thái Completed
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int orderId, int productId, int rating, string comment)
        {
            // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Vui lòng chọn số sao (từ 1 đến 5) và nhập nội dung nhận xét.";
                return RedirectToAction("MyOrders", "Cart");
            }

            if (comment.Trim().Length > 500)
            {
                TempData["Error"] = "Nội dung nhận xét tối đa 500 ký tự.";
                return RedirectToAction("MyOrders", "Cart");
            }

            var userEmail = User.Identity!.Name!;

            // 2. Xác minh đơn hàng tồn tại, thuộc về khách hàng đang đăng nhập và đã hoàn tất (Completed)
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserEmail == userEmail);

            if (order == null || order.Status != OrderStatus.Completed)
            {
                TempData["Error"] = "Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng đã giao thành công.";
                return RedirectToAction("MyOrders", "Cart");
            }

            // 3. Xác minh sản phẩm được đánh giá thực sự nằm trong đơn hàng này
            bool hasProductInOrder = order.OrderDetails.Any(od => od.ProductId == productId);
            if (!hasProductInOrder)
            {
                TempData["Error"] = "Sản phẩm này không thuộc đơn hàng bạn đã chọn.";
                return RedirectToAction("MyOrders", "Cart");
            }

            // 4. Chặn gửi đánh giá trùng lặp cho cùng 1 sản phẩm trong cùng 1 đơn hàng
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.OrderId == orderId && r.ProductId == productId);
            if (alreadyReviewed)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này cho đơn hàng trên rồi.";
                return RedirectToAction("MyOrders", "Cart");
            }

            // 5. Lưu đánh giá mới vào CSDL
            var review = new Review
            {
                OrderId = orderId,
                ProductId = productId,
                ReviewerName = !string.IsNullOrWhiteSpace(order.FullName) ? order.FullName : userEmail,
                ReviewerEmail = userEmail,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá cho sản phẩm!";
            return RedirectToAction(nameof(Details), new { id = productId });
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
        public async Task<IActionResult> Create([Bind("Name,ElementType,Power,Price,Stock,ImageUrl,Description")] Product product, IFormFile? imageFile, string? imageUrlInput)
        {
            ModelState.Remove("ImageUrl");
            product.ImageUrl ??= string.Empty;

            // [ĐÃ FIX] Ánh xạ ElementType về đúng giá trị Categories.Name trong DB để không bị lỗi khóa ngoại SQLite
            if (!string.IsNullOrWhiteSpace(product.ElementType))
            {
                product.ElementType = await ResolveCategoryNameAsync(product.ElementType);
            }

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

            // [ĐÃ FIX] Ánh xạ ElementType về đúng giá trị Categories.Name trong DB để không bị lỗi khóa ngoại SQLite
            if (!string.IsNullOrWhiteSpace(product.ElementType))
            {
                product.ElementType = await ResolveCategoryNameAsync(product.ElementType);
            }

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


        // HÀM HỖ TRỢ ĐỐI CHIẾU KHÓA NGOẠI CATEGORIES.NAME & ÁNH XẠ SONG NGỮ TRONG SQLITE

        private async Task<string> ResolveCategoryNameAsync(string? element)
        {
            if (string.IsNullOrWhiteSpace(element)) return string.Empty;

            var variants = GetElementVariants(element);
            var matchedCategory = await _context.Categories
                .FirstOrDefaultAsync(c => variants.Contains(c.Name));

            return matchedCategory != null ? matchedCategory.Name : element.Trim();
        }

        private static string ToVietnameseElement(string? element)
        {
            if (string.IsNullOrWhiteSpace(element)) return string.Empty;

            return element.Trim().ToLowerInvariant() switch
            {
                "fire" or "lửa" => "Lửa",
                "water" or "nước" => "Nước",
                "electric" or "điện" => "Điện",
                "rock" or "đá" => "Đá",
                "grass" or "cỏ" => "Cỏ",
                "psychic" or "tâm linh" => "Tâm linh",
                _ => element.Trim()
            };
        }

        private static string[] GetElementVariants(string? element)
        {
            if (string.IsNullOrWhiteSpace(element)) return Array.Empty<string>();

            string trimmed = element.Trim();
            return trimmed.ToLowerInvariant() switch
            {
                "fire" or "lửa" => new[] { "Lửa", "lửa", "LỬA", "Fire", "fire", "FIRE" },
                "water" or "nước" => new[] { "Nước", "nước", "NƯỚC", "Water", "water", "WATER" },
                "electric" or "điện" => new[] { "Điện", "điện", "ĐIỆN", "Electric", "electric", "ELECTRIC" },
                "rock" or "đá" => new[] { "Đá", "đá", "ĐÁ", "Rock", "rock", "ROCK" },
                "grass" or "cỏ" => new[] { "Cỏ", "cỏ", "CỎ", "Grass", "grass", "GRASS" },
                "psychic" or "tâm linh" => new[] { "Tâm linh", "Tâm Linh", "tâm linh", "TÂM LINH", "Psychic", "psychic", "PSYCHIC" },
                _ => new[] { trimmed }
            };
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