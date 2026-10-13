-- Crea la base de la app y un usuario propio (sin privilegios de servidor).
-- Idempotente: se puede ejecutar varias veces sin error.

IF DB_ID(N'$(APP_DB)') IS NULL
    CREATE DATABASE [$(APP_DB)];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(APP_USER)')
    CREATE LOGIN [$(APP_USER)] WITH PASSWORD = N'$(APP_PASSWORD)', CHECK_POLICY = ON;
ELSE
    ALTER LOGIN [$(APP_USER)] WITH PASSWORD = N'$(APP_PASSWORD)';
GO

USE [$(APP_DB)];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(APP_USER)')
    CREATE USER [$(APP_USER)] FOR LOGIN [$(APP_USER)];
GO

-- db_owner solo sobre esta base: necesario para que EF aplique migraciones.
ALTER ROLE db_owner ADD MEMBER [$(APP_USER)];
GO
