-- =============================================================================
-- ⚠ ADVERTENCIA (2026-07-06, RA-869d7eyvf): Este script describe el esquema
-- PRE-Identity (tabla Users con Password/Phone). El esquema AUTORITATIVO es
-- el generado por las migraciones EF Core (AspNetUsers, AspNetUserLogins,
-- AspNetUserClaims, AspNetUserTokens). Este fichero requiere actualización
-- o retirada para alinearse con Identity. No ejecutar en entornos que usen
-- dotnet ef database update. Ver vol. 1 §5.2 y §4.4.1.
-- =============================================================================
-- ReservArte: creación de la base de datos y esquema (solo DDL)
-- Ejecutar en SQL Server con permisos para crear BD.
-- Orden sugerido: 1) drop_ReservArteDB.sql (opcional) 2) este fichero 3) seed_ReservArteDB.sql
-- Diagrama ERD: Documentation/reservarte-memoria-1-analisis.md §5.2.1 y §5.2.2
-- Cambios v2 (histórico, pre-Identity):
--   - Password NVARCHAR(255) en Users (sustituido por PasswordHash en Identity)
--   - UpdatedAt añadido a todas las tablas que tenían CreatedAt sin él
--   - Configuration convertida a singleton (PK INT DEFAULT 1 con CHECK)
--   - ServicePhotos: S3Key/S3Bucket reemplazados por CloudinaryPublicId/CloudinarySecureUrl
--     (alineado con §3.1.8 y §4.1.1: almacenamiento de medios en Cloudinary)
-- =============================================================================

USE master;
GO

IF DB_ID(N'ReservArteDB') IS NULL
BEGIN
    CREATE DATABASE ReservArteDB;
END
GO

USE ReservArteDB;
GO

PRINT 'Usando base de datos ReservArteDB';

-- ============================================
-- TABLA USERS
-- ============================================
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,                          -- ← v2: era NVARCHAR(100)
    Rol NVARCHAR(50) NOT NULL CHECK (Rol IN ('admin', 'employee', 'client')),
    Phone NVARCHAR(20) NULL,
    ProfileImageUrl NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL                                  -- ← v2: añadido
);

-- ============================================
-- TABLA CUSTOMERS (Id = User.Id para unificar identificador)
-- ============================================
CREATE TABLE Customers (
    Id INT NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    Rol NVARCHAR(50) NOT NULL DEFAULT 'client',
    ProfileImageUrl NVARCHAR(500) NULL,
    BirthDate DATE NULL,
    Category NVARCHAR(50) NOT NULL DEFAULT 'regular' CHECK (Category IN ('regular', 'vip', 'blocked')),
    LoyaltyPoints INT NOT NULL DEFAULT 0,
    IsBlocked BIT NOT NULL DEFAULT 0,
    BlockedReason NVARCHAR(500) NULL,
    PreferredContactMethod NVARCHAR(50) NOT NULL DEFAULT 'email' CHECK (PreferredContactMethod IN ('email', 'phone', 'sms', 'whatsapp')),
    MarketingConsent BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (Id) REFERENCES Users(Id) ON DELETE CASCADE
);

-- ============================================
-- TABLA EMPLOYEES (Id = User.Id para unificar identificador)
-- ============================================
CREATE TABLE Employees (
    Id INT NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    Phone NVARCHAR(20) NULL,
    Rol NVARCHAR(50) NOT NULL DEFAULT 'employee',
    ProfileImageUrl NVARCHAR(500) NULL,
    HireDate DATE NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),        -- ← v2: añadido
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (Id) REFERENCES Users(Id) ON DELETE CASCADE
);

-- ============================================
-- RESTO DE TABLAS
-- ============================================

CREATE TABLE ServiceCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Color NVARCHAR(20) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL                                  -- ← v2: añadido
);

CREATE TABLE Services (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DurationMinutes INT NOT NULL,
    BasePrice DECIMAL(10,2) NOT NULL,
    CategoryId INT NULL,
    ImageUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    RequiresAllergyTest BIT NOT NULL DEFAULT 0,
    AllergyTestHoursBefore INT NOT NULL DEFAULT 48,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (CategoryId) REFERENCES ServiceCategories(Id)
);

CREATE TABLE ServiceVariations (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ServiceId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    PriceModifier DECIMAL(10,2) NOT NULL DEFAULT 0,
    DurationModifier INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE
);

CREATE TABLE ServicePricings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ServiceId INT NOT NULL,
    EmployeeLevel NVARCHAR(50) NOT NULL CHECK (EmployeeLevel IN ('Junior', 'Senior', 'Expert')),
    Price DECIMAL(10,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

CREATE TABLE ProductCategories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL                                  -- ← v2: añadido
);

CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Brand NVARCHAR(100) NULL,
    Sku NVARCHAR(100) NULL UNIQUE,
    Price DECIMAL(10,2) NOT NULL DEFAULT 0,
    Stock INT NOT NULL DEFAULT 0,
    MinStockAlert INT NOT NULL DEFAULT 5,
    ImageUrl NVARCHAR(500) NULL,
    CategoryId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (CategoryId) REFERENCES ProductCategories(Id)
);

