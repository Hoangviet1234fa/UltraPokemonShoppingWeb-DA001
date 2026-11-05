using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ MVC
builder.Services.AddControllersWithViews();

// Cấu hình Database SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=ultrapokemon.db"));

// Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// Đăng ký dịch vụ Session cho giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Tự động tạo Database và nạp dữ liệu mẫu (Sản phẩm + Tài khoản Admin)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Chỉ dùng duy nhất lệnh này để tạo DB, tránh lỗi "Database disk image is malformed"
    dbContext.Database.EnsureCreated();

    // 1. Tự động thêm lại các thẻ bài mẫu kèm số lượng tồn kho (Stock) nếu bảng đang trống
    if (!dbContext.Products.Any())
    {
        dbContext.Products.AddRange(
            new Product { Name = "Charizard", Price = 450000, Stock = 10, ElementType = "Fire", Power = 200, Description = "Lá bài lửa huyền thoại" },
            new Product { Name = "Blastoise", Price = 400000, Stock = 8, ElementType = "Water", Power = 180, Description = "Lá bài nước mạnh mẽ" },
            new Product { Name = "Raichu", Price = 120000, Stock = 15, ElementType = "Electric", Power = 150, Description = "Lá bài điện chớp nhoáng" },
            new Product { Name = "Jolteon", Price = 90000, Stock = 12, ElementType = "Electric", Power = 140, Description = "Tiến hóa hệ điện" },
            new Product { Name = "Onix", Price = 60000, Stock = 20, ElementType = "Rock", Power = 130, Description = "Hệ đá cứng cáp" },
            new Product { Name = "Pikachu", Price = 150000, Stock = 25, ElementType = "Electric", Power = 110, Description = "Chú chuột điện đáng yêu" },
            new Product { Name = "Vulpix", Price = 50000, Stock = 18, ElementType = "Fire", Power = 90, Description = "Cáo lửa nhỏ" },
            new Product { Name = "Psyduck", Price = 70000, Stock = 14, ElementType = "Water", Power = 95, Description = "Vịt khờ hệ nước" }
        );
        dbContext.SaveChanges();
    }

    // 2. Tự động tạo sẵn tài khoản Admin mẫu và mã hóa mật khẩu bằng PasswordHasher
    if (!dbContext.Users.Any(u => u.Email == "admin@ultrapokemon.com"))
    {
        var passwordHasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Email = "admin@ultrapokemon.com",
            Role = "Admin",
            IsApproved = true
        };

        // Mã hóa mật khẩu "123" chuẩn định dạng để không bị lỗi FormattedException khi đăng nhập
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "123");

        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

// Kích hoạt Session Middleware (Phải đặt trước UseAuthentication và UseAuthorization)
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();