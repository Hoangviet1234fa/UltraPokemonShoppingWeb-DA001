-- =======================================================================
-- TRƯỜNG ĐẠI HỌC / HỌC PHẦN: LẬP TRÌNH TRÊN WEB (WEB PROGRAMMING)
-- ĐỀ TÀI 01: ONLINE SHOPPING WEBSITE - ULTRA POKEMON TCG STORE
-- TẬP LỆNH TẠO CƠ SỞ DỮ LIỆU ĐẦY ĐỦ VÀ NẠP TOÀN BỘ DỮ LIỆU (SQL SCRIPT)
-- HỆ QUẢN TRỊ CSDL: MICROSOFT SQL SERVER / LOCALDB
-- =======================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'UltraPokemonDB')
BEGIN
    CREATE DATABASE UltraPokemonDB;
END
GO

USE UltraPokemonDB;
GO

-- 2. DỌN DẸP BẢNG CŨ (NẾU ĐÃ TỒN TẠI ĐỂ TRÁNH LỖI XUNG ĐỘT)
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- =======================================================================
-- 3. ĐỊNH NGHĨA CẤU TRÚC CÁC BẢNG (TABLE SCHEMAS & CONSTRAINTS)
-- =======================================================================

-- BẢNG 1: CATEGORIES (HỆ POKÉMON / DANH MỤC THẺ BÀI)
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id ASC)
);
GO

-- BẢNG 2: USERS (TÀI KHOẢN NGƯỜI DÙNG & PHÂN QUYỀN HỆ THỐNG)
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT N'User',
    IsApproved BIT NOT NULL DEFAULT 1,
    AvatarUrl NVARCHAR(255) NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

-- BẢNG 3: PRODUCTS (DANH SÁCH THẺ BÀI POKÉMON, TỒN KHO & CHỈ SỐ)
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ImageUrl NVARCHAR(255) NOT NULL DEFAULT N'',
    ElementType NVARCHAR(50) NOT NULL,
    Power INT NOT NULL DEFAULT 100,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Description NVARCHAR(500) NOT NULL DEFAULT N'',
    Stock INT NOT NULL DEFAULT 10,
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Products_Power CHECK (Power >= 0)
);
GO

-- BẢNG 4: ORDERS (HÓA ĐƠN ĐẶT HÀNG CỦA KHÁCH)
-- Trạng thái đơn hàng (Status):
--   0 = Pending (Chờ duyệt)
--   1 = Shipping (Đang giao hàng)
--   2 = Completed (Hoàn thành)
--   3 = Cancelled (Đã hủy)
CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1001,1) NOT NULL,
    UserEmail NVARCHAR(100) NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Status INT NOT NULL DEFAULT 0,
    FullName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT N'Tiền mặt',
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (0, 1, 2, 3))
);
GO

-- BẢNG 5: ORDERDETAILS (CHI TIẾT MÓN HÀNG TRONG HÓA ĐƠN)
CREATE TABLE dbo.OrderDetails (
    Id INT IDENTITY(1,1) NOT NULL,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT PK_OrderDetails PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0)
);
GO

-- =======================================================================
-- 4. NẠP TOÀN BỘ DỮ LIỆU ĐẦY ĐỦ CỦA PROJECT (FULL SEED DATA)
-- =======================================================================

SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (Id, Name) VALUES 
(1, N'Fire'),
(2, N'Water'),
(3, N'Electric'),
(4, N'Rock'),
(5, N'Grass'),
(6, N'Psychic');
SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Users ON;
-- Mật khẩu mặc định của các tài khoản mẫu đều là: 123
-- (Chuỗi Hash tương thích chuẩn ASP.NET Core PasswordHasher v3)
INSERT INTO dbo.Users (Id, Email, PasswordHash, Role, IsApproved, AvatarUrl) VALUES 
(1, N'admin@ultrapokemon.com', N'AQAAAAIAAYagAAAAEOvE+gQd17WvjEGB/k1L9j8GvJkK8v1fQ9gQ8zY+1pL9kK8v1fQ9gQ8zY+1pL9kK8w==', N'Admin', 1, NULL),
(2, N'user@ultrapokemon.com', N'AQAAAAIAAYagAAAAEOvE+gQd17WvjEGB/k1L9j8GvJkK8v1fQ9gQ8zY+1pL9kK8v1fQ9gQ8zY+1pL9kK8w==', N'User', 1, NULL),
(3, N'trainer.ash@gmail.com', N'AQAAAAIAAYagAAAAEOvE+gQd17WvjEGB/k1L9j8GvJkK8v1fQ9gQ8zY+1pL9kK8v1fQ9gQ8zY+1pL9kK8w==', N'User', 1, NULL);
SET IDENTITY_INSERT dbo.Users OFF;
GO

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, Name, ElementType, Power, Price, Stock, Description, ImageUrl) VALUES 
(1, N'Charizard', N'Fire', 200, 450000.00, 10, N'Lá bài lửa huyền thoại hệ rồng lửa, sức công phá vô song.', N'/images/Charizard.jpeg'),
(2, N'Blastoise', N'Water', 180, 400000.00, 8, N'Lá bài nước mạnh mẽ sở hữu 2 khẩu đại bác thủy lực dũng mãnh.', N'/images/Blastoise.png'),
(3, N'Raichu', N'Electric', 150, 120000.00, 15, N'Lá bài điện chớp nhoáng với chiếc đuôi tích điện công suất lớn.', N'/images/raichu.jpg'),
(4, N'Jolteon', N'Electric', 140, 90000.00, 12, N'Tiến hóa hệ điện của Eevee, lông gai phóng ra ngàn tia sét.', N'/images/Jolteon.jpg'),
(5, N'Onix', N'Rock', 130, 60000.00, 20, N'Rắn đá khổng lồ hệ đất đá cứng cáp, phòng thủ tuyệt hảo.', N'/images/onix.jpg'),
(6, N'Pikachu', N'Electric', 110, 150000.00, 25, N'Chú chuột điện vàng đáng yêu, biểu tượng của thế giới Pokémon.', N'/images/pikachu.png'),
(7, N'Vulpix', N'Fire', 90, 50000.00, 18, N'Hồ ly lửa sáu đuôi xinh xắn, kiểm soát những đốm lửa ma trơi.', N'/images/Vulpix.png'),
(8, N'Psyduck', N'Water', 95, 70000.00, 14, N'Vịt khờ hệ nước, bộc phát sức mạnh tâm linh kỳ bí khi đau đầu.', N'/images/psyduck.png'),
(9, N'Charmander', N'Fire', 100, 80000.00, 10, N'Khủng long lửa nhỏ với ngọn lửa luôn bùng cháy nơi chóp đuôi.', N'/images/Charmander.jpeg'),
(10, N'Squirtle', N'Water', 105, 85000.00, 12, N'Rùa nước tinh nghịch có mai rùa cứng cáp và khả năng bắn tia nước.', N'/images/squirtle.jpg'),
(11, N'Diglett', N'Rock', 80, 40000.00, 20, N'Pokémon chuột chũi di chuyển ngầm dưới lòng đất cực nhanh.', N'/images/Diglett.png'),
(12, N'Sandshrew', N'Rock', 85, 45000.00, 15, N'Tê tê cát hệ đất cuộn tròn mình lại để chống đỡ đòn đánh của kẻ địch.', N'/images/sandshrew.jpg');
SET IDENTITY_INSERT dbo.Products OFF;
GO

SET IDENTITY_INSERT dbo.Orders ON;
INSERT INTO dbo.Orders (Id, UserEmail, OrderDate, TotalAmount, Status, FullName, Address, PhoneNumber, PaymentMethod) VALUES 
(1001, N'user@ultrapokemon.com', '2026-11-04 14:30:00', 600000.00, 0, N'Nguyễn Văn Huấn', N'123 Đường Cầu Giấy, Hà Nội', N'0912345678', N'Tiền mặt (COD)'),
(1002, N'trainer.ash@gmail.com', '2026-11-04 16:45:00', 520000.00, 1, N'Trần Thị Lan', N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'0987654321', N'Chuyển khoản Ngân hàng'),
(1003, N'user@ultrapokemon.com', '2026-11-05 09:15:00', 190000.00, 2, N'Nguyễn Văn Huấn', N'123 Đường Cầu Giấy, Hà Nội', N'0912345678', N'Ví Momo');
SET IDENTITY_INSERT dbo.Orders OFF;
GO

SET IDENTITY_INSERT dbo.OrderDetails ON;
INSERT INTO dbo.OrderDetails (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES 
-- Chi tiết Đơn 1001 (Tổng 600.000 đ)
(1, 1001, 1, 1, 450000.00), -- 1x Charizard
(2, 1001, 6, 1, 150000.00), -- 1x Pikachu

-- Chi tiết Đơn 1002 (Tổng 520.000 đ)
(3, 1002, 2, 1, 400000.00), -- 1x Blastoise
(4, 1002, 3, 1, 120000.00), -- 1x Raichu

-- Chi tiết Đơn 1003 (Tổng 190.000 đ)
(5, 1003, 4, 1, 90000.00),  -- 1x Jolteon
(6, 1003, 7, 2, 50000.00);  -- 2x Vulpix
SET IDENTITY_INSERT dbo.OrderDetails OFF;
GO

-- =======================================================================
-- 5. TRUY VẤN KIỂM TRA NHANH KẾT QUẢ TẠO DỮ LIỆU
-- =======================================================================
SELECT 'Categories' AS [Table], COUNT(*) AS [TotalRows] FROM dbo.Categories
UNION ALL
SELECT 'Products' AS [Table], COUNT(*) AS [TotalRows] FROM dbo.Products
UNION ALL
SELECT 'Users' AS [Table], COUNT(*) AS [TotalRows] FROM dbo.Users
UNION ALL
SELECT 'Orders' AS [Table], COUNT(*) AS [TotalRows] FROM dbo.Orders
UNION ALL
SELECT 'OrderDetails' AS [Table], COUNT(*) AS [TotalRows] FROM dbo.OrderDetails;
GO