CREATE TABLE ServiceProducts (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ServiceId INT NOT NULL,
    ProductId INT NOT NULL,
    QuantityUsed DECIMAL(10,2) NULL,
    Notes NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),        -- ← v2: añadido
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE ServicePackages (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    DiscountPercentage DECIMAL(5,2) NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE ServicePackageItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ServicePackageId INT NOT NULL,
    ServiceId INT NOT NULL,
    [Order] INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (ServicePackageId) REFERENCES ServicePackages(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

CREATE TABLE ServicePromotions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ServiceId INT NULL,
    ServicePackageId INT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    DiscountPercentage DECIMAL(5,2) NOT NULL,
    DiscountAmount DECIMAL(10,2) NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsSeasonalService BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    FOREIGN KEY (ServicePackageId) REFERENCES ServicePackages(Id),
    CHECK ((ServiceId IS NOT NULL AND ServicePackageId IS NULL) OR (ServiceId IS NULL AND ServicePackageId IS NOT NULL))
);

CREATE TABLE EmployeeAvailabilities (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    DayOfWeek INT NOT NULL CHECK (DayOfWeek BETWEEN 0 AND 6),
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsRecurring BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),        -- ← v2: añadido
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE EmployeeExceptions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    StartDateTime DATETIME2 NOT NULL,
    EndDateTime DATETIME2 NOT NULL,
    Reason NVARCHAR(500) NULL,
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('vacation', 'sick_leave', 'personal', 'training', 'other')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),        -- ← v2: añadido
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE EmployeeServices (
    EmployeeId INT NOT NULL,
    ServiceId INT NOT NULL,
    ProficiencyLevel INT NOT NULL DEFAULT 1 CHECK (ProficiencyLevel BETWEEN 1 AND 5),
    IsActive BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (EmployeeId, ServiceId),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);

CREATE TABLE Appointments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    EmployeeId INT NOT NULL,
    AppointmentDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'pending' CHECK (Status IN ('pending', 'confirmed', 'in_progress', 'completed', 'cancelled', 'cancelled_by_customer', 'cancelled_by_business', 'no_show')),
    TotalPrice DECIMAL(10,2) NOT NULL,
    DepositAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    RedsysOrderNumber NVARCHAR(50) NULL,
    RedsysPreAuthToken NVARCHAR(200) NULL,
    PaymentMethodId INT NULL,
    CancellationReason NVARCHAR(500) NULL,
    CancelledAt DATETIME2 NULL,
    CancelledById INT NULL,
    CancelledByType NVARCHAR(50) NULL CHECK (CancelledByType IN ('customer', 'business', 'system')),
    Notes NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE AppointmentServiceItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AppointmentId INT NOT NULL,
    ServiceId INT NOT NULL,
    ServiceVariationId INT NULL,
    Price DECIMAL(10,2) NOT NULL,
    DurationMinutes INT NOT NULL,
    [Order] INT NOT NULL,
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id) ON DELETE CASCADE,
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    FOREIGN KEY (ServiceVariationId) REFERENCES ServiceVariations(Id)
);

CREATE TABLE CustomerPaymentMethods (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    RedsysToken NVARCHAR(200) NOT NULL,
    RedsysCofTxnid NVARCHAR(100) NULL,
    CardLast4 NVARCHAR(4) NOT NULL,
    CardBrand NVARCHAR(50) NOT NULL,
    CardExpiry NVARCHAR(4) NOT NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE Payments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AppointmentId INT NULL,
    CustomerId INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'EUR',
    PaymentMethodType NVARCHAR(50) NOT NULL CHECK (PaymentMethodType IN ('card', 'cash', 'transfer', 'bizum')),
    Status NVARCHAR(50) NOT NULL DEFAULT 'pending' CHECK (Status IN ('pending', 'authorized', 'captured', 'failed', 'cancelled', 'refunded', 'partially_refunded')),
    RedsysOrderNumber NVARCHAR(50) NULL UNIQUE,
    RedsysAuthCode NVARCHAR(50) NULL,
    RedsysResponse NVARCHAR(MAX) NULL,
    RedsysTransactionType NVARCHAR(10) NULL,
    RedsysCardNumber NVARCHAR(19) NULL,
    CustomerPaymentMethodId INT NULL,
    ProcessedAt DATETIME2 NULL,
    RefundedAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    RefundedAt DATETIME2 NULL,
    Metadata NVARCHAR(MAX) NULL,
    Notes NVARCHAR(500) NULL,
    RegisteredById INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (CustomerPaymentMethodId) REFERENCES CustomerPaymentMethods(Id)
);

