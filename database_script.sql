-- HỆ THỐNG CƠ SỞ DỮ LIỆU: ULTRAPOKÉMON CARD STORE 

-- 1. TẠO CƠ SỞ DỮ LIỆU
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'UltraPokemonDB')
BEGIN
    CREATE DATABASE UltraPokemonDB;
END
GO

USE UltraPokemonDB;
GO

-- 2. DỌN DẸP BẢNG CŨ (XÓA BẢNG CON TRƯỚC, BẢNG CHA SAU ĐỂ TRÁNH LỖI KHÓA NGOẠI)
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- 3. ĐỊNH NGHĨA CẤU TRÚC CÁC BẢNG (TABLE SCHEMAS & CONSTRAINTS)

-- BẢNG 1: CATEGORIES (HỆ POKÉMON / DANH MỤC THẺ BÀI)
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(250) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

-- BẢNG 2: USERS (TÀI KHOẢN NGƯỜI DÙNG & PHÂN QUYỀN HỆ THỐNG)
CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NULL,
    FullName NVARCHAR(100) NOT NULL DEFAULT N'Thành viên UltraPokémon',
    Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT N'Customer', -- 'Admin', 'Staff', 'Customer', 'User'
    IsApproved BIT NOT NULL DEFAULT 1,              -- 1 = Đã duyệt, 0 = Chờ duyệt/Khóa
    IsActive BIT NOT NULL DEFAULT 1,                -- 1 = Đang hoạt động, 0 = Bị vô hiệu hóa
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    AvatarUrl NVARCHAR(255) NULL,
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Email_VN CHECK (Email LIKE N'%@%.vn'),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Staff', N'Customer', N'User'))
);
GO

-- BẢNG 3: PRODUCTS (DANH SÁCH THẺ BÀI POKÉMON, TỒN KHO & CHỈ SỐ)
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    ImageUrl NVARCHAR(255) NOT NULL DEFAULT N'',
    ElementType NVARCHAR(50) NOT NULL,          -- Hệ nguyên tố tiếng Việt (Lửa, Nước, Điện, Đá, Cỏ, Tâm linh...)
    Power INT NOT NULL DEFAULT 100,             -- Chỉ số sức mạnh tấn công (HP/Power)
    Price DECIMAL(18,2) NOT NULL DEFAULT 0.00,  -- Giá bán (VNĐ)
    Description NVARCHAR(500) NULL DEFAULT N'',
    Stock INT NOT NULL DEFAULT 10,              -- Số lượng tồn kho thực tế
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (ElementType) REFERENCES dbo.Categories(Name) ON UPDATE CASCADE,
    CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Products_Power CHECK (Power >= 0 AND Power <= 10000)
);
GO

-- BẢNG 4: ORDERS (HÓA ĐƠN ĐẶT HÀNG CỦA KHÁCH)
-- Quy ước Status: 0 = Pending, 1 = Shipping, 2 = Completed, 3 = Cancelled
-- Quy ước IsPaid: 0 = Chưa thanh toán, 1 = Đã thanh toán
CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1001,1) NOT NULL,           -- Mã đơn bắt đầu từ 1001 (#ULTRA001001)
    UserId INT NULL,                            -- Khóa ngoại tùy chọn liên kết tới Users(Id)
    UserEmail NVARCHAR(150) NOT NULL,           -- Email tài khoản đặt mua
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    Status INT NOT NULL DEFAULT 0,
    IsPaid BIT NOT NULL DEFAULT 0,              -- Trạng thái thanh toán (0 = Chưa thanh toán, 1 = Đã thanh toán)
    FullName NVARCHAR(100) NOT NULL,            -- Họ tên người nhận hàng
    Address NVARCHAR(250) NOT NULL,             -- Địa chỉ giao hàng chi tiết
    PhoneNumber NVARCHAR(15) NOT NULL,          -- Số điện thoại liên hệ
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT N'Tiền mặt (COD)',
    Note NVARCHAR(500) NULL,                    -- Ghi chú của khách hàng (tùy chọn)
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_Orders_Users_UserEmail FOREIGN KEY (UserEmail) REFERENCES dbo.Users(Email) ON UPDATE CASCADE,
    CONSTRAINT FK_Orders_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Orders_Status CHECK (Status IN (0, 1, 2, 3)),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);
GO

