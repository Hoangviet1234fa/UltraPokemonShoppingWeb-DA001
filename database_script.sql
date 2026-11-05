-- =======================================================================
-- ĐỒ ÁN MÔN HỌC: LẬP TRÌNH TRÊN WEB (WEB PROGRAMMING)
-- ĐỀ TÀI: ONLINE SHOPPING WEBSITE - ULTRA POKEMON TCG STORE
-- DATABASE SCRIPT CHO SQL SERVER
-- =======================================================================

CREATE DATABASE UltraPokemonDB;
GO

USE UltraPokemonDB;
GO

-- 1. BẢNG CATEGORIES (HỆ POKÉMON)
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);
GO

-- 2. BẢNG USERS (TÀI KHOẢN VÀ PHÂN QUYỀN)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT 'User',
    IsApproved BIT NOT NULL DEFAULT 1,
    AvatarUrl NVARCHAR(255) NULL
);
GO

-- 3. BẢNG PRODUCTS (THẺ BÀI POKÉMON VÀ TỒN KHO)
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    ElementType NVARCHAR(50) NOT NULL,
    Power INT NOT NULL DEFAULT 100,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Stock INT NOT NULL DEFAULT 10
);
GO

-- 4. BẢNG ORDERS (HÓA ĐƠN ĐẶT HÀNG)
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserEmail NVARCHAR(100) NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Shipping, 2: Completed, 3: Cancelled
    FullName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL DEFAULT N'Tiền mặt'
);
GO

-- 5. BẢNG ORDERDETAILS (CHI TIẾT MÓN HÀNG TRONG HÓA ĐƠN)
CREATE TABLE OrderDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- =======================================================================
-- NẠP DỮ LIỆU MẪU (SEED DATA)
-- =======================================================================

-- Nạp Categories
INSERT INTO Categories (Name) VALUES 
(N'Fire'), (N'Water'), (N'Electric'), (N'Rock'), (N'Grass'), (N'Psychic');
GO

-- Nạp Tài khoản Admin và User (Mật khẩu mặc định: 123)
INSERT INTO Users (Email, PasswordHash, Role, IsApproved) VALUES 
('admin@ultrapokemon.com', 'AQAAAAIAAYagAAAAEOvE+gQd17WvjEGB/k1L9j8GvJkK...', 'Admin', 1),
('user@ultrapokemon.com', 'AQAAAAIAAYagAAAAEOvE+gQd17WvjEGB/k1L9j8GvJkK...', 'User', 1);
GO

-- Nạp Thẻ bài mẫu
INSERT INTO Products (Name, ElementType, Power, Price, Stock, Description, ImageUrl) VALUES
(N'Charizard', N'Fire', 200, 450000, 10, N'Lá bài lửa huyền thoại', '/images/Charizard.jpeg'),
(N'Blastoise', N'Water', 180, 400000, 8, N'Lá bài nước mạnh mẽ', '/images/Blastoise.png'),
(N'Raichu', N'Electric', 150, 120000, 15, N'Lá bài điện chớp nhoáng', '/images/raichu.jpg'),
(N'Jolteon', N'Electric', 140, 90000, 12, N'Tiến hóa hệ điện', '/images/Jolteon.jpg'),
(N'Onix', N'Rock', 130, 60000, 20, N'Hệ đá cứng cáp', '/images/onix.jpg'),
(N'Pikachu', N'Electric', 110, 150000, 25, N'Chú chuột điện đáng yêu', '/images/pikachu.png'),
(N'Vulpix', N'Fire', 90, 50000, 18, N'Cáo lửa nhỏ', '/images/Vulpix.png'),
(N'Psyduck', N'Water', 95, 70000, 14, N'Vịt khờ hệ nước', '/images/psyduck.png'),
(N'Charmander', N'Fire', 100, 80000, 10, N'Khủng long lửa nhỏ', '/images/Charmander.jpeg'),
(N'Squirtle', N'Water', 105, 85000, 12, N'Rùa nước tinh nghịch', '/images/squirtle.jpg'),
(N'Diglett', N'Rock', 80, 40000, 20, N'Pokemon chuột chũi', '/images/Diglett.png'),
(N'Sandshrew', N'Rock', 85, 45000, 15, N'Tê tê cát', '/images/sandshrew.jpg');
GO
