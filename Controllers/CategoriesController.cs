using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class CategoriesController : Controller
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? searchString)
    {
        var list = await _db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        if (!string.IsNullOrWhiteSpace(searchString))
            list = list.Where(c => UltraPokemon.Helpers.TextHelper.Matches(searchString, c.Name)).ToList();
        ViewBag.CurrentFilter = searchString;
        return View(list);
    }

    public async Task<IActionResult> Details(int? id)
    {
        var c = id == null ? null : await _db.Categories.FindAsync(id);
        return c == null ? NotFound() : View(c);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        _db.Add(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        var c = id == null ? null : await _db.Categories.FindAsync(id);
        return c == null ? NotFound() : View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id) return NotFound();
        if (!ModelState.IsValid) return View(category);
        _db.Update(category);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        var c = id == null ? null : await _db.Categories.FindAsync(id);
        return c == null ? NotFound() : View(c);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c != null) { _db.Categories.Remove(c); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
