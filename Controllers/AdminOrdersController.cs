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
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                      .ThenInclude(od => od.Product)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

       
        // 3. UPDATE: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG VÀ XỬ LÝ TỒN KHO
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            // QUAN TRỌNG: Phải dùng Include để lấy kèm chi tiết đơn và Sản phẩm thì mới có dữ liệu Stock để cộng/trừ
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                      .ThenInclude(od => od.Product)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            int oldStatusInt = (int)order.Status;
            int newStatusInt = (int)newStatus;

            // KỊCH BẢN 1: Admin hủy đơn hàng (Chuyển sang trạng thái 3 - Đã hủy)
            if (newStatusInt == 3 && oldStatusInt != 3)
            {
                // Vòng lặp chạy qua từng món hàng khách đã mua để cộng trả lại kho
                foreach (var detail in order.OrderDetails)
                {
                    detail.Product.Stock += detail.Quantity;
                    _context.Update(detail.Product);
                }
            }
            // KỊCH BẢN 2: Admin "quay xe", từ Đã hủy (3) khôi phục lại trạng thái bình thường (Chờ duyệt/Đang giao/...)
            else if (oldStatusInt == 3 && newStatusInt != 3)
            {
                // Vòng lặp chạy qua từng món hàng để trừ lại kho như lúc mới đặt
                foreach (var detail in order.OrderDetails)
                {
                    detail.Product.Stock -= detail.Quantity;
                    _context.Update(detail.Product);
                }
            }

            // Tiến hành cập nhật trạng thái mới
            order.Status = newStatus;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{order.Id} thành công!";
            return RedirectToAction(nameof(Details), new { id = id });
        }

       
        // 4. DELETE: XÓA HÓA ĐƠN KHỎI HỆ THỐNG VÀ HOÀN KHO
 
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Phải Include để lấy ra các thẻ bài nhằm mục đích cộng trả lại kho trước khi xóa bỏ
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                      .ThenInclude(od => od.Product)
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                // Nếu đơn hàng bị xóa khi ĐANG TRONG TRẠNG THÁI BÌNH THƯỜNG (không phải đã hủy),
                // thì hệ thống phải tự động hoàn trả hàng về kho để không bị thất thoát số lượng.
                if ((int)order.Status != 3)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        detail.Product.Stock += detail.Quantity;
                        _context.Update(detail.Product);
                    }
                }

                // Khi xóa Order, EF Core sẽ tự động dọn dẹp các OrderDetail liên quan (Cascading Delete)
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa đơn hàng và hoàn tất xử lý tồn kho!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}