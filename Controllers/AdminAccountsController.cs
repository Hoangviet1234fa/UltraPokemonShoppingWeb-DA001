using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // YÊU CẦU BẢO MẬT: Chỉ những tài khoản đăng nhập có Role là "Admin" mới được phép truy cập Controller này
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : Controller
    {
        private readonly AppDbContext _context;

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
                // 1. Xử lý upload ảnh nếu người dùng có chọn file
                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Gán đường dẫn ảnh vào thuộc tính của user (tùy thuộc vào tên trường trong Model của bạn)
                    user.AvatarUrl = "/images/" + fileName;
                }

                // 2. Thêm vào cơ sở dữ liệu và lưu lại bất đồng bộ
                _context.Users.Add(user);
                await _context.SaveChangesAsync(); // Giúp khắc phục cảnh báo CS1998

                // 3. TRẢ VỀ KẾT QUẢ KHI THÀNH CÔNG (Khắc phục lỗi CS0161)
                return RedirectToAction(nameof(Index));
            }

            // 4. TRẢ VỀ KẾT QUẢ KHI DỮ LIỆU KHÔNG HỢP LỆ (Bắt buộc phải có để phủ kín các nhánh logic)
            return View(user);
        }        
        // 1. READ: XEM DANH SÁCH TÀI KHOẢN (INDEX)
        public async Task<IActionResult> Index()
        {
            // Truy vấn lấy toàn bộ danh sách User từ cơ sở dữ liệu
            var accounts = await _context.Users.ToListAsync();
            return View(accounts);
        }

        
        // 2. UPDATE: CHỈNH SỬA THÔNG TIN TÀI KHOẢN
       
        // GET: Hiển thị form chỉnh sửa
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Dùng hàm FindAsync để tìm tài khoản dựa trên ID
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(); // Báo lỗi 404 nếu không tìm thấy ID tài khoản

            return View(user);
        }

        // POST: Xử lý dữ liệu form và lưu vào CSDL
        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công giả mạo yêu cầu chéo trang (CSRF)
        public async Task<IActionResult> Edit(int id, User user)
        {
            // Đảm bảo ID trên URL khớp với ID của form gửi lên
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user); // Đánh dấu đối tượng cần cập nhật
                    await _context.SaveChangesAsync(); // Lưu thay đổi vào CSDL
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Xử lý lỗi trong trường hợp tài khoản vừa bị ai đó xóa mất trước khi ta kịp lưu
                    if (!_context.Users.Any(e => e.Id == user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index)); // Chuyển hướng về trang danh sách
            }
            return View(user); // Trả lại form báo lỗi nếu Validation không hợp lệ
        }

       
        // 3. DELETE: XÓA TÀI KHOẢN (DELETE)
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Thực thi lệnh xóa tài khoản ra khỏi DbSet
                _context.Users.Remove(user);
                await _context.SaveChangesAsync(); // Cập nhật CSDL
            }
            return RedirectToAction(nameof(Index)); // Quay về trang danh sách
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