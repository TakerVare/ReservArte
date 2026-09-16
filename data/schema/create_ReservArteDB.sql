-- =============================================================================
-- ReservArte · CREACIÓN de la base de datos (solo esquema: DDL, sin datos)
-- =============================================================================
-- FICHERO GENERADO desde las migraciones de EF Core. NO EDITAR A MANO.
-- Un cambio de base de datos se hace con una migración y después se regenera:
--   bash data/schema/regenerate-create.sh
--
-- Última migración incluida: 20260916171801_RenameWaitingListToWaitingLists
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

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151444_BackfillCustomerProfiles'
)
BEGIN
    INSERT INTO Customers (Id, OrganizationId, FirstName, LastName, Email, Phone, Category,
                           LoyaltyPoints, IsBlocked, PreferredContactMethod, IsActive, CreatedAt)
    SELECT u.Id, u.OrganizationId, u.FirstName, u.LastName, u.Email, LEFT(u.PhoneNumber, 20),
           N'regular', 0, 0, N'email', 1, SYSUTCDATETIME()
    FROM AspNetUsers u
    WHERE u.Rol = N'Customer'
      AND u.Email IS NOT NULL
      AND LEN(u.Email) <= 255
      AND NOT EXISTS (SELECT 1 FROM Customers c WHERE c.Id = u.Id)
      AND NOT EXISTS (SELECT 1 FROM Customers c
                      WHERE c.OrganizationId = u.OrganizationId AND c.Email = u.Email);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151444_BackfillCustomerProfiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915151444_BackfillCustomerProfiles', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [ServiceCategories] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Color] nvarchar(20) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ServiceCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceCategories_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [ServicePackages] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [TotalPrice] decimal(10,2) NOT NULL,
        [DiscountPercentage] decimal(5,2) NOT NULL,
        [ImageUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ServicePackages] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServicePackages_DiscountPercentage] CHECK ([DiscountPercentage] >= 0 AND [DiscountPercentage] <= 100),
        CONSTRAINT [CK_ServicePackages_TotalPrice] CHECK ([TotalPrice] >= 0),
        CONSTRAINT [FK_ServicePackages_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [Services] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DurationMinutes] int NOT NULL,
        [BasePrice] decimal(10,2) NOT NULL,
        [CategoryId] int NULL,
        [ImageUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [RequiresAllergyTest] bit NOT NULL,
        [AllergyTestHoursBefore] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Services_DurationAndPrice] CHECK ([DurationMinutes] > 0 AND [BasePrice] >= 0),
        CONSTRAINT [FK_Services_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Services_ServiceCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ServiceCategories] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [EmployeeServices] (
        [EmployeeId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ProficiencyLevel] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_EmployeeServices] PRIMARY KEY ([EmployeeId], [ServiceId]),
        CONSTRAINT [CK_EmployeeServices_ProficiencyLevel] CHECK ([ProficiencyLevel] >= 1 AND [ProficiencyLevel] <= 5),
        CONSTRAINT [FK_EmployeeServices_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeServices_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeServices_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [ServicePackageItems] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ServicePackageId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [Order] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ServicePackageItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServicePackageItems_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicePackageItems_ServicePackages_ServicePackageId] FOREIGN KEY ([ServicePackageId]) REFERENCES [ServicePackages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ServicePackageItems_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [ServicePricings] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ServiceId] int NOT NULL,
        [EmployeeLevel] nvarchar(20) NOT NULL,
        [Price] decimal(10,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ServicePricings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ServicePricings_EmployeeLevel] CHECK ([EmployeeLevel] IN ('junior', 'senior', 'expert')),
        CONSTRAINT [CK_ServicePricings_Price] CHECK ([Price] >= 0),
        CONSTRAINT [FK_ServicePricings_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServicePricings_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE TABLE [ServiceVariations] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [ServiceId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [PriceModifier] decimal(10,2) NOT NULL,
        [DurationModifier] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ServiceVariations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceVariations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ServiceVariations_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_EmployeeServices_OrganizationId] ON [EmployeeServices] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_EmployeeServices_ServiceId] ON [EmployeeServices] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServiceCategories_OrganizationId_DisplayOrder] ON [ServiceCategories] ([OrganizationId], [DisplayOrder]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServicePackageItems_OrganizationId] ON [ServicePackageItems] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServicePackageItems_ServiceId] ON [ServicePackageItems] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServicePackageItems_ServicePackageId_Order] ON [ServicePackageItems] ([ServicePackageId], [Order]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServicePackages_OrganizationId] ON [ServicePackages] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServicePricings_OrganizationId] ON [ServicePricings] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServicePricings_ServiceId_EmployeeLevel] ON [ServicePricings] ([ServiceId], [EmployeeLevel]) WHERE [IsActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_Services_CategoryId] ON [Services] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_Services_OrganizationId] ON [Services] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServiceVariations_OrganizationId] ON [ServiceVariations] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    CREATE INDEX [IX_ServiceVariations_ServiceId] ON [ServiceVariations] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916084021_AddServiceCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916084021_AddServiceCatalog', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [CustomerId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [AppointmentDate] date NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [TotalPrice] decimal(10,2) NOT NULL,
        [DepositAmount] decimal(10,2) NOT NULL,
        [RedsysOrderNumber] nvarchar(20) NULL,
        [RedsysPreAuthToken] nvarchar(255) NULL,
        [CancellationReason] nvarchar(500) NULL,
        [CancelledAt] datetime2 NULL,
        [CancelledById] int NULL,
        [CancelledByType] nvarchar(20) NULL,
        [Notes] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Appointments_Amounts] CHECK ([TotalPrice] >= 0 AND [DepositAmount] >= 0),
        CONSTRAINT [CK_Appointments_CancelledByType] CHECK ([CancelledByType] IN ('customer', 'business')),
        CONSTRAINT [CK_Appointments_EndTime] CHECK ([EndTime] > [StartTime]),
        CONSTRAINT [CK_Appointments_Status] CHECK ([Status] IN ('pending', 'confirmed', 'in_progress', 'completed', 'cancelled', 'cancelled_by_customer', 'cancelled_by_business', 'no_show')),
        CONSTRAINT [FK_Appointments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Appointments_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE TABLE [WaitingList] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [CustomerId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [PreferredEmployeeId] int NULL,
        [PreferredDate] datetime2 NULL,
        [DateRangeStart] datetime2 NOT NULL,
        [DateRangeEnd] datetime2 NOT NULL,
        [Priority] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [NotifiedAt] datetime2 NULL,
        CONSTRAINT [PK_WaitingList] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_WaitingList_DateRange] CHECK ([DateRangeEnd] > [DateRangeStart]),
        CONSTRAINT [FK_WaitingList_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_WaitingList_Employees_PreferredEmployeeId] FOREIGN KEY ([PreferredEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WaitingList_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WaitingList_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE TABLE [AppointmentServiceItems] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationId] uniqueidentifier NOT NULL,
        [AppointmentId] int NOT NULL,
        [ServiceId] int NOT NULL,
        [ServiceVariationId] int NULL,
        [Price] decimal(10,2) NOT NULL,
        [DurationMinutes] int NOT NULL,
        [Order] int NOT NULL,
        CONSTRAINT [PK_AppointmentServiceItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_AppointmentServiceItems_PriceAndDuration] CHECK ([Price] >= 0 AND [DurationMinutes] > 0),
        CONSTRAINT [FK_AppointmentServiceItems_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AppointmentServiceItems_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppointmentServiceItems_ServiceVariations_ServiceVariationId] FOREIGN KEY ([ServiceVariationId]) REFERENCES [ServiceVariations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppointmentServiceItems_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [idx_appointments_org_date] ON [Appointments] ([OrganizationId], [AppointmentDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [idx_appointments_redsys_order] ON [Appointments] ([RedsysOrderNumber]) WHERE [RedsysOrderNumber] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_Appointments_CustomerId] ON [Appointments] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_Appointments_EmployeeId_AppointmentDate] ON [Appointments] ([EmployeeId], [AppointmentDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_AppointmentServiceItems_AppointmentId_Order] ON [AppointmentServiceItems] ([AppointmentId], [Order]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_AppointmentServiceItems_OrganizationId] ON [AppointmentServiceItems] ([OrganizationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_AppointmentServiceItems_ServiceId] ON [AppointmentServiceItems] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_AppointmentServiceItems_ServiceVariationId] ON [AppointmentServiceItems] ([ServiceVariationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [idx_waiting_list_org_service_priority] ON [WaitingList] ([OrganizationId], [ServiceId], [Priority]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_WaitingList_CustomerId] ON [WaitingList] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_WaitingList_PreferredEmployeeId] ON [WaitingList] ([PreferredEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    CREATE INDEX [IX_WaitingList_ServiceId] ON [WaitingList] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916161457_AddAppointments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916161457_AddAppointments', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [FK_WaitingList_Customers_CustomerId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [FK_WaitingList_Employees_PreferredEmployeeId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [FK_WaitingList_Organizations_OrganizationId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [FK_WaitingList_Services_ServiceId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [PK_WaitingList];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingList] DROP CONSTRAINT [CK_WaitingList_DateRange];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC sp_rename N'[WaitingList]', N'WaitingLists';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC sp_rename N'[WaitingLists].[IX_WaitingList_ServiceId]', N'IX_WaitingLists_ServiceId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC sp_rename N'[WaitingLists].[IX_WaitingList_PreferredEmployeeId]', N'IX_WaitingLists_PreferredEmployeeId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC sp_rename N'[WaitingLists].[IX_WaitingList_CustomerId]', N'IX_WaitingLists_CustomerId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC sp_rename N'[WaitingLists].[idx_waiting_list_org_service_priority]', N'idx_waiting_lists_org_service_priority', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingLists] ADD CONSTRAINT [PK_WaitingLists] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    EXEC(N'ALTER TABLE [WaitingLists] ADD CONSTRAINT [CK_WaitingLists_DateRange] CHECK ([DateRangeEnd] > [DateRangeStart])');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingLists] ADD CONSTRAINT [FK_WaitingLists_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingLists] ADD CONSTRAINT [FK_WaitingLists_Employees_PreferredEmployeeId] FOREIGN KEY ([PreferredEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingLists] ADD CONSTRAINT [FK_WaitingLists_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    ALTER TABLE [WaitingLists] ADD CONSTRAINT [FK_WaitingLists_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260916171801_RenameWaitingListToWaitingLists'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260916171801_RenameWaitingListToWaitingLists', N'8.0.0');
END;
GO

COMMIT;
GO

