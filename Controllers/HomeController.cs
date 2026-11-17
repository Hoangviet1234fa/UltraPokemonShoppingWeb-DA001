using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    // Trang chủ: hiển thị 8 lá bài nổi bật có chỉ số sức mạnh cao nhất
    public async Task<IActionResult> Index()
    {
        var featured = await _db.Products.AsNoTracking()
            .OrderByDescending(p => p.Power)
            .Take(8)
            .ToListAsync();
        return View(featured);
    }

    // Trang giới thiệu cửa hàng
    public IActionResult About() => View();

    // Trang thông báo từ chối truy cập khi tài khoản không có quyền Admin
    public IActionResult AccessDenied()
    {
        return View();
    }

    // [ĐÃ BỔ SUNG] Action xử lý ngoại lệ chung khi chạy môi trường Production (khớp với Program.cs)
    public IActionResult Error()
    {
        TempData["Error"] = "Đã xảy ra lỗi hệ thống trong quá trình xử lý. Vui lòng thử lại!";
        return RedirectToAction(nameof(Index));
    }
}