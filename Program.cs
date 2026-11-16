using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký dịch vụ MVC
builder.Services.AddControllersWithViews();

// 2. Cấu hình Database SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=ultrapokemon.db"));

// 3. Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// 4. Đăng ký dịch vụ Session cho giỏ hàng
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


// TỰ ĐỘNG TẠO DATABASE, NẠP DỮ LIỆU MẪU & ĐỒNG BỘ ẢNH THẺ BÀI

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Tạo Database nếu chưa tồn tại
    dbContext.Database.EnsureCreated();

    // 1. Danh sách 12 thẻ bài mẫu đồng bộ với database_script.sql & wwwroot/images
    var seedProducts = new List<Product>
    {
        new() { Name = "Charizard",  Price = 450000, Stock = 10, ElementType = "Fire",     Power = 200, ImageUrl = "/images/Charizard.jpeg",  Description = "Lá bài lửa huyền thoại hệ rồng lửa, sức công phá vô song." },
        new() { Name = "Blastoise",  Price = 400000, Stock = 8,  ElementType = "Water",    Power = 180, ImageUrl = "/images/Blastoise.png",   Description = "Lá bài nước mạnh mẽ sở hữu 2 khẩu đại bác thủy lực dũng mãnh." },
        new() { Name = "Raichu",     Price = 120000, Stock = 15, ElementType = "Electric", Power = 150, ImageUrl = "/images/raichu.jpg",      Description = "Lá bài điện chớp nhoáng với chiếc đuôi tích điện công suất lớn." },
        new() { Name = "Jolteon",    Price = 90000,  Stock = 12, ElementType = "Electric", Power = 140, ImageUrl = "/images/Jolteon.jpg",     Description = "Tiến hóa hệ điện của Eevee, lông gai phóng ra ngàn tia sét." },
        new() { Name = "Onix",       Price = 60000,  Stock = 20, ElementType = "Rock",     Power = 130, ImageUrl = "/images/onix.jpg",        Description = "Rắn đá khổng lồ hệ đất đá cứng cáp, phòng thủ tuyệt hảo." },
        new() { Name = "Pikachu",    Price = 150000, Stock = 25, ElementType = "Electric", Power = 110, ImageUrl = "/images/pikachu.png",     Description = "Chú chuột điện vàng đáng yêu, biểu tượng của thế giới Pokémon." },
        new() { Name = "Vulpix",     Price = 50000,  Stock = 18, ElementType = "Fire",     Power = 90,  ImageUrl = "/images/Vulpix.png",      Description = "Hồ ly lửa sáu đuôi xinh xắn, kiểm soát những đốm lửa ma trơi." },
        new() { Name = "Psyduck",    Price = 70000,  Stock = 14, ElementType = "Water",    Power = 95,  ImageUrl = "/images/psyduck.png",     Description = "Vịt khờ hệ nước, bộc phát sức mạnh tâm linh kỳ bí khi đau đầu." },
        new() { Name = "Charmander", Price = 80000,  Stock = 10, ElementType = "Fire",     Power = 100, ImageUrl = "/images/Charmander.jpeg", Description = "Khủng long lửa nhỏ với ngọn lửa luôn bùng cháy nơi chóp đuôi." },
        new() { Name = "Squirtle",   Price = 85000,  Stock = 12, ElementType = "Water",    Power = 105, ImageUrl = "/images/squirtle.jpg",    Description = "Rùa nước tinh nghịch có mai rùa cứng cáp và khả năng bắn tia nước." },
        new() { Name = "Diglett",    Price = 40000,  Stock = 20, ElementType = "Rock",     Power = 80,  ImageUrl = "/images/Diglett.png",     Description = "Pokémon chuột chũi di chuyển ngầm dưới lòng đất cực nhanh." },
        new() { Name = "Sandshrew",  Price = 45000,  Stock = 15, ElementType = "Rock",     Power = 85,  ImageUrl = "/images/sandshrew.jpg",   Description = "Tê tê cát hệ đất cuộn tròn mình lại để chống đỡ đòn đánh của kẻ địch." }
    };

    // Bổ sung những thẻ bài còn thiếu vào DB mà không làm mất dữ liệu đơn hàng cũ
    var existingNames = dbContext.Products
        .Select(p => p.Name.ToLower())
        .ToHashSet();

    var missingProducts = seedProducts
        .Where(sp => !existingNames.Contains(sp.Name.ToLower()))
        .ToList();

    if (missingProducts.Any())
    {
        dbContext.Products.AddRange(missingProducts);
        dbContext.SaveChanges();
    }

    // 2. Tự động đồng bộ ImageUrl từ thư mục wwwroot/images cho toàn bộ sản phẩm trong SQLite
    var imagesDir = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "images");
    if (Directory.Exists(imagesDir))
    {
        var imageFiles = Directory.GetFiles(imagesDir);
        var existingProducts = dbContext.Products.ToList();
        bool hasImageUpdates = false;

        foreach (var product in existingProducts)
        {
            var cleanName = product.Name.Trim().Replace(" ", "").Replace("-", "");
            var matchedFile = imageFiles.FirstOrDefault(file =>
                Path.GetFileNameWithoutExtension(file)
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Equals(cleanName, StringComparison.OrdinalIgnoreCase));

            if (matchedFile != null)
            {
                var webImagePath = "/images/" + Path.GetFileName(matchedFile);
                if (product.ImageUrl != webImagePath)
                {
                    product.ImageUrl = webImagePath;
                    hasImageUpdates = true;
                }
            }
        }

        if (hasImageUpdates)
        {
            dbContext.SaveChanges();
        }
    }

    // 3. Tự động tạo tài khoản Admin mẫu (Email: admin@ultrapokemon.com | Mật khẩu: 123)
    if (!dbContext.Users.Any(u => u.Email == "admin@ultrapokemon.com"))
    {
        var passwordHasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Email = "admin@ultrapokemon.com",
            Role = "Admin",
            IsApproved = true
        };

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