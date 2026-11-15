using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Bắt buộc phân quyền: Chỉ tài khoản có Role là "Admin" mới được phép truy cập trang quản lý đơn hàng
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

    
        // 1. READ: XEM DANH SÁCH TOÀN BỘ HÓA ĐƠN (HỖ TRỢ TÌM KIẾM & LỌC THANH TOÁN)

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? paymentMethod, int? status)
        {
            // Khởi tạo truy vấn IQueryable (chưa chạy xuống SQL ngay để có thể nối thêm điều kiện lọc)
            var query = _context.Orders.AsNoTracking().AsQueryable();

            // 1.1 Lọc theo từ khóa (Tên người nhận, SĐT, Email hoặc Mã đơn dạng #ULTRA000001 / số ID)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                // Cho phép Admin gõ "ULTRA000005" hoặc "5" đều tìm ra đơn có Id = 5
                string cleanIdStr = keyword.Replace("#", "").Replace("ULTRA", "", StringComparison.OrdinalIgnoreCase);
                bool isNumericId = int.TryParse(cleanIdStr, out int parsedId);

                query = query.Where(o =>
                    (o.FullName != null && o.FullName.Contains(keyword)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(keyword)) ||
                    o.UserEmail.Contains(keyword) ||
                    (isNumericId && o.Id == parsedId));
            }

            // 1.2 Lọc theo hình thức thanh toán (Tiền mặt (COD), Ví Momo, ATM, Visa Card)
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(o => o.PaymentMethod == paymentMethod);
            }

            // 1.3 Lọc theo trạng thái đơn hàng (0: Chờ duyệt, 1: Đang xử lý, 2: Hoàn tất, 3: Đã hủy)
            if (status.HasValue)
            {
                query = query.Where(o => (int)o.Status == status.Value);
            }

            // Sắp xếp các đơn mới đặt lên đầu danh sách và thực thi truy vấn
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lưu lại giá trị đang lọc để giữ trạng thái trên thanh tìm kiếm của View (nếu dùng)
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPaymentMethod = paymentMethod;
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

      
        // 2. READ: XEM CHI TIẾT ĐƠN HÀNG VÀ DANH SÁCH LÁ BÀI KHÁCH MUA
      
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

    
        // 3. UPDATE: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG & ĐỒNG BỘ TỒN KHO AN TOÀN
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            // Bắt buộc Include OrderDetails và Product để có dữ liệu Stock cộng/trừ kho
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            int oldStatusInt = (int)order.Status;
            int newStatusInt = (int)newStatus;

            // Nếu Admin bấm lưu nhưng không thay đổi trạng thái thì bỏ qua luôn cho nhẹ DB
            if (oldStatusInt == newStatusInt)
            {
                return RedirectToAction(nameof(Details), new { id });
            }

            // Mở Transaction để đảm bảo cập nhật kho và trạng thái đơn hàng luôn đồng nhất
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // KỊCH BẢN 1: Admin hủy đơn hàng (Chuyển sang trạng thái 3 - Đã hủy)
                // -> Hoàn trả toàn bộ số lượng lá bài của đơn này về lại kho
                if (newStatusInt == 3 && oldStatusInt != 3)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Stock += detail.Quantity;
                            _context.Products.Update(detail.Product);
                        }
                    }
                }
                // KỊCH BẢN 2: Admin khôi phục đơn từ trạng thái Đã hủy (3) về trạng thái hoạt động
                // -> Kiểm tra kho còn đủ hàng hay không trước khi trừ lại
                else if (oldStatusInt == 3 && newStatusInt != 3)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product == null || detail.Product.Stock < detail.Quantity)
                        {
                            string prodName = detail.Product?.Name ?? $"ID #{detail.ProductId}";
                            int currentStock = detail.Product?.Stock ?? 0;

                            await transaction.RollbackAsync();
                            TempData["Error"] = $"Không thể khôi phục đơn! Lá bài \"{prodName}\" chỉ còn {currentStock} trong kho (cần {detail.Quantity}).";
                            return RedirectToAction(nameof(Details), new { id });
                        }

                        detail.Product.Stock -= detail.Quantity;
                        _context.Products.Update(detail.Product);
                    }
                }

                // Cập nhật trạng thái mới cho đơn hàng
                order.Status = newStatus;
                _context.Orders.Update(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #ULTRA{order.Id:D6} thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"[Admin UpdateStatus Error]: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng. Vui lòng thử lại!";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

      
        // 4. DELETE: XÓA HÓA ĐƠN KHỎI HỆ THỐNG VÀ HOÀN TRẢ TỒN KHO

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Nếu đơn bị xóa khi CHƯA HỦY (trạng thái khác 3), phải cộng trả lại hàng vào kho
                // (Nếu đơn đã ở trạng thái 3 thì kho đã được hoàn lúc hủy rồi, không cộng thêm nữa để tránh nhân đôi)
                if ((int)order.Status != 3)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Stock += detail.Quantity;
                            _context.Products.Update(detail.Product);
                        }
                    }
                }

                // Xóa đơn hàng (EF Core tự động xóa các bản ghi OrderDetails con theo Cascade Delete)
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Đã xóa đơn hàng #ULTRA{order.Id:D6} và đồng bộ lại tồn kho!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"[Admin DeleteOrder Error]: {ex.Message}");
                TempData["Error"] = "Không thể xóa đơn hàng lúc này. Vui lòng thử lại!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}