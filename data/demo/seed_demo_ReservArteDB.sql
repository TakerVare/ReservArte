-- =============================================================================
-- ReservArte · DATOS DEMO para desarrollo (solo datos: DML)
-- =============================================================================
-- ⚠ SOLO DESARROLLO: contraseñas conocidas. NUNCA ejecutar en producción.
--
-- Requisito: base creada con data/schema/create_ReservArteDB.sql.
-- Alineado con el esquema de la migración 20260915112149_AddCustomers.
-- Idempotente: si ya existe alguna organización no inserta nada (mismo criterio
-- que DevSeeder). Todo va en una transacción: o entra entero o no entra nada.
--
-- Cuentas demo (las mismas que crea DevSeeder, con las mismas contraseñas):
--   guille@svalero.com              Admin1234!   Admin     (sin ficha de empleado)
--   maria.garcia@reservarte.com     Maria123!    Employee
--   lucia.martinez@reservarte.com   Lucia123!    Employee
--   carmen.lopez@example.com        Cliente123!  Customer  (ficha de clienta VIP)
--   sofia.ruiz@example.com          Cliente123!  Customer  (ficha de clienta)
-- Los PasswordHash son del PasswordHasher de ASP.NET Core Identity (PBKDF2).
--
-- Mantenimiento: si una migración cambia una tabla que este script siembra, se
-- actualiza en el mismo PR. Ver data/README.md.
-- =============================================================================

USE ReservArteDB;
GO

SET NOCOUNT ON;
-- Obligatorio: AspNetUsers tiene índices filtrados (EmailIndex, UserNameIndex).
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (SELECT 1 FROM Organizations)
BEGIN
    PRINT 'Ya hay organizaciones en ReservArteDB: no se insertan datos demo.';
    -- Salta el resto de lotes hasta SET NOEXEC OFF.
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Org UNIQUEIDENTIFIER = 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE';

-- ── Organización ─────────────────────────────────────────────────────────────
INSERT INTO Organizations (Id, Name, Subdomain, Email, Phone, Country, IsActive, CreatedAt)
VALUES (@Org, N'More Than Brows', N'morethanbrows', N'info@morethanbrows.com', N'+34600000000', N'ES', 1, SYSUTCDATETIME());

-- ── Cuentas de acceso (AspNetUsers) ──────────────────────────────────────────
-- Ids explícitos: la ficha de empleado comparte el Id de su cuenta.
SET IDENTITY_INSERT AspNetUsers ON;

INSERT INTO AspNetUsers (
    Id, OrganizationId, FirstName, LastName, Email, NormalizedEmail, UserName, NormalizedUserName,
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
    TwoFactorEnabled, LockoutEnabled, AccessFailedCount, Rol, CreatedAt)
VALUES
    (1, @Org, N'Guillermo', N'Admin', N'guille@svalero.com', N'GUILLE@SVALERO.COM', N'guille@svalero.com', N'GUILLE@SVALERO.COM', 1, N'AQAAAAIAAYagAAAAEJ9fwqD60nfRFIDsqQNAgUA5L//w6rW3oCdBZ3F9vyUj+n9yg5uxZ6NhNKr9nFSJAA==', N'A7AYE46V5ZPK54TLROYCLZDZD3HGMKDQ', N'7144748b-0c5d-49af-a59e-e3759243a156', N'+34600000001', 0, 0, 1, 0, N'Admin', SYSUTCDATETIME()),
    (2, @Org, N'María', N'García', N'maria.garcia@reservarte.com', N'MARIA.GARCIA@RESERVARTE.COM', N'maria.garcia@reservarte.com', N'MARIA.GARCIA@RESERVARTE.COM', 1, N'AQAAAAIAAYagAAAAENWuldHEn9f0gfdTc5VJcxrXK8TulE3DhRJA8wVBEzcX8CU8RNTx7cqXqXWI92e2ZQ==', N'5GXEUDADWVSWXKOM3TDCCRKCV7ZVAOVC', N'6d38ef73-2223-47ad-976d-bdb2fdf0cf55', N'+34600000002', 0, 0, 1, 0, N'Employee', SYSUTCDATETIME()),
    (3, @Org, N'Lucía', N'Martínez', N'lucia.martinez@reservarte.com', N'LUCIA.MARTINEZ@RESERVARTE.COM', N'lucia.martinez@reservarte.com', N'LUCIA.MARTINEZ@RESERVARTE.COM', 1, N'AQAAAAIAAYagAAAAEFhCwLkQrulXAFERzXr3koCGgnQ74Z+ybU71l9dR5HyaLXvGd5qWi+znJTBWxBe/IQ==', N'SDYQAPXHAMAA3NBX7M2LDZAXZOGOJW5Q', N'98b47c35-0da7-4efc-9191-1736be2ee035', N'+34600000003', 0, 0, 1, 0, N'Employee', SYSUTCDATETIME()),
    (4, @Org, N'Carmen', N'López', N'carmen.lopez@example.com', N'CARMEN.LOPEZ@EXAMPLE.COM', N'carmen.lopez@example.com', N'CARMEN.LOPEZ@EXAMPLE.COM', 1, N'AQAAAAIAAYagAAAAEIpf+2PwrKqGm+TkpYF+kB25tiQkhNjWyAxCMy5qDE8fBIjLJizzo1yyeD1p0MCPSw==', N'LF5EIFMGOLBAHHR4XLXI2LJ2MOV6UFM2', N'622f982f-6ae3-4413-ac05-f153196d6ae0', N'+34600000004', 0, 0, 1, 0, N'Customer', SYSUTCDATETIME()),
    (5, @Org, N'Sofía', N'Ruiz', N'sofia.ruiz@example.com', N'SOFIA.RUIZ@EXAMPLE.COM', N'sofia.ruiz@example.com', N'SOFIA.RUIZ@EXAMPLE.COM', 1, N'AQAAAAIAAYagAAAAEHdz3c0vLZhDNYlsW9mSrVb5aW4f9NyXPccM+cZGkI4paaZEiCoXxvL9fy3otWZ5WQ==', N'LI5VHQPUIFGV7CBI25UZG6A4CSH6WUZS', N'898793f5-79ed-4f1d-8e74-bcf24231a270', N'+34600000005', 0, 0, 1, 0, N'Customer', SYSUTCDATETIME());

