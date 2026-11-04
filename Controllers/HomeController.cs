using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    // Trang chủ: hiển thị các lá bài nổi bật (sức mạnh cao nhất)
    public async Task<IActionResult> Index()
    {
        var featured = await _db.Products.AsNoTracking()
            .OrderByDescending(p => p.Power)
            .Take(8)
            .ToListAsync();
        return View(featured);
    }

    public IActionResult About() => View();

    // Thêm action này để xử lý khi tài khoản không đủ quyền (ví dụ User cố vào trang Admin)
    public IActionResult AccessDenied()
    {
        return View();
    }
}
