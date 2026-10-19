-- สคริปต์สร้างฐานข้อมูล ShopDb บน SQL Server พร้อม Mock up Product / Stock data
-- (ไม่จำเป็นต้องรันเอง: Backend จะสร้างฐานข้อมูลให้อัตโนมัติตอนเริ่มทำงานถ้ายังไม่มี)

IF DB_ID('ShopDb') IS NULL CREATE DATABASE ShopDb;
GO
USE ShopDb;
GO

CREATE TABLE Products (
    ProductCode NVARCHAR(20)  NOT NULL PRIMARY KEY,
    Name        NVARCHAR(200) NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL
);

CREATE TABLE Stocks (
    ProductCode NVARCHAR(20) NOT NULL PRIMARY KEY
        REFERENCES Products(ProductCode) ON DELETE CASCADE,
    Quantity    INT NOT NULL,          -- จำนวนคงเหลือในสต๊อก
    Reserved    INT NOT NULL,          -- จำนวนที่ถูกจองในตะกร้า
    CONSTRAINT CK_Stock_Quantity CHECK (Quantity >= 0),
    CONSTRAINT CK_Stock_Reserved CHECK (Reserved >= 0 AND Reserved <= Quantity)
);

CREATE TABLE CartItems (
    Id          INT IDENTITY PRIMARY KEY,
    CartId      UNIQUEIDENTIFIER NOT NULL,
    ProductCode NVARCHAR(20) NOT NULL REFERENCES Products(ProductCode) ON DELETE CASCADE,
    Quantity    INT NOT NULL,
    CONSTRAINT UX_CartItems_Cart_Product UNIQUE (CartId, ProductCode)
);

CREATE TABLE Orders (
    Id          INT IDENTITY PRIMARY KEY,
    CartId      UNIQUEIDENTIFIER NOT NULL,
    CreatedAt   DATETIME2 NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL
);

CREATE TABLE OrderItems (
    Id          INT IDENTITY PRIMARY KEY,
    OrderId     INT NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    ProductCode NVARCHAR(20)  NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL,
    Quantity    INT NOT NULL,
    LineTotal   DECIMAL(18,2) NOT NULL
);

INSERT INTO Products (ProductCode, Name, UnitPrice) VALUES
('P001', N'Mechanical Keyboard',         2590.00),
('P002', N'Wireless Mouse',               890.00),
('P003', N'27" Monitor',                 6990.00),
('P004', N'USB-C Hub',                   1250.00),
('P005', N'Noise Cancelling Headphones', 4990.00),
('P006', N'Webcam 1080p',                1490.00);

INSERT INTO Stocks (ProductCode, Quantity, Reserved) VALUES
('P001', 10, 0), ('P002', 25, 0), ('P003', 3, 0),
('P004', 15, 0), ('P005', 5, 0),  ('P006', 0, 0);
GO
