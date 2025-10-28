-- SQL script: create helpful indexes for logging queries and PendingStockAdjustments table
-- Run this against your SQL Server database as a DBA or via migration tool.

-- Indexes to improve LogsController queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ErrorLogs_CreatedAt' AND object_id = OBJECT_ID('dbo.ErrorLogs'))
BEGIN
    CREATE INDEX IX_ErrorLogs_CreatedAt ON dbo.ErrorLogs(CreatedAt DESC);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ErrorLogs_Level' AND object_id = OBJECT_ID('dbo.ErrorLogs'))
BEGIN
    CREATE INDEX IX_ErrorLogs_Level ON dbo.ErrorLogs(Level);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ErrorLogs_Category_CreatedAt' AND object_id = OBJECT_ID('dbo.ErrorLogs'))
BEGIN
    CREATE INDEX IX_ErrorLogs_Category_CreatedAt ON dbo.ErrorLogs(Category, CreatedAt DESC);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogs_Timestamp' AND object_id = OBJECT_ID('dbo.AuditLogs'))
BEGIN
    CREATE INDEX IX_AuditLogs_Timestamp ON dbo.AuditLogs(Timestamp DESC);
END

-- PendingStockAdjustments table (for admin approval workflow)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PendingStockAdjustments')
BEGIN
    CREATE TABLE dbo.PendingStockAdjustments (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        ExternalOrderId NVARCHAR(100) NOT NULL,
        ProductId BIGINT NOT NULL,
        Sku NVARCHAR(100) NULL,
        Quantity INT NOT NULL,
        RequestedBy NVARCHAR(100) NOT NULL DEFAULT 'system',
        RequestedAt DATETIMEOFFSET NOT NULL DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        ApprovedBy NVARCHAR(100) NULL,
        ApprovedAt DATETIMEOFFSET NULL,
        RejectionReason NVARCHAR(500) NULL,
        Notes NVARCHAR(1000) NULL
    );
    CREATE UNIQUE INDEX UX_PendingStockAdjustments_ExternalOrderId ON dbo.PendingStockAdjustments(ExternalOrderId);
END

-- NOTE: Prefer creating an EF Core migration rather than running this script directly when possible.