-- BẢNG 5: ORDERDETAILS (CHI TIẾT TỪNG LÁ BÀI TRONG HÓA ĐƠN)
CREATE TABLE dbo.OrderDetails (
    Id INT IDENTITY(1,1) NOT NULL,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00, -- Lưu đơn giá tại thời điểm mua
    CONSTRAINT PK_OrderDetails PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_UnitPrice CHECK (UnitPrice >= 0)
);
GO

-- BẢNG 6: REVIEWS (ĐÁNH GIÁ SẢN PHẨM CỦA KHÁCH SAU KHI ĐƠN HÀNG HOÀN TẤT)
CREATE TABLE dbo.Reviews (
    Id INT IDENTITY(1,1) NOT NULL,
    ProductId INT NOT NULL,
    OrderId INT NOT NULL,
    ReviewerName NVARCHAR(100) NOT NULL,
    ReviewerEmail NVARCHAR(150) NOT NULL,
    Rating INT NOT NULL,
    Comment NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Reviews PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT UQ_Reviews_OrderId_ProductId UNIQUE (OrderId, ProductId), -- Chặn đánh giá trùng 1 sản phẩm trong cùng 1 đơn
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Reviews_Rating CHECK (Rating >= 1 AND Rating <= 5)
);
GO

-- 4. TẠO CÁC CHỈ MỤC (INDEXES) TỐI ƯU TỐC ĐỘ TÌM KIẾM & JOIN
CREATE NONCLUSTERED INDEX IX_Products_ElementType ON dbo.Products(ElementType);
CREATE NONCLUSTERED INDEX IX_Orders_UserId ON dbo.Orders(UserId);
CREATE NONCLUSTERED INDEX IX_Orders_UserEmail ON dbo.Orders(UserEmail);
CREATE NONCLUSTERED INDEX IX_Orders_OrderDate ON dbo.Orders(OrderDate DESC);
CREATE NONCLUSTERED INDEX IX_OrderDetails_OrderId ON dbo.OrderDetails(OrderId);
CREATE NONCLUSTERED INDEX IX_OrderDetails_ProductId ON dbo.OrderDetails(ProductId);
CREATE NONCLUSTERED INDEX IX_Reviews_ProductId ON dbo.Reviews(ProductId);
CREATE NONCLUSTERED INDEX IX_Reviews_CreatedAt ON dbo.Reviews(CreatedAt DESC);
GO

-- 5. NẠP TOÀN BỘ DỮ LIỆU MẪU (FULL SEED DATA)

-- 5.1 [ĐÃ CẬP NHẬT] Danh mục hệ nguyên tố Pokémon đồng bộ 100% tiếng Việt
SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (Id, Name, Description) VALUES
(1,  N'Lửa',       N'Thẻ bài Pokémon hệ Lửa rực cháy'),
(2,  N'Nước',      N'Thẻ bài Pokémon hệ Nước linh hoạt'),
(3,  N'Điện',      N'Thẻ bài Pokémon hệ Điện tốc độ cao'),
(4,  N'Đá',        N'Thẻ bài Pokémon hệ Đá phòng thủ vững chắc'),
(5,  N'Cỏ',        N'Thẻ bài Pokémon hệ Cỏ bền bỉ'),
(6,  N'Tâm linh',  N'Thẻ bài Pokémon hệ Tâm Linh huyền bí'),
(7,  N'Giác đấu',  N'Thẻ bài Pokémon hệ Giác Đấu cận chiến mạnh mẽ'),
(8,  N'Thường',    N'Thẻ bài Pokémon hệ Thường đa dụng'),
(9,  N'Bóng tối',  N'Thẻ bài Pokémon hệ Bóng Tối bí ẩn'),
(10, N'Thép',      N'Thẻ bài Pokémon hệ Thép phòng ngự kiên cố'),
(11, N'Rồng',      N'Thẻ bài Pokémon hệ Rồng uy lực tối thượng');
SET IDENTITY_INSERT dbo.Categories OFF;
GO

