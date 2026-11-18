using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using UltraPokemon.Data;
using UltraPokemon.Models;


// 1. KHỞI TẠO BUILDER & ĐĂNG KÝ CÁC DỊCH VỤ (DEPENDENCY INJECTION CONTAINER)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


// 2. TỰ ĐỘNG KHỞI TẠO DATABASE & NẠP DỮ LIỆU MẪU (SEED DATA) KHI CHẠY LẦN ĐẦU

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

    context.Database.EnsureCreated();

    // BƯỚC 2.1: Nạp dữ liệu mẫu cho bảng Categories
    if (!context.Categories.Any())
    {
        context.Categories.AddRange(
            new Category { Name = "Fire", Description = "Thẻ bài Pokémon hệ Lửa rực cháy" },
            new Category { Name = "Water", Description = "Thẻ bài Pokémon hệ Nước linh hoạt" },
            new Category { Name = "Electric", Description = "Thẻ bài Pokémon hệ Điện tốc độ cao" },
            new Category { Name = "Rock", Description = "Thẻ bài Pokémon hệ Đá phòng thủ vững chắc" },
            new Category { Name = "Grass", Description = "Thẻ bài Pokémon hệ Cỏ bền bỉ" },
            new Category { Name = "Psychic", Description = "Thẻ bài Pokémon hệ Tâm Linh huyền bí" }
        );
        context.SaveChanges();
    }

    // BƯỚC 2.2: Nạp đủ 12 thẻ bài Pokémon gốc sử dụng ảnh trong wwwroot/images
    if (!context.Products.Any())
    {
        context.Products.AddRange(
            new Product { Name = "Charizard", ElementType = "Fire", Power = 200, Price = 450000, Stock = 10, Description = "Lá bài lửa huyền thoại hệ rồng lửa, sức công phá vô song.", ImageUrl = "/images/Charizard.jpeg" },
            new Product { Name = "Blastoise", ElementType = "Water", Power = 180, Price = 400000, Stock = 8, Description = "Lá bài nước mạnh mẽ sở hữu 2 khẩu đại bác thủy lực dũng mãnh.", ImageUrl = "/images/Blastoise.png" },
            new Product { Name = "Raichu", ElementType = "Electric", Power = 150, Price = 120000, Stock = 15, Description = "Lá bài điện chớp nhoáng với chiếc đuôi tích điện công suất lớn.", ImageUrl = "/images/raichu.jpg" },
            new Product { Name = "Jolteon", ElementType = "Electric", Power = 140, Price = 90000, Stock = 12, Description = "Tiến hóa hệ điện của Eevee, lông gai phóng ra ngàn tia sét.", ImageUrl = "/images/Jolteon.jpg" },
            new Product { Name = "Onix", ElementType = "Rock", Power = 130, Price = 60000, Stock = 20, Description = "Rắn đá khổng lồ hệ đất đá cứng cáp, phòng thủ tuyệt hảo.", ImageUrl = "/images/onix.jpg" },
            new Product { Name = "Pikachu", ElementType = "Electric", Power = 110, Price = 150000, Stock = 25, Description = "Chú chuột điện vàng đáng yêu, biểu tượng của thế giới Pokémon.", ImageUrl = "/images/pikachu.png" },
            new Product { Name = "Vulpix", ElementType = "Fire", Power = 90, Price = 50000, Stock = 18, Description = "Hồ ly lửa sáu đuôi xinh xắn, kiểm soát những đốm lửa ma trơi.", ImageUrl = "/images/Vulpix.png" },
            new Product { Name = "Psyduck", ElementType = "Water", Power = 95, Price = 70000, Stock = 14, Description = "Vịt khờ hệ nước, bộc phát sức mạnh tâm linh kỳ bí khi đau đầu.", ImageUrl = "/images/psyduck.png" },
            new Product { Name = "Charmander", ElementType = "Fire", Power = 100, Price = 80000, Stock = 10, Description = "Khủng long lửa nhỏ với ngọn lửa luôn bùng cháy nơi chóp đuôi.", ImageUrl = "/images/Charmander.jpeg" },
            new Product { Name = "Squirtle", ElementType = "Water", Power = 105, Price = 85000, Stock = 12, Description = "Rùa nước tinh nghịch có mai rùa cứng cáp và khả năng bắn tia nước.", ImageUrl = "/images/squirtle.jpg" },
            new Product { Name = "Diglett", ElementType = "Rock", Power = 80, Price = 40000, Stock = 20, Description = "Pokémon chuột chũi di chuyển ngầm dưới lòng đất cực nhanh.", ImageUrl = "/images/Diglett.png" },
            new Product { Name = "Sandshrew", ElementType = "Rock", Power = 85, Price = 45000, Stock = 15, Description = "Tê tê cát hệ đất cuộn tròn mình lại để chống đỡ đòn đánh của kẻ địch.", ImageUrl = "/images/sandshrew.jpg" }
        );
        context.SaveChanges();
    }

    // BƯỚC 2.3: Nạp 3 tài khoản mẫu với Role kiểu string ("Admin", "Staff", "Customer")
    if (!context.Users.Any())
    {
        var admin = new User
        {
            FullName = "Quản Trị Viên",
            Email = "admin@ultrapokemon..vn",
            Role = "Admin",
            IsApproved = true
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, "123456");

        var staff = new User
        {
            FullName = "Nhân Viên Kho",
            Email = "staff@ultrapokemon..vn",
            Role = "Staff",
            IsApproved = true
        };
        staff.PasswordHash = passwordHasher.HashPassword(staff, "123456");

        var customer = new User
        {
            FullName = "Satoshi Ketchum",
            Email = "customer@ultrapokemon..vn",
            Role = "Customer",
            IsApproved = true
        };
        customer.PasswordHash = passwordHasher.HashPassword(customer, "123456");

        context.Users.AddRange(admin, staff, customer);
        context.SaveChanges();
    }
}


// 3. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();