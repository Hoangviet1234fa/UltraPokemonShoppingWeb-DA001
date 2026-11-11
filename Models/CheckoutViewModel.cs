using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    // ViewModel dùng để đóng gói và validate dữ liệu do khách hàng nhập vào từ Form Thanh toán (Checkout)
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng chi tiết.")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được vượt quá 250 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán.")]
        public string PaymentMethod { get; set; } = "Tiền mặt"; // Gán giá trị mặc định là Tiền mặt để tránh lỗi null
    }
}