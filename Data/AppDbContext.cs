using Microsoft.EntityFrameworkCore;
using UltraPokemon.Models;

namespace UltraPokemon.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // Thêm dòng này để kết nối bảng Users với cơ sở dữ liệu
        public DbSet<User> Users { get; set; }
    }
}