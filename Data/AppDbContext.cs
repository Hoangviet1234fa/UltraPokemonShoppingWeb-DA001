using Microsoft.EntityFrameworkCore;
using UltraPokemon.Models;

namespace UltraPokemon.Data
{
    // Giữ nguyên tên lớp AppDbContext để khớp với toàn bộ Controller và Migrations gốc
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Khai báo bảng Reviews trong Entity Framework Core
        public DbSet<Review> Reviews { get; set; }

        // Cấu hình khóa ngoại và ràng buộc chống đánh giá trùng lặp
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Review>(entity =>
            {
                // Ràng buộc duy nhất (Unique Index): Mỗi sản phẩm trong 1 đơn hàng chỉ được đánh giá tối đa 1 lần
                entity.HasIndex(r => new { r.OrderId, r.ProductId }).IsUnique();

                // Khi xóa sản phẩm thì tự động xóa các đánh giá của sản phẩm đó
                entity.HasOne(r => r.Product)
                      .WithMany(p => p.Reviews)
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Dùng Restrict cho Order để tránh xung đột vòng lặp xóa (Multiple Cascade Paths)
                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}