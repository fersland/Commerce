IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CommerceDB')
BEGIN
    CREATE DATABASE CommerceDB;
END
GO

USE CommerceDB;
GO

IF OBJECT_ID('dbo.commerce', 'U') IS NOT NULL
    DROP TABLE dbo.commerce;
GO

CREATE TABLE dbo.commerce (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    pc_processdate VARCHAR(20) NOT NULL,
    pc_nomcomred VARCHAR(250) NULL,
    pc_numdoc VARCHAR(50) NULL,
    FechaCreacion DATETIME DEFAULT GETDATE()
);
GO

IF OBJECT_ID('dbo.commerce_quarantine', 'U') IS NOT NULL
    DROP TABLE dbo.commerce_quarantine;
GO

CREATE TABLE dbo.commerce_quarantine (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    pc_processdate VARCHAR(20) NULL,
    pc_nomcomred VARCHAR(250) NULL,
    pc_numdoc VARCHAR(50) NULL,
    motivo VARCHAR(500) NOT NULL,
    FechaCuarentena DATETIME DEFAULT GETDATE()
);
GO

-- =============================================
-- sp_create_commerce
-- =============================================
IF OBJECT_ID('dbo.sp_create_commerce', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_create_commerce;
GO

CREATE PROCEDURE dbo.sp_create_commerce
    @pc_processdate VARCHAR(20),
    @pc_nomcomred VARCHAR(250),
    @pc_numdoc VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.commerce (pc_processdate, pc_nomcomred, pc_numdoc)
    VALUES (@pc_processdate, @pc_nomcomred, @pc_numdoc);
END;
GO

-- =============================================
-- sp_process_commerce_validation
-- =============================================
IF OBJECT_ID('dbo.sp_process_commerce_validation', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_process_commerce_validation;
GO

CREATE PROCEDURE dbo.sp_process_commerce_validation
    @p_processdate VARCHAR(20),
    @TotalQuarantine INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Tabla temporal para almacenar los errores
    DECLARE @Errores TABLE (
        pc_processdate VARCHAR(20),
        pc_nomcomred VARCHAR(250),
        pc_numdoc VARCHAR(50),
        motivo VARCHAR(500)
    );

    -- Validacion nombre de comercio vacio o nulo
    INSERT INTO @Errores (pc_processdate, pc_nomcomred, pc_numdoc, motivo)
    SELECT pc_processdate, pc_nomcomred, pc_numdoc, 'El nombre del comercio (nomcomred) se encuentra vacio'
    FROM dbo.commerce
    WHERE pc_processdate = @p_processdate
      AND (pc_nomcomred IS NULL OR LTRIM(RTRIM(pc_nomcomred)) = '');

    -- Validacion número de documento vacío, nulo, o con letras/caracteres especiales
    INSERT INTO @Errores (pc_processdate, pc_nomcomred, pc_numdoc, motivo)
    SELECT pc_processdate, pc_nomcomred, pc_numdoc, 'El número (numdoc) contiene letras o caracteres especiales'
    FROM dbo.commerce
    WHERE pc_processdate = @p_processdate
      AND (pc_numdoc IS NULL OR LTRIM(RTRIM(pc_numdoc)) = '' OR pc_numdoc LIKE '%[^0-9]%')
      -- Evitamos duplicar errores
      AND NOT (pc_nomcomred IS NULL OR LTRIM(RTRIM(pc_nomcomred)) = '');

    -- si un registro cumple ambos errores, registrar ambos motivos
    INSERT INTO @Errores (pc_processdate, pc_nomcomred, pc_numdoc, motivo)
    SELECT pc_processdate, pc_nomcomred, pc_numdoc, 'El nombre del comercio se encuentra vacio y el número contiene letras/caracteres especiales'
    FROM dbo.commerce
    WHERE pc_processdate = @p_processdate
      AND (pc_nomcomred IS NULL OR LTRIM(RTRIM(pc_nomcomred)) = '')
      AND (pc_numdoc IS NULL OR LTRIM(RTRIM(pc_numdoc)) = '' OR pc_numdoc LIKE '%[^0-9]%');

    -- Limpiamos duplicados en la tabla temporal
    ;WITH CTE_Unique AS (
        SELECT ROW_NUMBER() OVER(PARTITION BY pc_processdate, pc_nomcomred, pc_numdoc ORDER BY (SELECT NULL)) as rn,
               motivo
        FROM @Errores
    )

    INSERT INTO dbo.commerce_quarantine (pc_processdate, pc_nomcomred, pc_numdoc, motivo)
    SELECT pc_processdate, pc_nomcomred, pc_numdoc, motivo
    FROM @Errores;

    -- Obtener la cantidad de registros insertados en cuarentena
    SELECT @TotalQuarantine = COUNT(*) 
    FROM dbo.commerce_quarantine 
    WHERE pc_processdate = @p_processdate;

    -- Eliminar de la tabla principal 'commerce' los registros que fallaron las validaciones
    DELETE FROM dbo.commerce
    WHERE pc_processdate = @p_processdate
      AND (
            (pc_nomcomred IS NULL OR LTRIM(RTRIM(pc_nomcomred)) = '')
            OR 
            (pc_numdoc IS NULL OR LTRIM(RTRIM(pc_numdoc)) = '' OR pc_numdoc LIKE '%[^0-9]%')
          );
          
    RETURN;
END;
GO