-- 5.2 Tài khoản mẫu phân quyền (Đồng bộ định dạng đuôi .vn với Program.cs & RegisterViewModel)
-- Mật khẩu mặc định: 123456 (tự động đồng bộ hash chuẩn khi khởi chạy ứng dụng C#)
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (Id, Username, FullName, Email, PasswordHash, Role, IsApproved, IsActive, CreatedAt, AvatarUrl) VALUES
(1, N'admin',      N'Quản Trị Viên',   N'admin@ultrapokemon.vn',    N'SEED_SYNC_123456', N'Admin',    1, 1, '2026-11-01 08:00:00', NULL),
(2, N'staff',      N'Nhân Viên Kho',   N'staff@ultrapokemon.vn',    N'SEED_SYNC_123456', N'Staff',    1, 1, '2026-11-01 08:05:00', NULL),
(3, N'satoshi',    N'Satoshi Ketchum', N'customer@ultrapokemon.vn', N'SEED_SYNC_123456', N'Customer', 1, 1, '2026-11-02 09:00:00', NULL),
(4, N'huannguyen', N'Nguyễn Văn Huấn', N'user@ultrapokemon.vn',     N'SEED_SYNC_123456', N'Customer', 1, 1, '2026-11-02 09:30:00', NULL);
SET IDENTITY_INSERT dbo.Users OFF;
GO

-- 5.3 [ĐÃ CẬP NHẬT] Danh sách 15 thẻ bài Pokémon mẫu (Đồng bộ ElementType tiếng Việt & bổ sung hệ Tâm linh)
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, Name, ElementType, Power, Price, Stock, Description, ImageUrl) VALUES
(1,  N'Charizard Holo', N'Lửa',      200, 450000.00, 10, N'Thẻ bài hiếm Charizard Holo hệ lửa huyền thoại, hiệu ứng ánh kim toàn bề mặt.', N'/images/Charizard.jpeg'),
(2,  N'Blastoise Holo', N'Nước',     180, 400000.00, 8,  N'Thẻ bài hiếm Blastoise Holo sở hữu 2 khẩu đại bác thủy lực dũng mãnh.', N'/images/Blastoise.png'),
(3,  N'Raichu Holo',    N'Điện',     150, 140000.00, 15, N'Thẻ bài Raichu Holo chớp nhoáng với chiếc đuôi tích điện công suất lớn.', N'/images/raichu.jpg'),
(4,  N'Jolteon VMAX',   N'Điện',     260, 320000.00, 12, N'Thẻ bài siêu hiếm Jolteon VMAX 260 HP phóng ra ngàn tia sét Max Thunder Rumble.', N'/images/Jolteon.jpg'),
(5,  N'Onix',           N'Đá',       130, 60000.00,  20, N'Rắn đá khổng lồ hệ đất đá cứng cáp, phòng thủ tuyệt hảo.', N'/images/onix.jpg'),
(6,  N'Pikachu',        N'Điện',     110, 150000.00, 25, N'Chú chuột điện vàng đáng yêu, biểu tượng của thế giới Pokémon.', N'/images/pikachu.png'),
(7,  N'Vulpix',         N'Lửa',      90,  50000.00,  18, N'Hồ ly lửa sáu đuôi xinh xắn, kiểm soát những đốm lửa ma trơi.', N'/images/Vulpix.png'),
(8,  N'Psyduck',        N'Nước',     95,  70000.00,  14, N'Vịt khờ hệ nước, bộc phát sức mạnh tâm linh kỳ bí khi đau đầu.', N'/images/psyduck.png'),
(9,  N'Charmander',     N'Lửa',      100, 80000.00,  10, N'Khủng long lửa nhỏ với ngọn lửa luôn bùng cháy nơi chóp đuôi.', N'/images/Charmander.jpeg'),
(10, N'Squirtle',       N'Nước',     105, 85000.00,  12, N'Rùa nước tinh nghịch có mai rùa cứng cáp và khả năng bắn tia nước.', N'/images/squirtle.jpg'),
(11, N'Diglett',        N'Đá',       80,  40000.00,  20, N'Pokémon chuột chũi di chuyển ngầm dưới lòng đất cực nhanh.', N'/images/Diglett.png'),
(12, N'Sandshrew',      N'Đá',       85,  45000.00,  15, N'Tê tê cát hệ đất cuộn tròn mình lại để chống đỡ đòn đánh của kẻ địch.', N'/images/sandshrew.jpg'),
(13, N'Mewtwo',         N'Tâm linh', 220, 144000.00, 5,  N'Pokémon Huyền thoại nhân tạo hệ Siêu Linh (Psychic), thuộc nhóm Pokémon Di Truyền (Genetic Pokémon).', N'/images/mewtwo.png'),
(14, N'Mew',            N'Tâm linh', 180, 120000.00, 7,  N'Pokémon Huyền Ảo mang mã gen của mọi loài Pokémon với năng lực ngoại cảm thuần khiết.', N'/images/mew.png'),
(15, N'Abra',           N'Tâm linh', 50,  45000.00,  16, N'Pokémon hệ Tâm linh sử dụng dịch chuyển tức thời ngay cả trong lúc đang ngủ.', N'/images/abra.png');
SET IDENTITY_INSERT dbo.Products OFF;
GO

