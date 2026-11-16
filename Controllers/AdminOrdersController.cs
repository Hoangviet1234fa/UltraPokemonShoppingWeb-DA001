using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Bắt buộc phân quyền: Chỉ tài khoản có Role là "Admin" mới được phép truy cập Controller này
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        
        // 1. READ: DANH SÁCH ĐƠN HÀNG (KÈM SỐ ĐẾM TAB TRẠNG THÁI & BỘ LỌC TÌM KIẾM)
      
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? paymentMethod, int? status)
        {
            // 1.1 Đếm số lượng đơn theo từng trạng thái để hiển thị badge trên thanh Tab của View
            var allStatuses = await _context.Orders
                .AsNoTracking()
                .Select(o => o.Status)
                .ToListAsync();

            ViewBag.CountAll = allStatuses.Count;
            ViewBag.CountPending = allStatuses.Count(s => s == OrderStatus.Pending);
            ViewBag.CountShipping = allStatuses.Count(s => s == OrderStatus.Shipping);
            ViewBag.CountCompleted = allStatuses.Count(s => s == OrderStatus.Completed);
            ViewBag.CountCancelled = allStatuses.Count(s => s == OrderStatus.Cancelled);

            // 1.2 Khởi tạo truy vấn IQueryable (chưa thực thi SQL ngay để nối thêm các bộ lọc động)
            var query = _context.Orders.AsNoTracking().AsQueryable();

            // Lọc theo từ khóa: Tên người nhận, SĐT, Email hoặc Mã đơn (#ULTRA000001 / số ID)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                // Chuẩn hóa chuỗi để Admin nhập "#ULTRA000005", "ULTRA5" hay "5" đều tìm ra đơn Id = 5
                string cleanIdStr = keyword
                    .Replace("#", "")
                    .Replace("ULTRA", "", StringComparison.OrdinalIgnoreCase);
                bool isNumericId = int.TryParse(cleanIdStr, out int parsedId);

                query = query.Where(o =>
                    (o.FullName != null && o.FullName.Contains(keyword)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.Contains(keyword)) ||
                    o.UserEmail.Contains(keyword) ||
                    (isNumericId && o.Id == parsedId));
            }

            // Lọc theo phương thức thanh toán (Tiền mặt (COD), Ví Momo, ATM, Visa Card)
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(o => o.PaymentMethod == paymentMethod);
            }

            // Lọc theo trạng thái đơn hàng (0: Chờ duyệt, 1: Đang giao, 2: Hoàn thành, 3: Đã hủy)
            if (status.HasValue)
            {
                query = query.Where(o => (int)o.Status == status.Value);
            }

            // 1.3 Sắp xếp đơn mới đặt lên đầu danh sách và thực thi truy vấn xuống Database
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lưu lại giá trị đang lọc để giữ nguyên trạng thái trên các ô input/dropdown của View
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPaymentMethod = paymentMethod;
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

       
        // 2. READ: XEM CHI TIẾT ĐƠN HÀNG VÀ DANH SÁCH THẺ BÀI ĐÃ ĐẶT
       
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Nạp kèm bảng OrderDetails và Product để hiển thị tên lá bài, số lượng và đơn giá
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
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string? returnUrl = null)
        {
            // Bắt buộc Include OrderDetails và Product để lấy dữ liệu Stock phục vụ cộng/trừ kho
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Nếu trạng thái mới trùng với trạng thái hiện tại thì bỏ qua, không cần ghi DB
            if (order.Status == newStatus)
            {
                return RedirectAfterUpdate(id, returnUrl);
            }

            // Mở Transaction để đảm bảo cập nhật tồn kho và trạng thái đơn hàng luôn đồng bộ
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // KỊCH BẢN 1: Admin hủy đơn hàng (Chuyển sang trạng thái Cancelled)
                // -> Hoàn trả toàn bộ số lượng lá bài trong đơn về lại kho
                if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
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
                // KỊCH BẢN 2: Admin khôi phục đơn từ trạng thái Cancelled về trạng thái hoạt động
                // -> Kiểm tra tồn kho trước khi trừ lại để chặn trường hợp âm kho
                else if (order.Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product == null || detail.Product.Stock < detail.Quantity)
                        {
                            string prodName = detail.Product?.Name ?? $"ID #{detail.ProductId}";
                            int currentStock = detail.Product?.Stock ?? 0;

                            await transaction.RollbackAsync();
                            TempData["Error"] = $"Không thể khôi phục đơn! Lá bài \"{prodName}\" chỉ còn {currentStock} trong kho (cần {detail.Quantity}).";
                            return RedirectAfterUpdate(id, returnUrl);
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

                TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng {order.OrderCode} thành công!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"[Admin UpdateStatus Error]: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng. Vui lòng thử lại!";
            }

            return RedirectAfterUpdate(id, returnUrl);
        }

        
        // 4. DELETE: XÓA ĐƠN HÀNG KHỎI HỆ THỐNG VÀ HOÀN TRẢ TỒN KHO
        
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
                // Chỉ hoàn trả kho nếu đơn chưa ở trạng thái Cancelled (vì đơn đã hủy đã được hoàn kho trước đó)
                if (order.Status != OrderStatus.Cancelled)
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

                string deletedOrderCode = order.OrderCode;

                // Xóa đơn hàng (EF Core tự động xóa các bản ghi OrderDetails con theo Cascade Delete)
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Đã xóa đơn hàng {deletedOrderCode} và đồng bộ lại tồn kho!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"[Admin DeleteOrder Error]: {ex.Message}");
                TempData["Error"] = "Không thể xóa đơn hàng lúc này. Vui lòng thử lại!";
            }

            return RedirectToAction(nameof(Index));
        }

       
        // 5. HÀM HỖ TRỢ ĐIỀU HƯỚNG SAU KHI CẬP NHẬT TRẠNG THÁI
      
        private IActionResult RedirectAfterUpdate(int orderId, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
    }
}