-- =============================================================================
-- ReservArte: eliminar la base de datos ReservArteDB
-- Cierra sesiones activas con SINGLE_USER y ejecuta DROP DATABASE.
-- Ejecutar en SQL Server (master) con permisos adecuados.
-- =============================================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ReservArteDB')
BEGIN
    ALTER DATABASE ReservArteDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ReservArteDB;
    PRINT 'Base de datos ReservArteDB eliminada.';
END
ELSE
BEGIN
    PRINT 'La base de datos ReservArteDB no existe.';
END
GO
