-- =============================================================================
-- ReservArte · DATOS DEMO para desarrollo (solo datos: DML)
-- =============================================================================
-- ⚠ SOLO DESARROLLO: contraseñas conocidas. NUNCA ejecutar en producción.
--
-- Requisito: base creada con data/schema/create_ReservArteDB.sql.
-- Alineado con el esquema de la migración 20260913193719_NormalizeRolesToPascalCase.
-- Idempotente: si ya existe alguna organización no inserta nada (mismo criterio
-- que DevSeeder). Todo va en una transacción: o entra entero o no entra nada.
--
-- Cuentas demo (las mismas que crea DevSeeder, con las mismas contraseñas):
--   guille@svalero.com              Admin1234!   Admin     (sin ficha de empleado)
--   maria.garcia@reservarte.com     Maria123!    Employee
--   lucia.martinez@reservarte.com   Lucia123!    Employee
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
    (3, @Org, N'Lucía', N'Martínez', N'lucia.martinez@reservarte.com', N'LUCIA.MARTINEZ@RESERVARTE.COM', N'lucia.martinez@reservarte.com', N'LUCIA.MARTINEZ@RESERVARTE.COM', 1, N'AQAAAAIAAYagAAAAEFhCwLkQrulXAFERzXr3koCGgnQ74Z+ybU71l9dR5HyaLXvGd5qWi+znJTBWxBe/IQ==', N'SDYQAPXHAMAA3NBX7M2LDZAXZOGOJW5Q', N'98b47c35-0da7-4efc-9191-1736be2ee035', N'+34600000003', 0, 0, 1, 0, N'Employee', SYSUTCDATETIME());

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

COMMIT TRANSACTION;

PRINT 'Datos demo insertados en ReservArteDB.';
GO

SET NOEXEC OFF;
GO
