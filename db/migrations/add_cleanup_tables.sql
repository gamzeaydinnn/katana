-- Migration: Add Cleanup Operation and Log tables
-- Date: 2024-01-15
-- Description: Creates tables for tracking cleanup operations and their logs

-- Create CleanupOperations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CleanupOperations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CleanupOperations] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [OperationType] NVARCHAR(50) NOT NULL,
        [StartTime] DATETIME2 NOT NULL,
        [EndTime] DATETIME2 NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [UserId] NVARCHAR(100) NULL,
        [Parameters] NVARCHAR(MAX) NULL,
        [Result] NVARCHAR(MAX) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL
    );
    
    CREATE INDEX idx_operation_type ON [dbo].[CleanupOperations]([OperationType]);
    CREATE INDEX idx_status ON [dbo].[CleanupOperations]([Status]);
    CREATE INDEX idx_start_time ON [dbo].[CleanupOperations]([StartTime]);
    CREATE INDEX idx_user_id ON [dbo].[CleanupOperations]([UserId]);
END
GO

-- Create CleanupLogs table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CleanupLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[CleanupLogs] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [CleanupOperationId] INT NOT NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Level] NVARCHAR(20) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [EntityType] NVARCHAR(50) NULL,
        [EntityId] INT NULL,
        [Details] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY ([CleanupOperationId]) REFERENCES [dbo].[CleanupOperations]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX idx_cleanup_operation ON [dbo].[CleanupLogs]([CleanupOperationId]);
    CREATE INDEX idx_timestamp ON [dbo].[CleanupLogs]([Timestamp]);
    CREATE INDEX idx_level ON [dbo].[CleanupLogs]([Level]);
    CREATE INDEX idx_entity ON [dbo].[CleanupLogs]([EntityType], [EntityId]);
END
GO

-- Add approval fields to SalesOrders table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrders]') AND name = 'ApprovedDate')
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ApprovedDate] DATETIME2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrders]') AND name = 'ApprovedBy')
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [ApprovedBy] NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrders]') AND name = 'SyncStatus')
BEGIN
    ALTER TABLE [dbo].[SalesOrders] ADD [SyncStatus] NVARCHAR(50) NULL;
END
GO

-- Add KatanaOrderId to SalesOrderLines table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrderLines]') AND name = 'KatanaOrderId')
BEGIN
    ALTER TABLE [dbo].[SalesOrderLines] ADD [KatanaOrderId] INT NULL;
END
GO

-- Add indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrders]') AND name = 'idx_sales_orders_status')
BEGIN
    CREATE INDEX idx_sales_orders_status ON [dbo].[SalesOrders]([Status]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrders]') AND name = 'idx_sales_orders_approved_date')
BEGIN
    CREATE INDEX idx_sales_orders_approved_date ON [dbo].[SalesOrders]([ApprovedDate]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SalesOrderLines]') AND name = 'idx_sales_order_lines_katana_order_id')
BEGIN
    CREATE INDEX idx_sales_order_lines_katana_order_id ON [dbo].[SalesOrderLines]([KatanaOrderId]);
END
GO
