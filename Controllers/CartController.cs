using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltraPokemon.Data;
using UltraPokemon.Helpers;
using UltraPokemon.Models;

namespace UltraPokemon.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private const string PendingOnlineOrderKey = "PENDING_ONLINE_ORDER_ID";

    public CartController(AppDbContext db) => _db = db;


    // 1. QUẢN LÝ GIỎ HÀNG (XEM, THÊM, CẬP NHẬT SỐ LƯỢNG, XÓA, LÀM RỖNG)

    public async Task<IActionResult> Index()
    {
        // Nếu khách quay lại giỏ hàng từ cổng thanh toán mà chưa xác nhận trả tiền -> hoàn đơn nháp & giữ nguyên giỏ
        await RollbackAbandonedOnlineOrderAsync();

        var cart = CartSession.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = products
            .Select(p => new CartLine { Product = p, Quantity = cart[p.Id] })
            .OrderBy(l => l.Product.Name)
            .ToList();

        return View(lines);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? returnUrl)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        // Chặn ngay nếu sản phẩm đã hết hàng (Stock <= 0)
        if (product.Stock <= 0)
        {
            TempData["Error"] = $"Sản phẩm \"{product.Name}\" hiện đã hết hàng!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Product");
        }

        var cart = CartSession.Get(HttpContext.Session);
        cart.TryGetValue(id, out var qty);

        // Kiểm tra tồn kho trước khi cho phép thêm vào giỏ
        if (qty + 1 > product.Stock)
        {
            TempData["Error"] = $"Không thể thêm. Cửa hàng chỉ còn {product.Stock} lá \"{product.Name}\".";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Product");
        }

        cart[id] = qty + 1;
        CartSession.Save(HttpContext.Session, cart);
        TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Product");
    }

    // Cập nhật số lượng trực tiếp trong giỏ hàng
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var cart = CartSession.Get(HttpContext.Session);

        if (quantity <= 0)
        {
            cart.Remove(id);
            TempData["Success"] = $"Đã xóa \"{product.Name}\" khỏi giỏ hàng.";
        }
        else if (quantity > product.Stock)
        {
            TempData["Error"] = $"Không đủ hàng! Kho chỉ còn {product.Stock} lá \"{product.Name}\".";
        }
        else
        {
            cart[id] = quantity;
        }

        CartSession.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        var cart = CartSession.Get(HttpContext.Session);
        cart.Remove(id);
        CartSession.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());
        return RedirectToAction(nameof(Index));
    }


    // 2. THANH TOÁN (CHECKOUT) & TẠO ĐƠN HÀNG (TRANSACTION)

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        // Bắt buộc người dùng đăng nhập khi bấm nút Tiến hành thanh toán
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["Error"] = "Vui lòng đăng nhập tài khoản để tiến hành xác nhận thanh toán!";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Checkout), "Cart") });
        }

        // Nếu khách bấm Back từ trang QR MoMo/ATM quay về Checkout -> hoàn đơn nháp & giữ nguyên giỏ hàng
        await RollbackAbandonedOnlineOrderAsync();

        var cart = CartSession.Get(HttpContext.Session);
        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction(nameof(Index));
        }

        // Nạp danh sách sản phẩm và tổng tiền vào ViewBag cho Checkout.cshtml
        await LoadCartSummaryToViewBag(cart);
        return View(new CheckoutViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["Error"] = "Vui lòng đăng nhập tài khoản để tiến hành xác nhận thanh toán!";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Checkout), "Cart") });
        }

        return await ExecuteCheckoutAsync(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["Error"] = "Vui lòng đăng nhập tài khoản để tiến hành xác nhận thanh toán!";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Checkout), "Cart") });
        }

        return await ExecuteCheckoutAsync(model);
    }

    private async Task<IActionResult> ExecuteCheckoutAsync(CheckoutViewModel model)
    {
        // Dọn dẹp đơn thanh toán online chưa hoàn tất trước đó (nếu có)
        await RollbackAbandonedOnlineOrderAsync();

        var cart = CartSession.Get(HttpContext.Session);
        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await LoadCartSummaryToViewBag(cart);
            return View("Checkout", model);
        }

        var ids = cart.Keys.ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        // Kiểm tra tồn kho thực tế trước khi tạo hóa đơn
        foreach (var p in products)
        {
            if (cart[p.Id] > p.Stock)
            {
                TempData["Error"] = $"Lỗi: Sản phẩm \"{p.Name}\" chỉ còn {p.Stock} lá. Vui lòng giảm số lượng!";
                return RedirectToAction(nameof(Index));
            }
        }

        // Bọc trong Transaction để đảm bảo tạo Order, OrderDetail và trừ kho đồng bộ
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserEmail = User.Identity!.Name!,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                IsPaid = false, // Mặc định chưa thanh toán cho tới khi xác nhận hoặc thu hộ COD
                TotalAmount = products.Sum(p => Convert.ToDecimal(p.Price) * cart[p.Id]),
                FullName = model.FullName,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber,
                PaymentMethod = model.PaymentMethod
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var p in products)
            {
                var buyQuantity = cart[p.Id];

                _db.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = buyQuantity,
                    UnitPrice = Convert.ToDecimal(p.Price)
                });

                // Trừ tồn kho tương ứng số lượng đặt mua
                p.Stock -= buyQuantity;
                _db.Products.Update(p);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Với thanh toán Online: CHƯA xóa giỏ hàng ngay, lưu tạm ID đơn chờ thanh toán
            if (model.PaymentMethod == "Ví Momo")
            {
                HttpContext.Session.SetInt32(PendingOnlineOrderKey, order.Id);
                return RedirectToAction(nameof(MomoPayment), new { orderId = order.Id });
            }

            if (model.PaymentMethod == "Chuyển khoản Ngân hàng (ATM)" || model.PaymentMethod == "Visa Card")
            {
                HttpContext.Session.SetInt32(PendingOnlineOrderKey, order.Id);
                return RedirectToAction(nameof(AtmPayment), new { orderId = order.Id });
            }

            // Với COD: đơn hàng hoàn tất ngay nên làm rỗng giỏ hàng (IsPaid vẫn giữ false cho tới khi giao hàng)
            CartSession.Save(HttpContext.Session, new Dictionary<int, int>());
            TempData["Success"] = $"Đặt hàng thành công ({order.OrderCode})! Đơn hàng COD của bạn đang chờ duyệt.";
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"[Checkout Error]: {ex.Message}");
            TempData["Error"] = "Đã xảy ra lỗi hệ thống trong quá trình thanh toán. Vui lòng thử lại!";
            return RedirectToAction(nameof(Index));
        }
    }


    // 3. CÁC CỔNG THANH TOÁN TRỰC TUYẾN (MOMO & ATM / VISA)

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MomoPayment(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserEmail == User.Identity!.Name);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> AtmPayment(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserEmail == User.Identity!.Name);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ConfirmOnlinePayment(int orderId)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserEmail == User.Identity!.Name);

        if (order == null) return NotFound();

        // Đánh dấu đơn hàng đã thanh toán thành công trong CSDL
        if (order.Status != OrderStatus.Cancelled)
        {
            order.IsPaid = true;
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
        }

        // Chỉ xóa giỏ hàng sau khi khách đã xác nhận thanh toán thành công
        HttpContext.Session.Remove(PendingOnlineOrderKey);
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());

        TempData["Success"] = $"Thanh toán qua {order.PaymentMethod} thành công cho đơn hàng {order.OrderCode}!";
        return RedirectToAction(nameof(MyOrders));
    }


    // 4. LỊCH SỬ ĐƠN HÀNG & HỦY ĐƠN CHỜ DUYỆT (HOÀN KHO)

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        // Nếu khách bấm nút "Thanh toán sau • Xem đơn hàng của tôi", giữ nguyên IsPaid = false và làm rỗng giỏ hàng
        if (HttpContext.Session.GetInt32(PendingOnlineOrderKey).HasValue)
        {
            HttpContext.Session.Remove(PendingOnlineOrderKey);
            CartSession.Save(HttpContext.Session, new Dictionary<int, int>());
        }

        var userEmail = User.Identity!.Name!;
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserEmail == userEmail)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> CancelMyOrder(int id)
    {
        var userEmail = User.Identity!.Name!;
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserEmail == userEmail);

        if (order == null) return NotFound();

        // Chỉ cho phép khách tự hủy khi đơn còn ở trạng thái Chờ duyệt
        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = $"Đơn hàng {order.OrderCode} đã được xử lý hoặc đang giao, không thể tự hủy!";
            return RedirectToAction(nameof(MyOrders));
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Hoàn trả số lượng sản phẩm về kho
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    detail.Product.Stock += detail.Quantity;
                    _db.Products.Update(detail.Product);
                }
            }

            order.Status = OrderStatus.Cancelled;
            _db.Orders.Update(order);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã hủy đơn hàng {order.OrderCode} và hoàn trả số lượng sản phẩm về kho.";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            System.Diagnostics.Debug.WriteLine($"[CancelMyOrder Error]: {ex.Message}");
            TempData["Error"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại!";
        }

        return RedirectToAction(nameof(MyOrders));
    }


    // CÁC HÀM HỖ TRỢ NỘI BỘ (ROLLBACK ĐƠN NHÁP & TÍNH TỔNG GIỎ HÀNG)

    // Tự động hoàn kho & xóa đơn nháp nếu khách bỏ dở thanh toán online để quay lại giỏ hàng
    private async Task RollbackAbandonedOnlineOrderAsync()
    {
        var pendingOrderId = HttpContext.Session.GetInt32(PendingOnlineOrderKey);
        if (!pendingOrderId.HasValue) return;

        HttpContext.Session.Remove(PendingOnlineOrderKey);

        var abandonedOrder = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == pendingOrderId.Value
                                   && o.Status == OrderStatus.Pending
                                   && !o.IsPaid);

        if (abandonedOrder != null)
        {
            foreach (var detail in abandonedOrder.OrderDetails)
            {
                if (detail.Product != null)
                {
                    detail.Product.Stock += detail.Quantity;
                    _db.Products.Update(detail.Product);
                }
            }

            _db.OrderDetails.RemoveRange(abandonedOrder.OrderDetails);
            _db.Orders.Remove(abandonedOrder);
            await _db.SaveChangesAsync();
        }
    }

    // Hàm hỗ trợ tính toán giỏ hàng và truyền sang ViewBag cho Checkout.cshtml
    private async Task LoadCartSummaryToViewBag(Dictionary<int, int> cart)
    {
        var ids = cart.Keys.ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = products
            .Select(p => new CartLine { Product = p, Quantity = cart[p.Id] })
            .OrderBy(l => l.Product.Name)
            .ToList();

        ViewBag.CartLines = lines;
        ViewBag.TotalAmount = lines.Sum(l => Convert.ToDecimal(l.Product.Price) * l.Quantity);
        ViewBag.TotalQuantity = lines.Sum(l => l.Quantity);
    }
}