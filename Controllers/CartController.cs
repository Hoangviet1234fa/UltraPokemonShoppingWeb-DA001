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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? returnUrl)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        var cart = CartSession.Get(HttpContext.Session);
        cart.TryGetValue(id, out var qty);
        cart[id] = qty + 1;
        CartSession.Save(HttpContext.Session, cart);
        TempData["Success"] = $"Đã thêm \"{product.Name}\" vào giỏ hàng.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Product");
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

    //  THANH TOÁN & TỒN KHO
    
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize] // Bắt buộc khách hàng phải đăng nhập mới được thanh toán
    public async Task<IActionResult> Checkout()
    {
        var cart = CartSession.Get(HttpContext.Session);
        if (!cart.Any())
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống!";
            return RedirectToAction(nameof(Index));
        }

        // Lấy danh sách sản phẩm trong giỏ hàng từ DB (Không dùng AsNoTracking vì cần Update tồn kho)
        var ids = cart.Keys.ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();

        // 1. BUSINESS LOGIC: KIỂM TRA TỒN KHO TRƯỚC KHI LƯU
        foreach (var p in products)
        {
            var buyQuantity = cart[p.Id];
            if (buyQuantity > p.Stock)
            {
                TempData["Error"] = $"Lỗi: Sản phẩm \"{p.Name}\" chỉ còn {p.Stock} lá. Vui lòng giảm số lượng!";
                return RedirectToAction(nameof(Index)); // Quay lại giỏ hàng và báo lỗi
            }
        }

        // 2. TẠO HÓA ĐƠN (ORDER)
        var order = new Order
        {
            UserEmail = User.Identity!.Name!, // Lấy Email người đang đăng nhập
            OrderDate = DateTime.Now,
            Status = OrderStatus.Pending, // Chờ duyệt

            // Dùng LINQ tính tổng tiền: Lấy Giá (chuyển đổi nếu cần) nhân với Số lượng
            TotalAmount = products.Sum(p => Convert.ToDecimal(p.Price) * cart[p.Id])
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // Lưu để EF Core tự sinh ra mã OrderId

        // 3. TẠO CHI TIẾT HÓA ĐƠN & TRỪ TỒN KHO
        foreach (var p in products)
        {
            var buyQuantity = cart[p.Id];

            // 3.1 Thêm dữ liệu vào bảng OrderDetail
            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = p.Id,
                Quantity = buyQuantity,
                UnitPrice = Convert.ToDecimal(p.Price)
            };
            _db.OrderDetails.Add(orderDetail);

            // 3.2 BUSINESS LOGIC: TRỪ SỐ LƯỢNG TỒN KHO
            p.Stock -= buyQuantity;
            _db.Products.Update(p);
        }

        // 4. LƯU TOÀN BỘ THAY ĐỔI
        await _db.SaveChangesAsync();

        // 5. LÀM SẠCH GIỎ HÀNG SAU KHI THÀNH CÔNG
        CartSession.Save(HttpContext.Session, new Dictionary<int, int>());

        TempData["Success"] = "Đặt hàng thành công! Đơn hàng của bạn đang chờ duyệt.";
        return RedirectToAction(nameof(Index));
    }
}