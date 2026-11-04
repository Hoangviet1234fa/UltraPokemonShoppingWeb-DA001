using Microsoft.AspNetCore.Authentication; // Thư viện hỗ trợ xác thực (SignIn/SignOut)
using Microsoft.AspNetCore.Authentication.Cookies; // Thư viện cấu hình Cookie Authentication
using Microsoft.AspNetCore.Identity; // Thư viện mã hóa mật khẩu PasswordHasher
using Microsoft.AspNetCore.Mvc; // Thư viện cơ bản cho Controller và View trong mô hình MVC
using System.Security.Claims; // Thư viện quản lý thông tin định danh (Claims) của người dùng
using UltraPokemon.Data; // Import DbContext để kết nối cơ sở dữ liệu SQLite
using UltraPokemon.Models; // Import các mô hình dữ liệu (User, ViewModels)

namespace UltraPokemon.Controllers // Khai báo namespace chứa các Controller của ứng dụng
{
    public class AccountController : Controller // Khởi tạo lớp AccountController kế thừa từ Controller cơ sở
    {
        private readonly AppDbContext _context; // Biến lưu trữ DbContext để thao tác truy vấn CSDL
        private readonly PasswordHasher<User> _passwordHasher; // Biến hỗ trợ băm và kiểm tra mật khẩu bảo mật cho User

        // Constructor khởi tạo các Dependency Injection (tiêm phụ thuộc)
        public AccountController(AppDbContext context)
        {
            _context = context; // Gán giá trị DbContext vào biến cục bộ
            _passwordHasher = new PasswordHasher<User>(); // Khởi tạo đối tượng PasswordHasher chuyên dụng cho Model User
        }

        [HttpGet] // Định nghĩa phương thức HTTP GET cho trang Đăng ký tài khoản
        public IActionResult Register() => View(); // Trả về giao diện View Register.cshtml tương ứng

        [HttpPost] // Định nghĩa phương thức HTTP POST để xử lý dữ liệu gửi lên khi người dùng bấm đăng ký
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model); // Kiểm tra tính hợp lệ của dữ liệu đầu vào, nếu lỗi trả lại form kèm thông báo

            var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email); // Kiểm tra xem email đã tồn tại trong CSDL chưa
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký!"); // Thêm lỗi vào ModelState nếu trùng email
                return View(model); // Trả về lại form đăng ký
            }

            var newUser = new User // Khởi tạo đối tượng User mới từ dữ liệu người dùng nhập
            {
                Email = model.Email, // Gán email người dùng
                Role = "User", // Mặc định phân quyền tài khoản mới tạo là User thông thường
                IsApproved = true // ĐÃ CẬP NHẬT: Tự động phê duyệt tài khoản ngay khi đăng ký thành công mà không cần Admin duyệt
            };

            // Tiến hành mã hóa mật khẩu dạng thô thành chuỗi PasswordHash bảo mật
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, model.Password);

            _context.Users.Add(newUser); // Thêm đối tượng User mới vào DbSet của Entity Framework
            await _context.SaveChangesAsync(); // Lưu thay đổi vào cơ sở dữ liệu SQLite bất đồng bộ

            TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ."; // Cập nhật thông báo thành công cho người dùng
            return RedirectToAction("Login"); // Chuyển hướng người dùng sang trang Đăng nhập
        }

        [HttpGet] // Định nghĩa phương thức HTTP GET cho trang Đăng nhập
        public IActionResult Login() => View(); // Trả về giao diện Login.cshtml

        [HttpPost] // Định nghĩa phương thức HTTP POST để xử lý xác thực thông tin đăng nhập
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model); // Kiểm tra dữ liệu form đăng nhập có hợp lệ không

            var user = _context.Users.FirstOrDefault(u => u.Email == model.Email); // Tìm kiếm thông tin người dùng trong CSDL dựa theo Email
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác."); // Báo lỗi chung chung nếu không tìm thấy email
                return View(model);
            }

            // Kiểm tra mật khẩu nhập vào có khớp với chuỗi PasswordHash đã mã hóa trong CSDL không
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác."); // Báo lỗi nếu mật khẩu sai
                return View(model);
            }

            // Kiểm tra xem tài khoản đã được phê duyệt (IsApproved = true) hay chưa
            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Tài khoản của bạn chưa được phê duyệt!"); // Báo lỗi nếu tài khoản chưa được duyệt
                return View(model);
            }

            // Khởi tạo danh sách các Claim chứa thông tin định danh phiên làm việc của người dùng
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email), // Lưu tên nhận diện là Email
                new Claim(ClaimTypes.Email, user.Email), // Lưu thông tin Email
                new Claim(ClaimTypes.Role, user.Role ?? "User") // Lưu quyền hạn (Role) để phục vụ phân quyền Authorization
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme); // Tạo đối tượng ClaimsIdentity với scheme Cookie

            // Tiến hành đăng nhập hệ thống, ghi nhận Cookie xác thực vào HttpContext
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            // Nếu tài khoản là Admin, điều hướng chính xác về trang Quản lý tài khoản (AdminAccounts/Index)
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "AdminAccounts");
            }

            // Nếu là khách hàng thông thường (User), chuyển hướng về trang chủ mua sắm (Home/Index)
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout() // Phương thức xử lý chức năng Đăng xuất tài khoản
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Xóa bỏ Cookie xác thực hiện tại của HttpContext
            return RedirectToAction("Index", "Home"); // Chuyển hướng người dùng về trang chủ sau khi đăng xuất thành công
        }
    }
}