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
    }
}