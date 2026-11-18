using Microsoft.AspNetCore.Identity;
using UltraPokemon.Models;

namespace UltraPokemon.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            var passwordHasher = new PasswordHasher<User>();

            // Tự động tạo mới hoặc cập nhật lại PasswordHash chuẩn cho các tài khoản mặc định
            EnsureSeedUser(context, passwordHasher, "Quản Trị Viên", "admin@ultrapokemon.vn", "Admin");
            EnsureSeedUser(context, passwordHasher, "Nhân Viên Kho", "staff@ultrapokemon.vn", "Staff");
            EnsureSeedUser(context, passwordHasher, "Nguyễn Văn Huấn", "user@ultrapokemon.com", "Customer");
            EnsureSeedUser(context, passwordHasher, "Satoshi Ketchum", "trainer.ash@gmail.com", "Customer");

            context.SaveChanges();
        }

        private static void EnsureSeedUser(
            AppDbContext context,
            PasswordHasher<User> hasher,
            string fullName,
            string email,
            string role)
        {
            var existingUser = context.Users.FirstOrDefault(u => u.Email == email);

            if (existingUser == null)
            {
                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    Role = role,
                    IsApproved = true
                };
                newUser.PasswordHash = hasher.HashPassword(newUser, "123456");
                context.Users.Add(newUser);
            }
            else if (string.IsNullOrEmpty(existingUser.PasswordHash) || existingUser.PasswordHash == "SEED_SYNC_123456")
            {
                // Nếu dữ liệu được nạp từ file database_script.sql, tự động băm lại mật khẩu 123456 chuẩn C#
                existingUser.PasswordHash = hasher.HashPassword(existingUser, "123456");
                existingUser.Role = role;
                existingUser.IsApproved = true;
            }
        }
    }
}