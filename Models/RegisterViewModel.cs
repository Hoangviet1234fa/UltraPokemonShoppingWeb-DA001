using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)] // Định dạng dữ liệu là mật khẩu
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận lại mật khẩu")]
        [DataType(DataType.Password)]
        // LOGIC QUAN TRỌNG: Thuộc tính [Compare] sẽ tự động đối chiếu giá trị của ô này với ô "Password" ở trên.
        // Nếu người dùng nhập lệch dù chỉ 1 ký tự, hệ thống sẽ chặn lại và ném ra câu thông báo ErrorMessage.
        [Compare("Password", ErrorMessage = "Mật khẩu và Xác nhận mật khẩu không khớp nhau!")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}