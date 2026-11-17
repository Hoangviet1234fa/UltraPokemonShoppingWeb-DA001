namespace UltraPokemon.Models
{
  
    /// ViewModel chứa toàn bộ số liệu thống kê và dữ liệu biểu đồ cho trang Admin Dashboard.

    public class AdminDashboardViewModel
    {
      
        // 1. CÁC CHỈ SỐ TỔNG QUAN (KPI CARDS)
      
        public decimal TotalRevenue { get; set; }       // Tổng doanh thu dự kiến (tất cả đơn trừ đơn Đã hủy)
        public decimal CompletedRevenue { get; set; }   // Doanh thu thực nhận (chỉ tính đơn Hoàn thành)
        public int TotalOrders { get; set; }            // Tổng số hóa đơn trong hệ thống
        public int PendingOrders { get; set; }          // Số đơn đang chờ Admin duyệt
        public int TotalProducts { get; set; }          // Tổng số mẫu thẻ bài đang kinh doanh
        public int TotalStock { get; set; }             // Tổng số lá bài còn lại trong kho
        public int LowStockProducts { get; set; }       // Số mẫu thẻ bài sắp hết hàng (Stock <= 5)
        public int TotalCustomers { get; set; }         // Tổng số tài khoản khách hàng (Role = User)

    
        // 2. DỮ LIỆU BIỂU ĐỒ DOANH THU 12 THÁNG GẦN NHẤT (BAR CHART)
    
        public List<string> MonthLabels { get; set; } = new();      // Nhãn trục ngang (VD: "12/2025", "01/2026"...)
        public List<decimal> MonthlyRevenues { get; set; } = new(); // Giá trị doanh thu tương ứng từng tháng


        // 3. DỮ LIỆU BIỂU ĐỒ TRẠNG THÁI ĐƠN HÀNG (DOUGHNUT CHART)
        
        public int CountPending { get; set; }   // 0 - Chờ duyệt
        public int CountShipping { get; set; }  // 1 - Đang giao
        public int CountCompleted { get; set; } // 2 - Hoàn thành
        public int CountCancelled { get; set; } // 3 - Đã hủy

        // 4. THỐNG KÊ THEO CỔNG THANH TOÁN & ĐƠN HÀNG MỚI NHẤT
        
        public List<PaymentStatItem> PaymentStats { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
    }

    /// Lớp phụ trợ thống kê số lượng đơn và tổng tiền theo từng phương thức thanh toán.

    public class PaymentStatItem
    {
        public string MethodName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}