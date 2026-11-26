using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers
{
    // Chỉ cho phép Admin và Staff truy cập trang quản lý đơn hàng
    [Authorize(Roles = "Admin,Staff")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }


        // 1. DANH SÁCH ĐƠN HÀNG (TÌM KIẾM + LỌC TRẠNG THÁI + PHÂN TRANG LINQ)

        public async Task<IActionResult> Index(string? status, string? search, int page = 1)
        {
            // Nạp kèm OrderDetails để đếm số lượng thẻ trong từng đơn
            // Lưu ý: Không dùng .Include(o => o.User) vì Order liên kết qua trường UserEmail
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            // Lọc theo trạng thái đơn hàng
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
            {
                query = query.Where(o => o.Status == parsedStatus);
            }

            // Tìm kiếm theo mã đơn (#ULTRA000001 hoặc số Id), tên người nhận, SĐT hoặc UserEmail
            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower().Replace("#ultra", "").Replace("#", "").TrimStart('0');
                string rawKw = search.Trim().ToLower();

                query = query.Where(o => (!string.IsNullOrEmpty(kw) && o.Id.ToString().Contains(kw))
                                      || o.FullName.ToLower().Contains(rawKw)
                                      || o.PhoneNumber.Contains(rawKw)
                                      || o.UserEmail.ToLower().Contains(rawKw));
            }

            // Sắp xếp đơn hàng mới nhất lên đầu
            query = query.OrderByDescending(o => o.OrderDate);

            // Xử lý phân trang với LINQ Skip / Take
            int pageSize = 10;
            if (page < 1) page = 1;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Giữ lại thông số tìm kiếm và phân trang cho View
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(orders);
        }


        // 2. XEM CHI TIẾT ĐƠN HÀNG

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }


        // 3. CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (TỰ ĐỘNG HOÀN KHO NGUYÊN TỬ KHI HỦY ĐƠN)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            // Nhánh Admin/Staff hủy đơn: cập nhật nguyên tử có điều kiện dưới SQL để chống hoàn kho 2 lần
            // khi khách hàng cũng bấm hủy đơn tại cùng một thời điểm (Concurrency)
            if (newStatus == OrderStatus.Cancelled)
            {
                var orderInfo = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (orderInfo == null) return NotFound();

                // Khóa không cho sửa nếu đơn đã Hoàn thành hoặc Đã hủy
                if (orderInfo.Status == OrderStatus.Cancelled || orderInfo.Status == OrderStatus.Completed)
                {
                    TempData["Error"] = "Đơn hàng đã hoàn tất hoặc đã hủy nên không thể thay đổi trạng thái.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Chỉ chuyển sang Cancelled nếu đơn chưa bị hủy và chưa hoàn tất
                    int affectedRows = await _context.Orders
                        .Where(o => o.Id == id
                                 && o.Status != OrderStatus.Cancelled
                                 && o.Status != OrderStatus.Completed)
                        .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled));

                    // Nếu affectedRows == 0 nghĩa là khách hàng vừa tự hủy đơn này trước đó 1 mili-giây -> chặn cộng kho lần 2
                    if (affectedRows == 0)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "Đơn hàng đã hoàn tất hoặc đã bị khách hàng hủy trước đó nên không thể thay đổi trạng thái.";
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    // Cộng hoàn trả số lượng thẻ bài vào kho nguyên tử (Stock = Stock + Quantity)
                    var orderDetails = await _context.OrderDetails
                        .AsNoTracking()
                        .Where(od => od.OrderId == id)
                        .Select(od => new { od.ProductId, od.Quantity })
                        .ToListAsync();

                    foreach (var detail in orderDetails)
                    {
                        await _context.Products
                            .Where(p => p.Id == detail.ProductId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + detail.Quantity));
                    }

                    await transaction.CommitAsync();
                    TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng {orderInfo.OrderCode} thành công!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại!";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Các trạng thái khác (Pending -> Shipping -> Completed): giữ nguyên luồng xử lý gốc
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Khóa không cho sửa nếu đơn đã Hoàn thành hoặc Đã hủy
            if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Completed)
            {
                TempData["Error"] = "Đơn hàng đã hoàn tất hoặc đã hủy nên không thể thay đổi trạng thái.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (newStatus == OrderStatus.Completed) order.IsPaid = true;
            order.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng {order.OrderCode} thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}