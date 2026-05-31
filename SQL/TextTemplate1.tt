-- ============================================================
-- Модуль 2: Создание базы данных в SQL Server
-- Производство и реализация продукции
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ManufacturingDB')
    DROP DATABASE ManufacturingDB;
GO

CREATE DATABASE ManufacturingDB
    COLLATE Cyrillic_General_CI_AS;
GO

USE ManufacturingDB;
GO

-- ============================================================
-- Таблица: Roles (Роли пользователей)
-- ============================================================
CREATE TABLE Roles (
    RoleID   INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ============================================================
-- Таблица: Users (Пользователи системы)
-- ============================================================
CREATE TABLE Users (
    UserID         INT IDENTITY(1,1) PRIMARY KEY,
    Login          NVARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash   NVARCHAR(256) NOT NULL,
    RoleID         INT           NOT NULL,
    IsBlocked      BIT           NOT NULL DEFAULT 0,
    FailedAttempts INT           NOT NULL DEFAULT 0,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID)
        REFERENCES Roles(RoleID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- Таблица: Materials (Материалы)
-- ============================================================
CREATE TABLE Materials (
    MaterialID   INT IDENTITY(1,1) PRIMARY KEY,
    MaterialName NVARCHAR(100)  NOT NULL,
    UnitPrice    DECIMAL(10, 2) NOT NULL CHECK (UnitPrice >= 0),
    UnitName     NVARCHAR(20)   NOT NULL   -- кг, шт, м и т.д.
);
GO

-- ============================================================
-- Таблица: Products (Продукция)
-- ============================================================
CREATE TABLE Products (
    ProductID   INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NULL
);
GO

-- ============================================================
-- Таблица: Specification (Спецификация — норма расхода материалов)
-- ============================================================
CREATE TABLE Specification (
    SpecID     INT IDENTITY(1,1) PRIMARY KEY,
    ProductID  INT            NOT NULL,
    MaterialID INT            NOT NULL,
    Quantity   DECIMAL(10, 3) NOT NULL CHECK (Quantity > 0),
    CONSTRAINT FK_Spec_Products  FOREIGN KEY (ProductID)
        REFERENCES Products(ProductID)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_Spec_Materials FOREIGN KEY (MaterialID)
        REFERENCES Materials(MaterialID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,
    CONSTRAINT UQ_Spec_ProductMaterial UNIQUE (ProductID, MaterialID)
);
GO

-- ============================================================
-- Таблица: Customers (Заказчики)
-- ============================================================
CREATE TABLE Customers (
    CustomerID   INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName  NVARCHAR(100) NOT NULL,
    ContactName  NVARCHAR(100) NULL,
    Phone        NVARCHAR(20)  NULL,
    Email        NVARCHAR(100) NULL,
    Address      NVARCHAR(200) NULL
);
GO

-- ============================================================
-- Таблица: Orders (Заказы)
-- ============================================================
CREATE TABLE Orders (
    OrderID    INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT          NOT NULL,
    OrderDate  DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Status     NVARCHAR(50) NOT NULL DEFAULT N'Новый',
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerID)
        REFERENCES Customers(CustomerID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- Таблица: OrderItems (Состав заказа — позиции заказа)
-- ============================================================
CREATE TABLE OrderItems (
    OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID     INT            NOT NULL,
    ProductID   INT            NOT NULL,
    Quantity    INT            NOT NULL CHECK (Quantity > 0),
    UnitPrice   DECIMAL(10, 2) NOT NULL CHECK (UnitPrice >= 0),
    CONSTRAINT FK_OrderItems_Orders   FOREIGN KEY (OrderID)
        REFERENCES Orders(OrderID)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductID)
        REFERENCES Products(ProductID)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- Модуль 3: Запрос — полная стоимость заказа покупателя
-- ============================================================
-- Стоимость = количество продукции × сумма (норма расхода × цена материала)
-- ============================================================
CREATE VIEW vw_OrderTotalCost AS
SELECT
    o.OrderID,
    o.OrderDate,
    o.Status,
    c.CompanyName                    AS CustomerName,
    oi.OrderItemID,
    p.ProductName,
    oi.Quantity                      AS ProductQty,

    -- Стоимость материалов на единицу продукции
    SUM(s.Quantity * m.UnitPrice)    AS MaterialCostPerUnit,

    -- Полная стоимость позиции (кол-во × себестоимость из материалов)
    oi.Quantity * SUM(s.Quantity * m.UnitPrice) AS LineTotal

FROM Orders o
JOIN Customers  c  ON c.CustomerID  = o.CustomerID
JOIN OrderItems oi ON oi.OrderID    = o.OrderID
JOIN Products   p  ON p.ProductID   = oi.ProductID
JOIN Specification s ON s.ProductID = oi.ProductID
JOIN Materials  m  ON m.MaterialID  = s.MaterialID
GROUP BY
    o.OrderID, o.OrderDate, o.Status,
    c.CompanyName,
    oi.OrderItemID, p.ProductName, oi.Quantity;
GO

-- Итоговая стоимость заказа целиком
CREATE VIEW vw_OrderSummary AS
SELECT
    OrderID,
    OrderDate,
    Status,
    CustomerName,
    SUM(LineTotal) AS TotalOrderCost
FROM vw_OrderTotalCost
GROUP BY OrderID, OrderDate, Status, CustomerName;
GO

-- ============================================================
-- Тестовые данные
-- ============================================================

-- Роли
INSERT INTO Roles (RoleName) VALUES (N'Администратор'), (N'Пользователь');

-- Пользователи (пароль "admin123" и "user123" — MD5-хэши, в реальном ПО bcrypt)
-- Пароли в WPF будут хэшироваться через SHA256
-- admin : admin123  -> sha256
-- user  : user123   -> sha256
INSERT INTO Users (Login, PasswordHash, RoleID, IsBlocked, FailedAttempts) VALUES
(N'admin', N'240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', 1, 0, 0),
(N'user',  N'E606E38B0D8C19B24CF0EE3808183162EA7CD63FF7912DBB22B5E803286B4446', 2, 0, 0);
-- Примечание: хэши будут пересчитаны при первом запуске; в демо используется SHA256 от пароля

-- Материалы
INSERT INTO Materials (MaterialName, UnitPrice, UnitName) VALUES
(N'Сталь листовая',   85.00,  N'кг'),
(N'Алюминиевый профиль', 210.00, N'кг'),
(N'Болт М8',          2.50,   N'шт'),
(N'Краска акриловая', 180.00, N'л'),
(N'Резиновый уплотнитель', 45.00, N'м');

-- Продукция
INSERT INTO Products (ProductName, Description) VALUES
(N'Металлическая дверь',     N'Входная металлическая дверь стандартных размеров'),
(N'Стеллаж металлический',   N'Сборный металлический стеллаж для склада'),
(N'Ограждение балконное',    N'Балконное ограждение из алюминиевого профиля');

-- Спецификация (норма расхода)
INSERT INTO Specification (ProductID, MaterialID, Quantity) VALUES
-- Металлическая дверь
(1, 1, 45.000),  -- 45 кг стали
(1, 3, 20.000),  -- 20 болтов
(1, 4,  0.500),  -- 0.5 л краски
(1, 5,  3.000),  -- 3 м уплотнителя
-- Стеллаж металлический
(2, 1, 30.000),  -- 30 кг стали
(2, 3, 40.000),  -- 40 болтов
(2, 4,  0.300),  -- 0.3 л краски
-- Ограждение балконное
(3, 2, 12.000),  -- 12 кг алюминия
(3, 3, 16.000),  -- 16 болтов
(3, 4,  0.200);  -- 0.2 л краски

-- Заказчики
INSERT INTO Customers (CompanyName, ContactName, Phone, Email, Address) VALUES
(N'ООО "СтройГрупп"',      N'Иванов Алексей',  N'+7-495-123-4567', N'ivanov@stroigroup.ru', N'г. Москва, ул. Строителей, 15'),
(N'АО "МегаСтрой"',        N'Петрова Мария',   N'+7-812-987-6543', N'petrova@megastroy.ru', N'г. Санкт-Петербург, пр. Невский, 100'),
(N'ИП Сидоров В.А.',       N'Сидоров Василий', N'+7-343-555-0011', N'sidorov@mail.ru',      N'г. Екатеринбург, ул. Ленина, 5');

-- Заказы
INSERT INTO Orders (CustomerID, OrderDate, Status) VALUES
(1, '2024-03-01', N'Выполнен'),
(1, '2024-04-15', N'В работе'),
(2, '2024-04-20', N'Новый'),
(3, '2024-05-01', N'В работе');

-- Состав заказов
INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPrice) VALUES
(1, 1, 5,  3900.00),  -- 5 дверей
(1, 2, 3,  4500.00),  -- 3 стеллажа
(2, 1, 2,  3900.00),  -- 2 двери
(2, 3, 10, 2800.00),  -- 10 ограждений
(3, 2, 8,  4500.00),  -- 8 стеллажей
(4, 1, 3,  3900.00),  -- 3 двери
(4, 3, 5,  2800.00);  -- 5 ограждений

GO

-- ============================================================
-- Проверочный запрос из Модуля 3
-- ============================================================
SELECT * FROM vw_OrderSummary ORDER BY OrderID;
GO
