IF OBJECT_ID(N'dbo.BotExchangeRateSnapshot', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BotExchangeRateSnapshot
    (
        SnapshotId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_BotExchangeRateSnapshot PRIMARY KEY,
        SourceRateDate DATE NOT NULL,
        SourceUpdatedAt DATETIME2(0) NOT NULL,
        CurrencyCode NVARCHAR(10) NOT NULL,
        CurrencyName NVARCHAR(50) NOT NULL,
        CashBuy DECIMAL(18, 6) NULL,
        CashSell DECIMAL(18, 6) NULL,
        SpotBuy DECIMAL(18, 6) NULL,
        SpotSell DECIMAL(18, 6) NULL,
        SourceUrl NVARCHAR(500) NOT NULL,
        InsertedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_BotExchangeRateSnapshot_InsertedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_BotExchangeRateSnapshot_SourceUpdatedAt_CurrencyCode'
      AND object_id = OBJECT_ID(N'dbo.BotExchangeRateSnapshot')
)
BEGIN
    CREATE UNIQUE INDEX UX_BotExchangeRateSnapshot_SourceUpdatedAt_CurrencyCode
        ON dbo.BotExchangeRateSnapshot (SourceUpdatedAt, CurrencyCode);
END;
GO
