using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }


        // 1. CHỨC NĂNG ĐĂNG KÝ TÀI KHOẢN (REGISTER)

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công giả mạo yêu cầu từ trang khác (CSRF)
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra Email đã tồn tại trong CSDL chưa để tránh đăng ký trùng
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký!");
                return View(model);
            }

            // Khởi tạo đối tượng tài khoản mới
            var newUser = new User
            {
                Email = model.Email.Trim(),
                FullName = string.IsNullOrWhiteSpace(model.FullName)
                    ? model.Email.Trim()
                    : model.FullName.Trim(),

                // Gán cứng quyền "Customer" tại Server để ngăn chặn tấn công leo thang đặc quyền "Admin"
                Role = "Customer",

                // Tự động kích hoạt để người dùng có thể đăng nhập và mua hàng ngay
                IsApproved = true
            };

            // Mã hóa mật khẩu một chiều (Hash) trước khi lưu xuống CSDL
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            //  Sử dụng key "Success" khớp với khu vực hiển thị thông báo của _Layout.cshtml
            TempData["Success"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }


        // 2. CHỨC NĂNG ĐĂNG NHẬP (LOGIN)

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm tài khoản theo Email trong CSDL
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // So khớp mật khẩu nhập vào với chuỗi mật khẩu đã băm trong CSDL
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra trạng thái khóa/phê duyệt của tài khoản
            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa hoặc chưa được phê duyệt!");
                return View(model);
            }

            // Tạo danh sách Claims lưu Email và Vai trò (Admin/User) vào Cookie xác thực
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Ghi Cookie phiên đăng nhập vào trình duyệt
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Điều hướng về trang quản trị nếu là Admin, ngược lại về Trang chủ
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "AdminDashboard");
            }

            return RedirectToAction("Index", "Home");
        }


        // 3. CHỨC NĂNG ĐĂNG XUẤT (LOGOUT)

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Hủy Cookie đăng nhập và xóa Session giỏ hàng hiện tại
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}