SET IDENTITY_INSERT AspNetUsers OFF;

-- ── Fichas de empleado (Id = Id de la cuenta) ────────────────────────────────
INSERT INTO Employees (Id, OrganizationId, FirstName, LastName, Email, Phone, Rol, HireDate, IsActive, CreatedAt)
VALUES
    (2, @Org, N'María', N'García', N'maria.garcia@reservarte.com', N'+34600000002', N'Employee', '2024-03-01', 1, SYSUTCDATETIME()),
    (3, @Org, N'Lucía', N'Martínez', N'lucia.martinez@reservarte.com', N'+34600000003', N'Employee', '2025-01-15', 1, SYSUTCDATETIME());

-- ── Horario semanal demo ─────────────────────────────────────────────────────
-- Convención del proyecto: 0 = lunes … 6 = domingo (RA-869d7ezrr). NO es
-- System.DayOfWeek, que empieza en domingo.
INSERT INTO EmployeeAvailabilities (OrganizationId, EmployeeId, DayOfWeek, StartTime, EndTime, IsRecurring, IsActive, CreatedAt)
VALUES
    (@Org, 2, 0, '09:00', '18:00', 1, 1, SYSUTCDATETIME()),  -- María, lunes
    (@Org, 2, 1, '09:00', '18:00', 1, 1, SYSUTCDATETIME()),  -- María, martes
    (@Org, 2, 2, '09:00', '18:00', 1, 1, SYSUTCDATETIME()),  -- María, miércoles
    (@Org, 2, 3, '09:00', '18:00', 1, 1, SYSUTCDATETIME()),  -- María, jueves
    (@Org, 2, 4, '09:00', '14:00', 1, 1, SYSUTCDATETIME()),  -- María, viernes
    (@Org, 3, 0, '10:00', '19:00', 1, 1, SYSUTCDATETIME()),  -- Lucía, lunes
    (@Org, 3, 1, '10:00', '19:00', 1, 1, SYSUTCDATETIME()),  -- Lucía, martes
    (@Org, 3, 2, '10:00', '19:00', 1, 1, SYSUTCDATETIME()),  -- Lucía, miércoles
    (@Org, 3, 3, '10:00', '19:00', 1, 1, SYSUTCDATETIME()),  -- Lucía, jueves
    (@Org, 3, 4, '10:00', '15:00', 1, 1, SYSUTCDATETIME());  -- Lucía, viernes

-- ── Fichas de clienta (Id = Id de la cuenta; RA-869d7f32r) ──────────────────
INSERT INTO Customers (Id, OrganizationId, FirstName, LastName, Email, Phone, Category, LoyaltyPoints, IsBlocked, PreferredContactMethod, IsActive, CreatedAt)
VALUES
    (4, @Org, N'Carmen', N'López', N'carmen.lopez@example.com', N'+34600000004', N'vip', 0, 0, N'email', 1, SYSUTCDATETIME()),
    (5, @Org, N'Sofía', N'Ruiz', N'sofia.ruiz@example.com', N'+34600000005', N'regular', 0, 0, N'email', 1, SYSUTCDATETIME());

-- Tratamiento de datos (obligatorio) para las dos; Carmen acepta además marketing.
INSERT INTO CustomerConsents (OrganizationId, CustomerId, ConsentType, IsGranted, GrantedAt, IsActive, CreatedAt)
VALUES
    (@Org, 4, N'data_processing', 1, SYSUTCDATETIME(), 1, SYSUTCDATETIME()),
    (@Org, 4, N'marketing', 1, SYSUTCDATETIME(), 1, SYSUTCDATETIME()),
    (@Org, 5, N'data_processing', 1, SYSUTCDATETIME(), 1, SYSUTCDATETIME());

INSERT INTO CustomerAllergies (OrganizationId, CustomerId, AllergyDescription, Severity, IsActive, CreatedAt)
VALUES (@Org, 4, N'Látex', N'high', 1, SYSUTCDATETIME());

-- Nota interna escrita por María (EmployeeId 2).
INSERT INTO CustomerNotes (OrganizationId, CustomerId, EmployeeId, Note, IsActive, CreatedAt)
VALUES (@Org, 4, 2, N'Prefiere citas por la tarde.', 1, SYSUTCDATETIME());

COMMIT TRANSACTION;

PRINT 'Datos demo insertados en ReservArteDB.';
GO

SET NOEXEC OFF;
GO
