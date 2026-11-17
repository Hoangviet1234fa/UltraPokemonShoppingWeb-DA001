using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // SỬA LỖI: Giữ nguyên kiểu string ("Admin", "Staff", "Customer") khớp với AccountController gốc
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public bool IsApproved { get; set; } = true;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}