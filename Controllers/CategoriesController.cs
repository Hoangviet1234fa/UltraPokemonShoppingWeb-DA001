using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Chỉ tài khoản có vai trò Admin mới được phép quản lý danh mục Hệ nguyên tố
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        
        // 1. READ: HIỂN THỊ DANH SÁCH HỆ NGUYÊN TỐ & ĐẾM SỐ THẺ BÀI THUỘC TỪNG HỆ
     
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync();

            // Dùng LINQ GroupBy để thống kê số lượng thẻ bài theo từng hệ
            var productCounts = await _db.Products
                .AsNoTracking()
                .GroupBy(p => p.ElementType)
                .Select(g => new { Element = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Element, x => x.Count);

            ViewBag.ProductCounts = productCounts;
            return View(categories);
        }

        
        // 2. CREATE: THÊM MỚI HỆ NGUYÊN TỐ
       
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            category.Name = (category.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(category.Name))
            {
                ModelState.AddModelError(nameof(Category.Name), "Tên hệ nguyên tố không được để trống.");
            }
            else
            {
                // Kiểm tra trùng tên hệ (không phân biệt hoa/thường)
                bool isDuplicate = await _db.Categories
                    .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Category.Name), $"Hệ \"{category.Name}\" đã tồn tại trong hệ thống.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm hệ nguyên tố \"{category.Name}\" thành công.";
            return RedirectToAction(nameof(Index));
        }

        
        // 3. UPDATE: CHỈNH SỬA TÊN HỆ & ĐỒNG BỘ SANG BẢNG PRODUCTS
        
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category input)
        {
            if (id != input.Id) return NotFound();

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var newName = (input.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                ModelState.AddModelError(nameof(Category.Name), "Tên hệ nguyên tố không được để trống.");
            }
            else
            {
                bool isDuplicate = await _db.Categories
                    .AnyAsync(c => c.Id != id && c.Name.ToLower() == newName.ToLower());

                if (isDuplicate)
                {
                    ModelState.AddModelError(nameof(Category.Name), $"Hệ \"{newName}\" đã tồn tại.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var oldName = category.Name;

            // Sử dụng Transaction để cập nhật đồng bộ tên hệ trên cả Categories và Products
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                category.Name = newName;
                _db.Categories.Update(category);

                if (!string.Equals(oldName, newName, StringComparison.Ordinal))
                {
                    var relatedProducts = await _db.Products
                        .Where(p => p.ElementType == oldName)
                        .ToListAsync();

                    foreach (var product in relatedProducts)
                    {
                        product.ElementType = newName;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Đã cập nhật hệ nguyên tố thành \"{newName}\".";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật hệ nguyên tố. Vui lòng thử lại!";
                return View(input);
            }
        }

        // 4. DELETE: XÓA HỆ NGUYÊN TỐ (CHẶN XÓA NẾU ĐANG CÓ THẺ BÀI SỬ DỤNG)
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            int productCount = await _db.Products.CountAsync(p => p.ElementType == category.Name);
            if (productCount > 0)
            {
                TempData["Error"] = $"Không thể xóa hệ \"{category.Name}\" vì đang có {productCount} thẻ bài thuộc hệ này.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa hệ nguyên tố \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}