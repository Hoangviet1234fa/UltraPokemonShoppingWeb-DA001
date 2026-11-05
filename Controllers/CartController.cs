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
    public CartController(AppDbContext db) => _db = db;

    // 1. HIỂN THỊ GIỎ HÀNG
    public async Task<IActionResult> Index()
    {
        var cart = CartSession.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();

        var lines = products
            .Select(p => new CartLine { Product = p, Quantity = cart[p.Id] })
            .OrderBy(l => l.Product.Name)
            .ToList();

        return View(lines);
    }

    // 2. THÊM SẢN PHẨM VÀO GIỎ
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? returnUrl)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var cart = CartSession.Get(HttpContext.Session);
        cart.TryGetValue(id, out var qty);

        // BUSINESS LOGIC BỔ SUNG: Chặn ngay từ lúc thêm vào giỏ nếu vượt quá tồn kho
        if (qty + 1 > product.Stock)
        {
            TempData["Error"] = $"Không thể thêm. Cửa hàng chỉ còn {product.Stock} lá \"{product.Name}\".";
            return RedirectToAction("Index", "Product");
        }

        cart[id] = qty + 1;
        CartSession.Save(HttpContext.Session, cart);
        TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Product");
    }

    // 3. XÓA 1 SẢN PHẨM KHỎI GIỎ
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        var cart = CartSession.Get(HttpContext.Session);
        cart.Remove(id);
        CartSession.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }

    // 4. XÓA TRẮNG GIỎ HÀNG
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());
        return RedirectToAction(nameof(Index));
    }

    // ==============================================================
    // 5. THANH TOÁN & NGHIỆP VỤ TỒN KHO (Phần quan trọng nhất)
    // ==============================================================
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize] // Bắt buộc người dùng đăng nhập mới được thanh toán
    public async Task<IActionResult> Checkout()
    {
        var cart = CartSession.Get(HttpContext.Session);
        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction(nameof(Index));
        }

        // Lấy danh sách sản phẩm. CẦN THIẾT: Bỏ AsNoTracking() để EF Core có thể Update lại số lượng (Stock)
        var ids = cart.Keys.ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        // 5.1 BUSINESS LOGIC: KIỂM TRA TỒN KHO LẦN CUỐI TRƯỚC KHI LƯU
        foreach (var p in products)
        {
            var buyQuantity = cart[p.Id];
            if (buyQuantity > p.Stock)
            {
                TempData["Error"] = $"Lỗi: Sản phẩm \"{p.Name}\" chỉ còn {p.Stock} lá. Vui lòng giảm số lượng!";
                return RedirectToAction(nameof(Index));
            }
        }

        // BẮT ĐẦU TRANSACTION: Đảm bảo dữ liệu không bị lưu nửa chừng nếu có lỗi phát sinh
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 5.2 TẠO HÓA ĐƠN (ORDER)
            var order = new Order
            {
                UserEmail = User.Identity!.Name!,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = products.Sum(p => Convert.ToDecimal(p.Price) * cart[p.Id])
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(); // Lưu lần 1 để EF Core sinh ra mã OrderId

            // 5.3 TẠO CHI TIẾT HÓA ĐƠN & TRỪ TỒN KHO
            foreach (var p in products)
            {
                var buyQuantity = cart[p.Id];

                // Lưu OrderDetail
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = buyQuantity,
                    UnitPrice = Convert.ToDecimal(p.Price)
                };
                _db.OrderDetails.Add(orderDetail);

                // Cập nhật trừ số lượng tồn kho
                p.Stock -= buyQuantity;
                _db.Products.Update(p);
            }

            // Lưu toàn bộ chi tiết Hóa đơn và thay đổi tồn kho của Sản phẩm
            await _db.SaveChangesAsync();

            // Xác nhận hoàn tất Transaction (Commit) - Khóa dữ liệu thành công
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            // Nếu có bất kỳ lỗi gì xảy ra (vd mất kết nối DB), hủy toàn bộ thao tác trước đó (Rollback)
            await transaction.RollbackAsync();
            TempData["Error"] = "Đã xảy ra lỗi hệ thống trong quá trình thanh toán. Vui lòng thử lại!";
            return RedirectToAction(nameof(Index));
        }
        // 5.4 LÀM SẠCH GIỎ HÀNG VÀ THÔNG BÁO THÀNH CÔNG
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());

        TempData["Success"] = "Đặt hàng thành công! Đơn hàng của bạn đang chờ duyệt.";

        // Sửa dòng cũ thành dòng này (Quay lại chính trang Giỏ hàng)
        return RedirectToAction(nameof(Index));
    }
}