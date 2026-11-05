using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Helpers;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class ProductController : Controller
{
    private static readonly string[] AllowedExt = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ------------------------------------------------------------------
    // INDEX: Mở công khai cho mọi người xem danh sách, tìm kiếm, lọc
    // ------------------------------------------------------------------
    public async Task<IActionResult> Index(string? searchString, string? elementType, string? sortOrder)
    {
        // Danh sách hệ chuẩn lấy từ bảng Categories (dùng cho các nút lọc)
        var elements = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Id).Select(c => c.Name).ToListAsync();

        // Đưa giá trị "elementType" trên URL về đúng tên hệ trong bảng Categories
        string? element = null;
        if (!string.IsNullOrWhiteSpace(elementType))
        {
            var key = TextHelper.Normalize(elementType);
            element = elements.FirstOrDefault(e => TextHelper.Normalize(e) == key) ?? elementType.Trim();
        }

        // 1) Lọc theo Hệ ngay trong câu SQL (LINQ -> WHERE ElementType = ...)
        IQueryable<Product> query = _db.Products.AsNoTracking();
        if (element != null)
            query = query.Where(p => p.ElementType == element);

        var products = await query.ToListAsync();

        // 2) Tìm theo TÊN thẻ. SQLite phân biệt hoa/thường và dấu khi dùng Contains(),
        //    nên so khớp bằng TextHelper.Normalize để "charizard", "CHARIZARD" đều ra "Charizard".
        string? note = null;
        var keyword = TextHelper.Normalize(searchString);
        var ranked = false;

        if (keyword.Length > 0)
        {
            var words = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var byName = products
                .Where(p => { var n = TextHelper.Normalize(p.Name); return words.All(w => n.Contains(w)); })
                .ToList();

            if (byName.Count > 0)
            {
                products = byName;
                ranked = true;
            }
            else
            {
                // Không có thẻ nào trùng tên: nếu khách gõ tên hệ (vd "lửa") thì gợi ý các thẻ thuộc hệ đó
                var byElement = products
                    .Where(p => { var e = TextHelper.Normalize(p.ElementType); return words.All(w => e.Contains(w)); })
                    .ToList();
                if (byElement.Count > 0)
                {
                    products = byElement;
                    note = $"Không có thẻ nào tên \"{searchString!.Trim()}\" - hiển thị các thẻ thuộc hệ phù hợp.";
                }
                else
                {
                    products = new List<Product>();
                }
            }
        }

        // 3) Sắp xếp. Khi tìm theo tên và chưa chọn sắp xếp: khớp chính xác -> bắt đầu bằng -> còn lại
        products = sortOrder switch
        {
            "price_asc" => products.OrderBy(p => p.Price).ToList(),
            "price_desc" => products.OrderByDescending(p => p.Price).ToList(),
            "power_desc" => products.OrderByDescending(p => p.Power).ToList(),
            "name_desc" => products.OrderByDescending(p => p.Name).ToList(),
            _ when ranked => products
                .OrderBy(p => { var n = TextHelper.Normalize(p.Name); return n == keyword ? 0 : n.StartsWith(keyword) ? 1 : 2; })
                .ThenBy(p => p.Name).ToList(),
            _ => products.OrderBy(p => p.Name).ToList()
        };

        ViewBag.CurrentFilter = searchString;
        ViewBag.CurrentElement = element;
        ViewBag.CurrentSort = sortOrder;
        ViewBag.Elements = elements;
        ViewBag.SearchNote = note;

        return View(products);
    }

    // Trang chi tiết: Mở công khai cho mọi người xem thông tin thẻ bài
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return BadRequest();
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (product == null) return NotFound();

        // Gợi ý các thẻ cùng hệ
        ViewBag.Related = await _db.Products.AsNoTracking()
            .Where(p => p.ElementType == product.ElementType && p.Id != product.Id)
            .OrderByDescending(p => p.Power)
            .Take(4)
            .ToListAsync();

        return View(product);
    }

    // ---------- Create (CHỈ ADMIN MỚI ĐƯỢC PHÉP TRUY CẬP) ----------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await LoadElements();
        return View(new Product());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,ElementType,Power,Price,Description")] Product product, IFormFile? imageFile)
    {
        ModelState.Remove(nameof(Product.ImageUrl));
        ModelState.Remove(nameof(Product.Description));
        product.Description ??= string.Empty;

        if (string.IsNullOrWhiteSpace(product.Name))
            ModelState.AddModelError(nameof(Product.Name), "Tên thẻ bài không được để trống!");

        var upload = await SaveImage(imageFile);
        if (upload.Error != null) ModelState.AddModelError("imageFile", upload.Error);

        if (!ModelState.IsValid)
        {
            await LoadElements();
            return View(product);
        }

        product.ImageUrl = upload.Url ?? string.Empty;
        _db.Add(product);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm thẻ \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Edit (CHỈ ADMIN MỚI ĐƯỢC PHÉP TRUY CẬP) ----------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        await LoadElements();
        return View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ElementType,Power,Price,Description")] Product input, IFormFile? imageFile)
    {
        if (id != input.Id) return NotFound();
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        ModelState.Remove(nameof(Product.ImageUrl));
        ModelState.Remove(nameof(Product.Description));

        if (string.IsNullOrWhiteSpace(input.Name))
            ModelState.AddModelError(nameof(Product.Name), "Tên thẻ bài không được để trống!");

        var upload = await SaveImage(imageFile);
        if (upload.Error != null) ModelState.AddModelError("imageFile", upload.Error);

        if (!ModelState.IsValid)
        {
            input.ImageUrl = product.ImageUrl;
            await LoadElements();
            return View(input);
        }

        product.Name = input.Name;
        product.ElementType = input.ElementType;
        product.Power = input.Power;
        product.Price = input.Price;
        product.Description = input.Description ?? string.Empty;
        if (upload.Url != null) product.ImageUrl = upload.Url;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật thẻ \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Delete (CHỈ ADMIN MỚI ĐƯỢC PHÉP TRUY CẬP) ----------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product == null ? NotFound() : View(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product != null)
        {
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa thẻ \"{product.Name}\".";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------
    private async Task LoadElements() =>
        ViewBag.ElementList = new SelectList(
            await _db.Categories.AsNoTracking().OrderBy(c => c.Id).Select(c => c.Name).ToListAsync());

    private async Task<(string? Url, string? Error)> SaveImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return (null, null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext)) return (null, "Chỉ hỗ trợ ảnh .png, .jpg, .jpeg, .webp, .gif");
        if (file.Length > MaxImageBytes) return (null, "Ảnh tối đa 5 MB");

        var dir = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var fs = new FileStream(Path.Combine(dir, name), FileMode.Create))
            await file.CopyToAsync(fs);

        return ($"/images/{name}", null);
    }
}