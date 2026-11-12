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
        [ValidateAntiForgeryToken] // Chống tấn công giả mạo (CSRF)
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra email đã tồn tại trong CSDL chưa (Tránh đăng ký trùng lặp)
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký!");
                return View(model);
            }

            // Khởi tạo tài khoản mới
            var newUser = new User
            {
                Email = model.Email,

                // BUSINESS LOGIC QUAN TRỌNG: 
                // Gán cứng (Hardcode) quyền là "User". 
                // Việc gán cứng ở đây đảm bảo dù ai đó cố tình dùng tool sửa code HTML gửi lên role "Admin" thì vẫn bị ép thành "User".
                Role = "User",

                // Tự động phê duyệt để khách có thể đăng nhập và mua hàng ngay lập tức
                IsApproved = true
            };

            // Băm mật khẩu (Mã hóa 1 chiều) trước khi lưu vào CSDL để bảo mật thông tin khách hàng
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            // Lưu vào Database
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }


       
        // 2. CHỨC NĂNG ĐĂNG NHẬP (LOGIN)
        
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken] // Chống tấn công giả mạo (CSRF)
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm kiếm tài khoản trong CSDL thông qua Email
            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Giải mã mật khẩu băm trong CSDL và so sánh với mật khẩu người dùng vừa nhập
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra trạng thái tài khoản (Tránh trường hợp Admin đã khóa/vô hiệu hóa tài khoản này)
            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa hoặc chưa được phê duyệt!");
                return View(model);
            }

            // Tạo các thông tin định danh (Claims) chứa Email và Quyền (Role) để nạp vào Session/Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "User") // Cấp quyền tương ứng từ CSDL
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Thiết lập Cookie phiên đăng nhập (Lưu thông tin đăng nhập vào trình duyệt)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // ĐIỀU HƯỚNG THÔNG MINH DỰA THEO PHÂN QUYỀN (ROLE)
            if (user.Role == "Admin")
            {
                // Nếu là Admin -> Đẩy thẳng vào trang Quản trị Tài khoản
                return RedirectToAction("Index", "AdminAccounts");
            }

            // Nếu là User bình thường -> Đẩy ra trang chủ mua sắm
            return RedirectToAction("Index", "Home");
        }


        // 3. CHỨC NĂNG ĐĂNG XUẤT (LOGOUT)
        
        [HttpPost] // Cập nhật thành HttpPost để khớp với form Đăng xuất bên file _Layout.cshtml
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Hủy Cookie, xóa phiên đăng nhập hiện tại
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa luôn giỏ hàng trong Session (nếu cần, để người khác dùng máy không thấy giỏ hàng cũ)
            HttpContext.Session.Clear();

            // Đẩy về trang chủ
            return RedirectToAction("Index", "Home");
        }
    }
}