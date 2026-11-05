using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Bắt buộc phân quyền: Chỉ tài khoản có Role là "Admin" mới được phép quản lý đơn hàng
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. READ: XEM DANH SÁCH TOÀN BỘ HÓA ĐƠN (INDEX)
        
        public async Task<IActionResult> Index()
        {
            // Dùng LINQ lấy danh sách đơn hàng, sắp xếp các đơn mới đặt lên đầu danh sách
            var orders = await _context.Orders
                                       .OrderByDescending(o => o.OrderDate)
                                       .ToListAsync();
            return View(orders);
        }

        
        // 2. READ: XEM CHI TIẾT ĐƠN HÀNG VÀ CÁC MÓN HÀNG KHÁCH MUA
      
        public async Task<IActionResult> Details(int id)
        {
            // Dùng LINQ kết hợp Include và ThenInclude (Eager Loading) để lấy Hóa đơn kèm theo Chi tiết Hóa đơn và thông tin Sản phẩm
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails) // Lấy danh sách sản phẩm trong đơn (OrderDetail)
                                      .ThenInclude(od => od.Product) // Lấy chi tiết thông tin lá bài (Tên, Hình ảnh,...)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }


        // 3. UPDATE: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (NGHIỆP VỤ CỐT LÕI)
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Tiến hành cập nhật trạng thái mới (Chờ duyệt -> Đang giao -> Hoàn thành)
            order.Status = newStatus;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{order.Id} thành công!";

            // Cập nhật xong chuyển hướng về trang danh sách đơn hàng
            return RedirectToAction(nameof(Index));
        }

       
        // 4. DELETE: XÓA HÓA ĐƠN KHỎI HỆ THỐNG
      
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                // Khi xóa Order, EF Core sẽ tự động dọn dẹp các OrderDetail liên quan (Cascading Delete)
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn hàng khỏi hệ thống!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}