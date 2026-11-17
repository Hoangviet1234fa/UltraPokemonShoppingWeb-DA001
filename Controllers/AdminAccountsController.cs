using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AdminAccountsController(AppDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // 1. DANH SÁCH TÀI KHOẢN (Lọc Role theo kiểu string)
        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(kw) || u.Email.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            ViewBag.Search = search;
            ViewBag.Role = role;

            var users = await query.OrderByDescending(u => u.Id).ToListAsync();
            return View(users);
        }

        // 2. DUYỆT TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã phê duyệt tài khoản {user.Email}!";
            return RedirectToAction(nameof(Index));
        }

        // 3. KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "Không thể tự khóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            user.IsApproved = !user.IsApproved;
            await _context.SaveChangesAsync();

            TempData["Success"] = user.IsApproved
                ? $"Đã mở khóa tài khoản {user.Email}."
                : $"Đã khóa tài khoản {user.Email}.";
            return RedirectToAction(nameof(Index));
        }

        // 4. TẠO TÀI KHOẢN MỚI
        public IActionResult Create()
        {
            return View(new User { IsApproved = true, Role = "Staff" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string rawPassword)
        {
            ModelState.Remove("PasswordHash");

            if (string.IsNullOrWhiteSpace(rawPassword) || rawPassword.Length < 6)
            {
                ModelState.AddModelError("rawPassword", "Mật khẩu phải có ít nhất 6 ký tự.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại trong hệ thống.");
            }

            if (ModelState.IsValid)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, rawPassword);
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã tạo tài khoản {user.Email} thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // 5. CHỈNH SỬA TÀI KHOẢN
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model, string? newPassword)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("PasswordHash");

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
            }

            if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length < 6)
            {
                ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            if (ModelState.IsValid)
            {
                existingUser.FullName = model.FullName;
                existingUser.Email = model.Email;
                existingUser.Role = model.Role;
                existingUser.IsApproved = model.IsApproved;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, newPassword);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã cập nhật tài khoản {existingUser.Email}!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 6. XÓA TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "Không thể xóa chính tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa tài khoản {user.Email}.";
            return RedirectToAction(nameof(Index));
        }
    }
}