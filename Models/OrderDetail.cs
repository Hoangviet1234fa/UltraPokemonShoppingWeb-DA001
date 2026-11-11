using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UltraPokemon.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        // Khóa ngoại liên kết với bảng Order (Hóa đơn)
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        // Khóa ngoại liên kết với bảng Product (Sản phẩm/Lá bài)
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        // Số lượng lá bài khách mua trong đơn này
        public int Quantity { get; set; }

        // Đơn giá của lá bài tại thời điểm chốt đơn
        public decimal UnitPrice { get; set; }
    }
}