-- =============================================================================
-- ReservArte · CREACIÓN de la base de datos (solo esquema: DDL, sin datos)
-- =============================================================================
-- FICHERO GENERADO desde las migraciones de EF Core. NO EDITAR A MANO.
-- Un cambio de base de datos se hace con una migración y después se regenera:
--   bash data/schema/regenerate-create.sh
--
-- Última migración incluida: 20260915112149_AddCustomers
-- Idempotente: se puede ejecutar varias veces; las migraciones ya aplicadas
-- se saltan gracias a __EFMigrationsHistory.
-- Orden de uso: 1) drop_ReservArteDB.sql (opcional, DESTRUYE)
--               2) este fichero
--               3) data/demo/seed_demo_ReservArteDB.sql (solo desarrollo)
-- Detalle: data/README.md
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

-- Opciones de sesión obligatorias para los índices filtrados de Identity
-- (EmailIndex, UserNameIndex). sqlcmd arranca con QUOTED_IDENTIFIER OFF, y sin
-- esto el script falla al crearlos (error 1934). Afectan a toda la sesión.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Subdomain] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Address] nvarchar(300) NULL,
        [City] nvarchar(100) NULL,
        [PostalCode] nvarchar(10) NULL,
        [Country] nvarchar(2) NOT NULL DEFAULT N'ES',
        [TaxId] nvarchar(20) NULL,
        [LogoUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Password] nvarchar(255) NOT NULL,
        [Rol] nvarchar(50) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [ProfileImageUrl] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Rol] nvarchar(50) NOT NULL,
        [ProfileImageUrl] nvarchar(500) NULL,
        [HireDate] date NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Users_Id] FOREIGN KEY ([Id]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_OrganizationId] ON [Employees] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_Subdomain] ON [Organizations] ([Subdomain]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_OrganizationId] ON [Users] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703131039_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703131039_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [Employees] DROP CONSTRAINT [FK_Employees_Users_Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [FK_Users_Organizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [PK_Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    DROP INDEX [IX_Users_Email] ON [Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Password');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Users] DROP COLUMN [Password];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Phone');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Users] DROP COLUMN [Phone];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    EXEC sp_rename N'[Users]', N'AspNetUsers';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    EXEC sp_rename N'[AspNetUsers].[IX_Users_OrganizationId]', N'IX_AspNetUsers_OrganizationId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'Email');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [Email] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [AccessFailedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ConcurrencyStamp] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [EmailConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LockoutEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [LockoutEnd] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [NormalizedEmail] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [NormalizedUserName] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PasswordHash] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PhoneNumber] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PhoneNumberConfirmed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [SecurityStamp] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [TwoFactorEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [UserName] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_AspNetUsers_Id] FOREIGN KEY ([Id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706212020_AddAspNetIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706212020_AddAspNetIdentity', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717063404_AddRefreshTokens'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] int NOT NULL,
        [Token] nvarchar(200) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsRevoked] bit NOT NULL,
        [CreatedByIp] nvarchar(45) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717063404_AddRefreshTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717063404_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260717063404_AddRefreshTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260717063404_AddRefreshTokens', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825095439_AddRgpdConsentToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [AcceptedPrivacyVersion] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825095439_AddRgpdConsentToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [AcceptedTermsVersion] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825095439_AddRgpdConsentToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [ConsentAcceptedAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825095439_AddRgpdConsentToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825095439_AddRgpdConsentToUser', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE TABLE [EmployeeAvailabilities] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [EmployeeId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [IsRecurring] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_EmployeeAvailabilities] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeAvailabilities_DayOfWeek] CHECK ([DayOfWeek] >= 0 AND [DayOfWeek] <= 6),
        CONSTRAINT [FK_EmployeeAvailabilities_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeAvailabilities_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE TABLE [EmployeeExceptions] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [EmployeeId] int NOT NULL,
        [StartDateTime] datetime2 NOT NULL,
        [EndDateTime] datetime2 NOT NULL,
        [Reason] nvarchar(500) NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_EmployeeExceptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_EmployeeExceptions_Interval] CHECK ([EndDateTime] > [StartDateTime]),
        CONSTRAINT [CK_EmployeeExceptions_Type] CHECK ([Type] IN ('vacation', 'sick_leave', 'personal', 'training', 'other')),
        CONSTRAINT [FK_EmployeeExceptions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_EmployeeExceptions_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE INDEX [IX_EmployeeAvailabilities_EmployeeId_DayOfWeek] ON [EmployeeAvailabilities] ([EmployeeId], [DayOfWeek]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE INDEX [IX_EmployeeAvailabilities_OrganizationId] ON [EmployeeAvailabilities] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE INDEX [IX_EmployeeExceptions_EmployeeId_StartDateTime_EndDateTime] ON [EmployeeExceptions] ([EmployeeId], [StartDateTime], [EndDateTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    CREATE INDEX [IX_EmployeeExceptions_OrganizationId] ON [EmployeeExceptions] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913084923_AddEmployeeAvailabilityAndExceptions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913084923_AddEmployeeAvailabilityAndExceptions', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913193719_NormalizeRolesToPascalCase'
)
BEGIN
                    UPDATE AspNetUsers SET Rol = 'Admin'    WHERE LOWER(Rol) = 'admin';
                    UPDATE AspNetUsers SET Rol = 'Manager'  WHERE LOWER(Rol) = 'manager';
                    UPDATE AspNetUsers SET Rol = 'Employee' WHERE LOWER(Rol) = 'employee';
                    UPDATE AspNetUsers SET Rol = 'Customer' WHERE LOWER(Rol) IN ('customer', 'client');
                    UPDATE Employees SET Rol = 'Admin'    WHERE LOWER(Rol) = 'admin';
                    UPDATE Employees SET Rol = 'Manager'  WHERE LOWER(Rol) = 'manager';
                    UPDATE Employees SET Rol = 'Employee' WHERE LOWER(Rol) = 'employee';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913193719_NormalizeRolesToPascalCase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913193719_NormalizeRolesToPascalCase', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    DROP INDEX [IX_Employees_Email] ON [Employees];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    DROP INDEX [IX_Employees_OrganizationId] ON [Employees];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    DROP INDEX [EmailIndex] ON [AspNetUsers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    DROP INDEX [UserNameIndex] ON [AspNetUsers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] DROP CONSTRAINT [PK_AspNetUserLogins];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] ADD [OrganizationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    UPDATE l
    SET l.OrganizationId = u.OrganizationId
    FROM AspNetUserLogins l
    INNER JOIN AspNetUsers u ON u.Id = l.UserId;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] ADD CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([OrganizationId], [LoginProvider], [ProviderKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_OrganizationId_Email] ON [Employees] ([OrganizationId], [Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [EmailIndex] ON [AspNetUsers] ([OrganizationId], [NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([OrganizationId], [NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915101445_ScopeEmailAndExternalLoginsToOrganization'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915101445_ScopeEmailAndExternalLoginsToOrganization', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [ProfileImageUrl] nvarchar(500) NULL,
        [BirthDate] date NULL,
        [Category] nvarchar(20) NOT NULL,
        [LoyaltyPoints] int NOT NULL,
        [IsBlocked] bit NOT NULL,
        [BlockedReason] nvarchar(500) NULL,
        [PreferredContactMethod] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Customers_Category] CHECK ([Category] IN ('regular', 'vip', 'new')),
        CONSTRAINT [CK_Customers_PreferredContactMethod] CHECK ([PreferredContactMethod] IN ('email', 'phone', 'sms', 'whatsapp')),
        CONSTRAINT [FK_Customers_AspNetUsers_Id] FOREIGN KEY ([Id]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Customers_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE TABLE [CustomerAllergies] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [CustomerId] int NOT NULL,
        [AllergyDescription] nvarchar(500) NOT NULL,
        [Severity] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CustomerAllergies] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerAllergies_Severity] CHECK ([Severity] IN ('low', 'medium', 'high')),
        CONSTRAINT [FK_CustomerAllergies_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomerAllergies_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE TABLE [CustomerConsents] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [CustomerId] int NOT NULL,
        [ConsentType] nvarchar(50) NOT NULL,
        [IsGranted] bit NOT NULL,
        [GrantedAt] datetime2 NULL,
        [RevokedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CustomerConsents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CustomerConsents_ConsentType] CHECK ([ConsentType] IN ('data_processing', 'marketing', 'photos', 'whatsapp', 'saved_cards')),
        CONSTRAINT [CK_CustomerConsents_GrantedAt] CHECK ([IsGranted] = 0 OR [GrantedAt] IS NOT NULL),
        CONSTRAINT [FK_CustomerConsents_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomerConsents_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE TABLE [CustomerNotes] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [CustomerId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [Note] nvarchar(2000) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CustomerNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerNotes_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CustomerNotes_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CustomerNotes_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerAllergies_CustomerId] ON [CustomerAllergies] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerAllergies_OrganizationId] ON [CustomerAllergies] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CustomerConsents_CustomerId_ConsentType] ON [CustomerConsents] ([CustomerId], [ConsentType]) WHERE [IsActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerConsents_OrganizationId] ON [CustomerConsents] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerNotes_CustomerId_CreatedAt] ON [CustomerNotes] ([CustomerId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerNotes_EmployeeId] ON [CustomerNotes] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE INDEX [IX_CustomerNotes_OrganizationId] ON [CustomerNotes] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_OrganizationId_Email] ON [Customers] ([OrganizationId], [Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915112149_AddCustomers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915112149_AddCustomers', N'8.0.0');
END;
GO

COMMIT;
GO