CREATE TABLE CustomerNotes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    EmployeeId INT NOT NULL,
    Note NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE CustomerAllergies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    AllergyDescription NVARCHAR(500) NOT NULL,
    Severity NVARCHAR(50) NOT NULL CHECK (Severity IN ('Low', 'Medium', 'High')),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE CustomerConsents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    ConsentType NVARCHAR(100) NOT NULL,
    IsGranted BIT NOT NULL,
    GrantedAt DATETIME2 NULL,
    RevokedAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE ServicePhotos (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AppointmentId INT NOT NULL,
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('before', 'after', 'process')),
    CloudinaryPublicId NVARCHAR(500) NOT NULL,      -- ← v2: era S3Key (almacenamiento migrado a Cloudinary)
    CloudinarySecureUrl NVARCHAR(500) NOT NULL,     -- ← v2: era S3Bucket
    UploadedBy INT NOT NULL,
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsPublic BIT NOT NULL DEFAULT 0,
    ExpiresAt DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),
    FOREIGN KEY (UploadedBy) REFERENCES Employees(Id)
);

CREATE TABLE CancellationPolicies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MinHoursBeforeCancel INT NOT NULL DEFAULT 24,
    PenaltyPercentage INT NOT NULL DEFAULT 0 CHECK (PenaltyPercentage BETWEEN 0 AND 100),
    MaxNoShowsBeforeBlock INT NOT NULL DEFAULT 3,
    VipMinHoursBeforeCancel INT NULL,
    VipPenaltyPercentage INT NULL CHECK (VipPenaltyPercentage BETWEEN 0 AND 100),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE WaitingList (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL,
    ServiceId INT NOT NULL,
    PreferredEmployeeId INT NULL,
    PreferredDate DATETIME2 NULL,
    DateRangeStart DATETIME2 NOT NULL,
    DateRangeEnd DATETIME2 NOT NULL,
    Priority INT NOT NULL DEFAULT 1000,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    NotifiedAt DATETIME2 NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id),
    FOREIGN KEY (PreferredEmployeeId) REFERENCES Employees(Id)
);

CREATE TABLE ProductSales (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NULL,
    AppointmentId INT NULL,
    TotalAmount DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Completed', 'Cancelled')),
    PaymentMethod NVARCHAR(50) NULL,
    Notes NVARCHAR(MAX) NULL,
    SoldBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id),
    FOREIGN KEY (SoldBy) REFERENCES Employees(Id)
);

CREATE TABLE ProductSaleItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SaleId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (SaleId) REFERENCES ProductSales(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE InventoryMovements (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    MovementType NVARCHAR(50) NOT NULL CHECK (MovementType IN ('purchase', 'sale', 'adjustment', 'waste', 'return')),
    ReferenceId INT NULL,
    Notes NVARCHAR(500) NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (CreatedBy) REFERENCES Employees(Id)
);

CREATE TABLE MessageTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Type NVARCHAR(100) NOT NULL CHECK (Type IN ('email', 'sms', 'whatsapp', 'push')),
    Subject NVARCHAR(500) NULL,
    Body NVARCHAR(MAX) NOT NULL,
    Language NVARCHAR(10) NOT NULL DEFAULT 'es',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),        -- ← v2: añadido
    UpdatedAt DATETIME2 NULL                                  -- ← v2: añadido
);

CREATE TABLE ReminderConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ReminderOrder INT NOT NULL,
    HoursBeforeAppointment INT NOT NULL,
    Channel NVARCHAR(50) NOT NULL CHECK (Channel IN ('email', 'sms', 'whatsapp', 'push')),
    IsActive BIT NOT NULL DEFAULT 1,
    MessageTemplateId UNIQUEIDENTIFIER NOT NULL,
    AllowedSendStartTime TIME NULL,
    AllowedSendEndTime TIME NULL,
    FOREIGN KEY (MessageTemplateId) REFERENCES MessageTemplates(Id)
);

CREATE TABLE ReminderLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AppointmentId INT NOT NULL,
    ReminderConfigurationId UNIQUEIDENTIFIER NOT NULL,
    Channel NVARCHAR(50) NOT NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('pending', 'sent', 'failed', 'delivered', 'read')),
    ExternalMessageId NVARCHAR(200) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id) ON DELETE CASCADE,
    FOREIGN KEY (ReminderConfigurationId) REFERENCES ReminderConfigurations(Id)
);

CREATE TABLE ConfirmationTokens (
    Token NVARCHAR(200) PRIMARY KEY,
    AppointmentId INT NOT NULL,
    Action NVARCHAR(50) NOT NULL CHECK (Action IN ('confirm', 'cancel')),
    ExpiresAt DATETIME2 NOT NULL,
    UsedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id) ON DELETE CASCADE
);

-- ============================================
-- TABLA CONFIGURATION (singleton)
-- ← v2: PK cambiada a INT DEFAULT 1 con CHECK para garantizar una sola fila
-- ============================================
CREATE TABLE Configuration (
    Id INT PRIMARY KEY DEFAULT 1,
    CompanyName NVARCHAR(150),
    CompanyLogo NVARCHAR(250),
    UpdatedAt DATETIME2 NULL,                                 -- ← v2: añadido
    CONSTRAINT CHK_Configuration_SingleRow CHECK (Id = 1)
);

PRINT 'Esquema ReservArteDB creado correctamente (v2 — Cloudinary)';
GO