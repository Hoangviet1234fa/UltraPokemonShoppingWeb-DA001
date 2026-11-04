using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Helpers;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var cart = CartSession.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = products
            .Select(p => new CartLine { Product = p, Quantity = cart[p.Id] })
            .OrderBy(l => l.Product.Name)
            .ToList();
        return View(lines);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? returnUrl)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var cart = CartSession.Get(HttpContext.Session);
        cart.TryGetValue(id, out var qty);
        cart[id] = qty + 1;
        CartSession.Save(HttpContext.Session, cart);
        TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Product");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        var cart = CartSession.Get(HttpContext.Session);
        cart.Remove(id);
        CartSession.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());
        return RedirectToAction(nameof(Index));
    }
}
