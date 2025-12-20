using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Katana.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FK'yı koşullu sil
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers_CustomerId')
                    ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_Customers_CustomerId];
            ");

            // Kolonları koşullu ekle - Suppliers
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'City')
                    ALTER TABLE [Suppliers] ADD [City] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'LastSyncError')
                    ALTER TABLE [Suppliers] ADD [LastSyncError] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'LucaCode')
                    ALTER TABLE [Suppliers] ADD [LucaCode] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'LucaFinansalNesneId')
                    ALTER TABLE [Suppliers] ADD [LucaFinansalNesneId] bigint NULL;
            ");

            // PurchaseOrders kolonları
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'Description')
                    ALTER TABLE [PurchaseOrders] ADD [Description] nvarchar(500) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'DocumentSeries')
                    ALTER TABLE [PurchaseOrders] ADD [DocumentSeries] nvarchar(10) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'DocumentTypeDetailId')
                    ALTER TABLE [PurchaseOrders] ADD [DocumentTypeDetailId] int NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'IsSyncedToLuca')
                    ALTER TABLE [PurchaseOrders] ADD [IsSyncedToLuca] bit NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'LastSyncAt')
                    ALTER TABLE [PurchaseOrders] ADD [LastSyncAt] datetime2 NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'LastSyncError')
                    ALTER TABLE [PurchaseOrders] ADD [LastSyncError] nvarchar(2000) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'LucaDocumentNo')
                    ALTER TABLE [PurchaseOrders] ADD [LucaDocumentNo] nvarchar(20) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'LucaPurchaseOrderId')
                    ALTER TABLE [PurchaseOrders] ADD [LucaPurchaseOrderId] bigint NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'ProjectCode')
                    ALTER TABLE [PurchaseOrders] ADD [ProjectCode] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'ReferenceCode')
                    ALTER TABLE [PurchaseOrders] ADD [ReferenceCode] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'ShippingAddressId')
                    ALTER TABLE [PurchaseOrders] ADD [ShippingAddressId] bigint NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'SyncRetryCount')
                    ALTER TABLE [PurchaseOrders] ADD [SyncRetryCount] int NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'VatIncluded')
                    ALTER TABLE [PurchaseOrders] ADD [VatIncluded] bit NOT NULL DEFAULT 0;
            ");

            // PurchaseOrderItems kolonları
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'DiscountAmount')
                    ALTER TABLE [PurchaseOrderItems] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'LucaDetailId')
                    ALTER TABLE [PurchaseOrderItems] ADD [LucaDetailId] bigint NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'LucaStockCode')
                    ALTER TABLE [PurchaseOrderItems] ADD [LucaStockCode] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'UnitCode')
                    ALTER TABLE [PurchaseOrderItems] ADD [UnitCode] nvarchar(10) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'VatRate')
                    ALTER TABLE [PurchaseOrderItems] ADD [VatRate] decimal(5,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderItems') AND name = 'WarehouseCode')
                    ALTER TABLE [PurchaseOrderItems] ADD [WarehouseCode] nvarchar(10) NOT NULL DEFAULT '';
            ");

            // Customers kolonları
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'Currency')
                    ALTER TABLE [Customers] ADD [Currency] nvarchar(10) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'DefaultDiscountRate')
                    ALTER TABLE [Customers] ADD [DefaultDiscountRate] decimal(18,4) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'District')
                    ALTER TABLE [Customers] ADD [District] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'GroupCode')
                    ALTER TABLE [Customers] ADD [GroupCode] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'LastSyncError')
                    ALTER TABLE [Customers] ADD [LastSyncError] nvarchar(1000) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'LucaCode')
                    ALTER TABLE [Customers] ADD [LucaCode] nvarchar(50) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'LucaFinansalNesneId')
                    ALTER TABLE [Customers] ADD [LucaFinansalNesneId] bigint NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'ReferenceId')
                    ALTER TABLE [Customers] ADD [ReferenceId] nvarchar(100) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'TaxOffice')
                    ALTER TABLE [Customers] ADD [TaxOffice] nvarchar(200) NULL;
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'Type')
                    ALTER TABLE [Customers] ADD [Type] int NOT NULL DEFAULT 0;
            ");

            // KozaDepots tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KozaDepots')
                BEGIN
                    CREATE TABLE [KozaDepots] (
                        [Id] int NOT NULL IDENTITY,
                        [DepoId] bigint NULL,
                        [Kod] nvarchar(50) NOT NULL,
                        [Tanim] nvarchar(200) NOT NULL,
                        [KategoriKod] nvarchar(50) NULL,
                        [Ulke] nvarchar(50) NULL,
                        [Il] nvarchar(50) NULL,
                        [Ilce] nvarchar(50) NULL,
                        [AdresSerbest] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_KozaDepots] PRIMARY KEY ([Id])
                    );
                END
            ");

            // LocationKozaDepotMappings tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LocationKozaDepotMappings')
                BEGIN
                    CREATE TABLE [LocationKozaDepotMappings] (
                        [Id] int NOT NULL IDENTITY,
                        [KatanaLocationId] nvarchar(100) NOT NULL,
                        [KozaDepoKodu] nvarchar(50) NOT NULL,
                        [KozaDepoId] bigint NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [KatanaLocationName] nvarchar(200) NULL,
                        [KozaDepoTanim] nvarchar(200) NULL,
                        CONSTRAINT [PK_LocationKozaDepotMappings] PRIMARY KEY ([Id])
                    );
                END
            ");

            // LucaProducts tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LucaProducts')
                BEGIN
                    CREATE TABLE [LucaProducts] (
                        [Id] int NOT NULL IDENTITY,
                        [LucaCode] nvarchar(100) NOT NULL,
                        [LucaName] nvarchar(300) NOT NULL,
                        [LucaCategory] nvarchar(200) NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_LucaProducts] PRIMARY KEY ([Id])
                    );
                END
            ");

            // OrderMappings tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderMappings')
                BEGIN
                    CREATE TABLE [OrderMappings] (
                        [Id] int NOT NULL IDENTITY,
                        [OrderId] int NOT NULL,
                        [LucaInvoiceId] bigint NOT NULL,
                        [EntityType] nvarchar(50) NOT NULL,
                        [ExternalOrderId] nvarchar(200) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_OrderMappings] PRIMARY KEY ([Id])
                    );
                END
            ");

            // ProductLucaMappings tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductLucaMappings')
                BEGIN
                    CREATE TABLE [ProductLucaMappings] (
                        [Id] int NOT NULL IDENTITY,
                        [KatanaProductId] nvarchar(max) NOT NULL,
                        [KatanaSku] nvarchar(max) NOT NULL,
                        [LucaStockCode] nvarchar(max) NOT NULL,
                        [LucaStockId] bigint NULL,
                        [Version] int NOT NULL,
                        [IsActive] bit NOT NULL,
                        [SyncStatus] nvarchar(max) NOT NULL,
                        [SyncedProductName] nvarchar(max) NULL,
                        [SyncedPrice] decimal(18,2) NULL,
                        [SyncedVatRate] int NULL,
                        [SyncedBarcode] nvarchar(max) NULL,
                        [LastSyncError] nvarchar(max) NULL,
                        [SyncedAt] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NULL,
                        CONSTRAINT [PK_ProductLucaMappings] PRIMARY KEY ([Id])
                    );
                END
            ");

            // SalesOrders tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesOrders')
                BEGIN
                    CREATE TABLE [SalesOrders] (
                        [Id] int NOT NULL IDENTITY,
                        [KatanaOrderId] bigint NOT NULL,
                        [OrderNo] nvarchar(100) NOT NULL,
                        [CustomerId] int NOT NULL,
                        [OrderCreatedDate] datetime2 NULL,
                        [DeliveryDate] datetime2 NULL,
                        [Currency] nvarchar(3) NULL,
                        [Status] nvarchar(50) NOT NULL,
                        [Total] decimal(18,2) NULL,
                        [TotalInBaseCurrency] decimal(18,2) NULL,
                        [AdditionalInfo] nvarchar(500) NULL,
                        [CustomerRef] nvarchar(100) NULL,
                        [Source] nvarchar(50) NULL,
                        [LocationId] bigint NULL,
                        [LucaOrderId] int NULL,
                        [BelgeSeri] nvarchar(10) NULL,
                        [BelgeNo] nvarchar(50) NULL,
                        [DuzenlemeSaati] nvarchar(5) NULL,
                        [BelgeTurDetayId] int NULL,
                        [NakliyeBedeliTuru] int NULL,
                        [TeklifSiparisTur] int NULL,
                        [OnayFlag] bit NOT NULL,
                        [LastSyncAt] datetime2 NULL,
                        [LastSyncError] nvarchar(1000) NULL,
                        [IsSyncedToLuca] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE NO ACTION
                    );
                END
            ");

            // SupplierKozaCariMappings tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupplierKozaCariMappings')
                BEGIN
                    CREATE TABLE [SupplierKozaCariMappings] (
                        [Id] int NOT NULL IDENTITY,
                        [KatanaSupplierId] nvarchar(100) NOT NULL,
                        [KozaCariKodu] nvarchar(50) NOT NULL,
                        [KozaFinansalNesneId] bigint NULL,
                        [KatanaSupplierName] nvarchar(200) NULL,
                        [KozaCariTanim] nvarchar(200) NULL,
                        [UpdatedAt] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_SupplierKozaCariMappings] PRIMARY KEY ([Id])
                    );
                END
            ");

            // SalesOrderLines tablosu zaten varsa atla
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesOrderLines')
                BEGIN
                    CREATE TABLE [SalesOrderLines] (
                        [Id] int NOT NULL IDENTITY,
                        [SalesOrderId] int NOT NULL,
                        [KatanaRowId] bigint NOT NULL,
                        [VariantId] bigint NOT NULL,
                        [SKU] nvarchar(100) NOT NULL,
                        [ProductName] nvarchar(500) NULL,
                        [Quantity] decimal(18,4) NOT NULL,
                        [PricePerUnit] decimal(18,4) NULL,
                        [PricePerUnitInBaseCurrency] decimal(18,4) NULL,
                        [Total] decimal(18,2) NULL,
                        [TotalInBaseCurrency] decimal(18,2) NULL,
                        [TaxRate] decimal(5,2) NULL,
                        [TaxRateId] bigint NULL,
                        [LocationId] bigint NULL,
                        [ProductAvailability] nvarchar(50) NULL,
                        [ProductExpectedDate] datetime2 NULL,
                        [LucaDetayId] int NULL,
                        [LucaStokId] int NULL,
                        [LucaDepoId] int NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders]([Id]) ON DELETE CASCADE
                    );
                END
            ");

            // Indexleri koşullu olarak oluştur
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suppliers_Code')
                    CREATE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseOrders_CreatedAt')
                    CREATE INDEX [IX_PurchaseOrders_CreatedAt] ON [PurchaseOrders] ([CreatedAt]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PurchaseOrders_Status')
                    CREATE INDEX [IX_PurchaseOrders_Status] ON [PurchaseOrders] ([Status]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KozaDepots_DepoId')
                    CREATE INDEX [IX_KozaDepots_DepoId] ON [KozaDepots] ([DepoId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KozaDepots_Kod')
                    CREATE INDEX [IX_KozaDepots_Kod] ON [KozaDepots] ([Kod]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LocationKozaDepotMappings_KatanaLocationId')
                    CREATE UNIQUE INDEX [IX_LocationKozaDepotMappings_KatanaLocationId] ON [LocationKozaDepotMappings] ([KatanaLocationId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LocationKozaDepotMappings_KozaDepoId')
                    CREATE INDEX [IX_LocationKozaDepotMappings_KozaDepoId] ON [LocationKozaDepotMappings] ([KozaDepoId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LocationKozaDepotMappings_KozaDepoKodu')
                    CREATE INDEX [IX_LocationKozaDepotMappings_KozaDepoKodu] ON [LocationKozaDepotMappings] ([KozaDepoKodu]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderMappings_ExternalOrderId')
                    CREATE INDEX [IX_OrderMappings_ExternalOrderId] ON [OrderMappings] ([ExternalOrderId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderMappings_LucaInvoiceId')
                    CREATE INDEX [IX_OrderMappings_LucaInvoiceId] ON [OrderMappings] ([LucaInvoiceId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderMappings_OrderId_EntityType')
                    CREATE UNIQUE INDEX [IX_OrderMappings_OrderId_EntityType] ON [OrderMappings] ([OrderId], [EntityType]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrderLines_SalesOrderId')
                    CREATE INDEX [IX_SalesOrderLines_SalesOrderId] ON [SalesOrderLines] ([SalesOrderId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrders_CustomerId')
                    CREATE INDEX [IX_SalesOrders_CustomerId] ON [SalesOrders] ([CustomerId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrders_IsSyncedToLuca')
                    CREATE INDEX [IX_SalesOrders_IsSyncedToLuca] ON [SalesOrders] ([IsSyncedToLuca]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrders_KatanaOrderId')
                    CREATE UNIQUE INDEX [IX_SalesOrders_KatanaOrderId] ON [SalesOrders] ([KatanaOrderId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrders_LucaOrderId')
                    CREATE INDEX [IX_SalesOrders_LucaOrderId] ON [SalesOrders] ([LucaOrderId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesOrders_OrderNo')
                    CREATE INDEX [IX_SalesOrders_OrderNo] ON [SalesOrders] ([OrderNo]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierKozaCariMappings_KatanaSupplierId')
                    CREATE UNIQUE INDEX [IX_SupplierKozaCariMappings_KatanaSupplierId] ON [SupplierKozaCariMappings] ([KatanaSupplierId]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierKozaCariMappings_KozaCariKodu')
                    CREATE INDEX [IX_SupplierKozaCariMappings_KozaCariKodu] ON [SupplierKozaCariMappings] ([KozaCariKodu]);
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SupplierKozaCariMappings_KozaFinansalNesneId')
                    CREATE INDEX [IX_SupplierKozaCariMappings_KozaFinansalNesneId] ON [SupplierKozaCariMappings] ([KozaFinansalNesneId]);
            ");

            // FK'yı koşullu ekle
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers_CustomerId')
                    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id]) ON DELETE NO ACTION;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "KozaDepots");

            migrationBuilder.DropTable(
                name: "LocationKozaDepotMappings");

            // LucaProducts sadece bu migration tarafından oluşturulduysa sil
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'LucaProducts')
                    AND NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE MigrationId LIKE '%AddKozaMappingsAndInventoryFixes%')
                BEGIN
                    DROP TABLE [LucaProducts];
                END
            ");

            migrationBuilder.DropTable(
                name: "OrderMappings");

            migrationBuilder.DropTable(
                name: "ProductLucaMappings");

            migrationBuilder.DropTable(
                name: "SalesOrderLines");

            migrationBuilder.DropTable(
                name: "SupplierKozaCariMappings");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Code",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LucaCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LucaFinansalNesneId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DocumentSeries",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DocumentTypeDetailId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsSyncedToLuca",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LastSyncAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LucaDocumentNo",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "LucaPurchaseOrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SyncRetryCount",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VatIncluded",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LucaDetailId",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "LucaStockCode",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "WarehouseCode",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DefaultDiscountRate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "GroupCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastSyncError",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LucaCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LucaFinansalNesneId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Customers");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
