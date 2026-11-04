using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data; // Thêm namespace chứa AppDbContext
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Bắt buộc phân quyền: Chỉ tài khoản có Role là "Admin" mới được truy cập Controller này
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : Controller
    {
        private readonly AppDbContext _context; // Đã đổi chuẩn sang AppDbContext

        public AdminAccountsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. READ: Xem danh sách toàn bộ tài khoản trong hệ thống
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Users.ToListAsync();
            return View(accounts);
        }

        // 2. UPDATE (Hiển thị form chỉnh sửa tài khoản)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 2. UPDATE (Xử lý lưu thông tin chỉnh sửa tài khoản)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Users.Any(e => e.Id == user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // 3. DELETE: Xóa tài khoản khỏi hệ thống
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}