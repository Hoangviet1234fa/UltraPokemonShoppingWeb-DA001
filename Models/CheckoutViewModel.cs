using System.ComponentModel.DataAnnotations;

namespace UltraPokemon.Models
{
    /// <summary>
    /// ViewModel hứng dữ liệu từ Form Checkout.
    /// Bắt buộc nhập đầy đủ thông tin người nhận cho mọi phương thức vì sản phẩm là thẻ bài vật lý cần giao hàng.
    /// </summary>
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng hợp lệ.")]
        [StringLength(15, ErrorMessage = "Số điện thoại tối đa 15 ký tự.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng chi tiết.")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được vượt quá 250 ký tự.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán.")]
        public string PaymentMethod { get; set; } = "Tiền mặt (COD)";
    }
}