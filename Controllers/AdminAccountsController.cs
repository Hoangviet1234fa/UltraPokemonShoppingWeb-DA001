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
        // 1. CREATE (Hiển thị form thêm tài khoản mới)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 1. CREATE (Xử lý lưu tài khoản mới vào CSDL và hỗ trợ up ảnh)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh nếu người dùng có chọn file
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Tạo tên file độc lập bằng Guid để không bị trùng lặp
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

                    // Đường dẫn lưu vào thư mục wwwroot/images
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Lưu đường dẫn ảnh vào database (Giả sử model User của bạn có thuộc tính AvatarUrl hoặc ImageUrl)
                    user.AvatarUrl = "/images/" + fileName;
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // 2. READ: Xem danh sách toàn bộ tài khoản trong hệ thống
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Users.ToListAsync();
            return View(accounts);
        }

        // 3. UPDATE (Hiển thị form chỉnh sửa tài khoản)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 3. UPDATE (Xử lý lưu thông tin chỉnh sửa tài khoản)
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

        // 4. DELETE: Xóa tài khoản khỏi hệ thống
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
        // Hành động duyệt tài khoản
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsApproved = true; // Chuyển thành đã duyệt
                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}