-- 5.4 Đơn hàng mẫu (Phủ đủ 4 hình thức thanh toán, 4 trạng thái & trạng thái thanh toán IsPaid)
SET IDENTITY_INSERT dbo.Orders ON;
INSERT INTO dbo.Orders (Id, UserId, UserEmail, OrderDate, TotalAmount, Status, IsPaid, FullName, Address, PhoneNumber, PaymentMethod, Note) VALUES
(1001, 3, N'customer@ultrapokemon.vn', '2026-11-04 14:30:00', 600000.00, 0, 0, N'Satoshi Ketchum', N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'0912345678', N'Tiền mặt (COD)', N'Giao giờ hành chính giúp mình nhé.'),
(1002, 4, N'user@ultrapokemon.vn',     '2026-11-04 16:45:00', 540000.00, 1, 1, N'Nguyễn Văn Huấn', N'123 Đường Cầu Giấy, Hà Nội', N'0987654321', N'Chuyển khoản Ngân hàng (ATM)', N'Bọc chống sốc kỹ cho thẻ bài.'),
(1003, 3, N'customer@ultrapokemon.vn', '2026-11-05 09:15:00', 420000.00, 2, 1, N'Satoshi Ketchum', N'45 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh', N'0912345678', N'Ví Momo', NULL),
(1004, 4, N'user@ultrapokemon.vn',     '2026-11-06 10:20:00', 165000.00, 3, 0, N'Nguyễn Văn Huấn', N'123 Đường Cầu Giấy, Hà Nội', N'0987654321', N'Visa Card', N'Khách báo đổi ý đặt sang bộ khác.');
SET IDENTITY_INSERT dbo.Orders OFF;
GO

-- 5.5 Chi tiết sản phẩm từng đơn hàng
SET IDENTITY_INSERT dbo.OrderDetails ON;
INSERT INTO dbo.OrderDetails (Id, OrderId, ProductId, Quantity, UnitPrice) VALUES
(1, 1001, 1,  1, 450000.00),
(2, 1001, 6,  1, 150000.00),
(3, 1002, 2,  1, 400000.00),
(4, 1002, 3,  1, 140000.00),
(5, 1003, 4,  1, 320000.00),
(6, 1003, 7,  2, 50000.00),
(7, 1004, 9,  1, 80000.00),
(8, 1004, 10, 1, 85000.00);
SET IDENTITY_INSERT dbo.OrderDetails OFF;
GO

-- 5.6 Đánh giá sản phẩm mẫu 
SET IDENTITY_INSERT dbo.Reviews ON;
INSERT INTO dbo.Reviews (Id, ProductId, OrderId, ReviewerName, ReviewerEmail, Rating, Comment, CreatedAt) VALUES
(1, 4, 1003, N'Satoshi Ketchum', N'customer@ultrapokemon.vn', 5, N'Thẻ chính hãng, in sắc nét. Shop lồng sleeve và kẹp khung cứng chống gập rất cẩn thận.', '2026-11-06 15:20:00'),
(2, 7, 1003, N'Satoshi Ketchum', N'customer@ultrapokemon.vn', 5, N'Thẻ mới đẹp, không trầy xước. Giao hàng nhanh đúng hẹn.', '2026-11-06 15:25:00');
SET IDENTITY_INSERT dbo.Reviews OFF;
GO

-- 6. TRUY VẤN KIỂM TRA NHANH KẾT QUẢ TẠO DỮ LIỆU
SELECT 'Categories'   AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.Categories
UNION ALL
SELECT 'Users'        AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.Users
UNION ALL
SELECT 'Products'     AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.Products
UNION ALL
SELECT 'Orders'       AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.Orders
UNION ALL
SELECT 'OrderDetails' AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.OrderDetails
UNION ALL
SELECT 'Reviews'      AS [TableName], COUNT(*) AS [TotalRows] FROM dbo.Reviews;
GO