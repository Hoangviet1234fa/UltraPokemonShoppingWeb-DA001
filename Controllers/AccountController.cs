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

        // 1. CHỨC NĂNG ĐĂNG KÝ (REGISTER)
       
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra email đã tồn tại trong CSDL chưa (Tránh trùng lặp tài khoản)
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký!");
                return View(model);
            }

            var newUser = new User
            {
                Email = model.Email,
                Role = "User", // Phân quyền mặc định cho tài khoản mới
                // BUSINESS LOGIC: Tự động phê duyệt tài khoản ngay khi đăng ký để người dùng có thể mua hàng lập tức
                IsApproved = true
            };

            // Mã hóa mật khẩu bảo mật (Password Hash) trước khi lưu xuống CSDL
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

       
        // 2. CHỨC NĂNG ĐĂNG NHẬP (LOGIN)
      
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm kiếm tài khoản trong CSDL bằng Email (LINQ)
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Giải mã và đối chiếu mật khẩu người dùng nhập vào với chuỗi băm trong CSDL
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra trạng thái phê duyệt của tài khoản
            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Tài khoản của bạn chưa được phê duyệt!");
                return View(model);
            }

            // Khởi tạo các thông tin định danh (Claims) để lưu vào Cookie phiên đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User") // Lưu Quyền hạn (Role) để hệ thống phân quyền
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Ghi Cookie xác thực vào hệ thống (Thiết lập phiên đăng nhập)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // ĐIỀU HƯỚNG THÔNG MINH DỰA THEO PHÂN QUYỀN (ROLE)
            if (user.Role == "Admin")
            {
                // Tránh lỗi 404: Trỏ Admin về đúng trang quản lý danh sách tài khoản
                return RedirectToAction("Index", "AdminAccounts");
            }

            // Người dùng bình thường sẽ được đưa về trang mua sắm
            return RedirectToAction("Index", "Home");
        }

      
        // 3. CHỨC NĂNG ĐĂNG XUẤT (LOGOUT)
       
        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie để hủy phiên đăng nhập hiện tại
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}