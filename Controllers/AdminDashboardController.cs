using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Chỉ tài khoản có quyền Admin mới được phép truy cập Bảng điều khiển
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Lấy dữ liệu nhẹ từ các bảng về bộ nhớ (AsNoTracking giúp tăng tốc độ đọc)
            // Việc đưa về List trước khi Sum/GroupBy giúp tránh lỗi dịch hàm decimal của SQLite
            var allOrders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var allProducts = await _context.Products
                .AsNoTracking()
                .Select(p => new { p.Id, p.Stock })
                .ToListAsync();

            var totalCustomers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.Role == "User");

            // 2. Tính toán các chỉ số KPI tổng quan
            var validOrders = allOrders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

            var model = new AdminDashboardViewModel
            {
                TotalRevenue = validOrders.Sum(o => o.TotalAmount),
                CompletedRevenue = allOrders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .Sum(o => o.TotalAmount),

                TotalOrders = allOrders.Count,
                PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending),

                TotalProducts = allProducts.Count,
                TotalStock = allProducts.Sum(p => p.Stock),
                LowStockProducts = allProducts.Count(p => p.Stock <= 5),

                TotalCustomers = totalCustomers,

                // Đếm số lượng từng trạng thái cho Biểu đồ tròn (Doughnut Chart)
                CountPending = allOrders.Count(o => o.Status == OrderStatus.Pending),
                CountShipping = allOrders.Count(o => o.Status == OrderStatus.Shipping),
                CountCompleted = allOrders.Count(o => o.Status == OrderStatus.Completed),
                CountCancelled = allOrders.Count(o => o.Status == OrderStatus.Cancelled),

                // Lấy 5 đơn hàng mới nhất để hiển thị bảng theo dõi nhanh
                RecentOrders = allOrders.Take(5).ToList()
            };

            // 3. Xây dựng dữ liệu doanh thu 12 tháng gần nhất (tính lùi từ tháng hiện tại)
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            for (int i = 11; i >= 0; i--)
            {
                var targetMonth = currentMonth.AddMonths(-i);
                model.MonthLabels.Add(targetMonth.ToString("MM/yyyy"));

                // Tổng doanh thu của các đơn hợp lệ (không hủy) phát sinh trong tháng targetMonth
                decimal revenueInMonth = validOrders
                    .Where(o => o.OrderDate.Year == targetMonth.Year && o.OrderDate.Month == targetMonth.Month)
                    .Sum(o => o.TotalAmount);

                model.MonthlyRevenues.Add(revenueInMonth);
            }

            // 4. Thống kê doanh thu & số đơn theo từng cổng thanh toán (COD, MoMo, ATM, Visa)
            model.PaymentStats = validOrders
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new PaymentStatItem
                {
                    MethodName = g.Key,
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            return View(model);
        }
    }
}