using Katana.Core.Entities;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Katana.Core.Interfaces;
using AuditLogEntity = Katana.Core.Entities.AuditLog;

namespace Katana.Data.Context;
public class IntegrationDbContext : DbContext
{
    private readonly Katana.Core.Services.PendingDbWriteQueue? _pendingQueue;
    private readonly ICurrentUserService? _currentUser;

    public IntegrationDbContext(
        DbContextOptions<IntegrationDbContext> options,
        Katana.Core.Services.PendingDbWriteQueue? pendingQueue = null,
        ICurrentUserService? currentUser = null) : base(options)
    {
        _pendingQueue = pendingQueue;
        _currentUser = currentUser;
    }
    
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<SyncOperationLog> SyncOperationLogs { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
    public DbSet<AccountingRecord> AccountingRecords { get; set; } = null!;
    public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
    public DbSet<VariantMapping> VariantMappings { get; set; } = null!;
    public DbSet<Batch> Batches { get; set; } = null!;
    public DbSet<ManufacturingOrder> ManufacturingOrders { get; set; } = null!;
    public DbSet<BillOfMaterials> BillOfMaterials { get; set; } = null!;
    public DbSet<StockTransfer> StockTransfers { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;

    
    public DbSet<IntegrationLog> IntegrationLogs { get; set; } = null!;
    public DbSet<MappingTable> MappingTables { get; set; } = null!;
    public DbSet<LucaProduct> LucaProducts { get; set; } = null!;
    public DbSet<FailedSyncRecord> FailedSyncRecords { get; set; } = null!;
    
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!; 
    public DbSet<PendingStockAdjustment> PendingStockAdjustments { get; set; } = null!;
    public DbSet<Katana.Core.Entities.Notification> Notifications { get; set; } = null!;
    public DbSet<Katana.Core.Entities.FailedNotification> FailedNotifications { get; set; } = null!;
    public DbSet<DashboardMetric> DashboardMetrics { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<KeepSeparateGroup> KeepSeparateGroups { get; set; } = null!;
    public DbSet<MergeHistory> MergeHistories { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;
    public DbSet<DataCorrectionLog> DataCorrectionLogs { get; set; } = null!;
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
     public DbSet<OrderItem> OrderItems { get; set; }
     public DbSet<Supplier> Suppliers { get; set; }
      public DbSet<SupplierPrice> SupplierPrices { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesOrderLine> SalesOrderLines { get; set; }
    public DbSet<Katana.Core.Entities.User> Users { get; set; } = null!;
    public DbSet<LocationKozaDepotMapping> LocationKozaDepotMappings => Set<LocationKozaDepotMapping>();
    public DbSet<SupplierKozaCariMapping> SupplierKozaCariMappings => Set<SupplierKozaCariMapping>();
    public DbSet<CustomerKozaCariMapping> CustomerKozaCariMappings => Set<CustomerKozaCariMapping>();
    public DbSet<KozaDepot> KozaDepots => Set<KozaDepot>();
    public DbSet<Katana.Data.Models.OrderMapping> OrderMappings => Set<Katana.Data.Models.OrderMapping>();
    public DbSet<ProductLucaMapping> ProductLucaMappings => Set<ProductLucaMapping>();
    public DbSet<TaxRateMapping> TaxRateMappings => Set<TaxRateMapping>();
    public DbSet<UoMMapping> UoMMappings => Set<UoMMapping>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Katana.Data.Models.LucaProduct>(entity =>
    {
        entity.ToTable("LucaProducts");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.LucaCode).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LucaName).IsRequired().HasMaxLength(300);
        entity.Property(e => e.LucaCategory).HasMaxLength(200);
        entity.Property(e => e.CreatedAt).IsRequired();
        entity.Property(e => e.UpdatedAt).IsRequired();
    });
    
    modelBuilder.Entity<SyncOperationLog>(entity =>
    {
        entity.ToTable("SyncLogs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SyncType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.StartTime).IsRequired();
        entity.Property(e => e.Details).HasColumnType("nvarchar(max)");
    });

    
    modelBuilder.Entity<Product>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SKU).IsUnique();
        entity.HasIndex(e => e.CategoryId); 
        entity.Property(e => e.Price).HasPrecision(18, 2);
        
        // Luca sync fields configuration
        entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
        entity.Property(e => e.Barcode).HasMaxLength(100);
        entity.Property(e => e.KategoriAgacKod).HasMaxLength(50);
        entity.Property(e => e.GtipCode).HasMaxLength(50);
        entity.Property(e => e.UzunAdi).HasMaxLength(500);
        
        entity.HasOne<Category>()
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        entity.HasMany(e => e.StockMovements)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        
        entity.HasMany(e => e.Stocks)
            .WithOne(s => s.Product)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.Batches)
            .WithOne(b => b.Product)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(e => e.BillOfMaterials)
            .WithOne(b => b.Product)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    });


        
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProductId, e.Timestamp });
            entity.HasIndex(e => e.IsSynced);
        });
        
        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.Timestamp).IsRequired();
            
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.VariantId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.ProductId, e.Timestamp })
                .HasDatabaseName("IX_InventoryMovements_Product_Timestamp");
            
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductSku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SourceDocument).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WarehouseCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timestamp).IsRequired();
            
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.IsSynced);
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<Category>()
            .HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasIndex(e => new { e.ParentId, e.Name }).IsUnique();
        });

        
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaxNo).IsUnique();
            
            // Luca integration fields
            entity.Property(e => e.DefaultDiscountRate).HasPrecision(18, 4);
            entity.Property(e => e.LucaCode).HasMaxLength(50);
            entity.Property(e => e.GroupCode).HasMaxLength(50);
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.Property(e => e.TaxOffice).HasMaxLength(200);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.ReferenceId).HasMaxLength(100);
            entity.Property(e => e.LastSyncError).HasMaxLength(1000);
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncHash);
            entity.HasIndex(e => e.SyncedAt);

            entity.HasMany(e => e.Invoices)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNo).IsUnique();
            entity.HasIndex(e => e.IsSynced);

            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasMany(e => e.InvoiceItems)
                .WithOne(e => e.Invoice)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(5, 4);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        
        modelBuilder.Entity<IntegrationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SyncType, e.StartTime });
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.FailedRecords)
                .WithOne(e => e.IntegrationLog)
                .HasForeignKey(e => e.IntegrationLogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<MappingTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MappingType, e.SourceValue }).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
        });

        
        modelBuilder.Entity<FailedSyncRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RecordType, e.Status });
            entity.HasIndex(e => e.NextRetryAt);
        });

        
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => new { e.Timestamp, e.ActionType });
            entity.Property(e => e.Timestamp).IsRequired();
        });

        
        modelBuilder.Entity<PendingStockAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalOrderId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.RequestedBy).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.ExternalOrderId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
        });

        
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => new { e.CreatedAt, e.Level });
            entity.HasIndex(e => e.Category);
            entity.Property(e => e.StackTrace).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ContextData).HasMaxLength(1000);
        });

        
        modelBuilder.Entity<PendingStockAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalOrderId).IsUnique();
            entity.Property(e => e.ExternalOrderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RequestedBy).HasMaxLength(100).IsRequired();
        });

        
        modelBuilder.Entity<Katana.Core.Entities.Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Payload).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.RelatedPendingId).HasColumnType("bigint");
        });

        modelBuilder.Entity<Katana.Core.Entities.FailedNotification>(entity =>
        {
            entity.ToTable("FailedNotifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Payload).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<DashboardMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Hour).IsUnique();
        });

        
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Code);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Barcode).HasMaxLength(100);
            entity.Property(e => e.Attributes).HasMaxLength(1000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<VariantMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KatanaVariantId).IsUnique();
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductId).IsRequired();
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProductId, e.BatchNo }).IsUnique();
            entity.Property(e => e.BatchNo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ManufacturingOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNo).IsUnique();
            entity.Property(e => e.OrderNo).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
        });

        modelBuilder.Entity<BillOfMaterials>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProductId, e.MaterialId }).IsUnique();
            entity.Property(e => e.Unit).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.BillOfMaterials)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Material)
                .WithMany()
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FromWarehouse).HasMaxLength(50);
            entity.Property(e => e.ToWarehouse).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);
            entity.HasIndex(e => e.TransferDate);
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.PaymentDate);
            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<SupplierPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(sp => sp.Supplier)
                .WithMany(s => s.PriceList)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KozaDepot entity
        modelBuilder.Entity<KozaDepot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Kod);
            entity.HasIndex(e => e.DepoId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNo).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(10);
            entity.HasIndex(e => e.OrderNo);
            
            // Order silindiğinde OrderItem'lar da cascade silinmeli
            entity.HasMany(e => e.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Customer foreign key - restrict (müşteri silinemez eğer siparişi varsa)
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ProductId);
            
            entity.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountingRecord>(entity =>
        {
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        // SalesOrder - SalesOrderLine cascade delete
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNo);
            entity.HasIndex(e => e.KatanaOrderId).IsUnique();
            entity.HasIndex(e => e.LucaOrderId);
            entity.HasIndex(e => e.IsSyncedToLuca);
            
            // SalesOrder silindiğinde SalesOrderLine'lar da cascade silinmeli
            entity.HasMany(e => e.Lines)
                .WithOne(l => l.SalesOrder)
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Customer foreign key - restrict
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.ProductAvailability).HasMaxLength(50);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,4)");
            entity.Property(e => e.PricePerUnit).HasColumnType("decimal(18,4)");
            entity.Property(e => e.PricePerUnitInBaseCurrency).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Total).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalInBaseCurrency).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
            
            entity.HasIndex(e => e.SalesOrderId);
            entity.HasIndex(e => e.KatanaRowId);
            entity.HasIndex(e => e.VariantId);
            
            entity.HasOne(l => l.SalesOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseOrder - PurchaseOrderItem cascade delete
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNo).HasMaxLength(100);
            entity.Property(e => e.SupplierCode).HasMaxLength(100);
            
            // Performance indeksleri
            entity.HasIndex(e => e.OrderNo);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            
            // PurchaseOrder silindiğinde PurchaseOrderItem'lar da cascade silinmeli
            entity.HasMany(e => e.Items)
                .WithOne(i => i.PurchaseOrder)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LucaStockCode).HasMaxLength(50);
            entity.Property(e => e.WarehouseCode).HasMaxLength(10);
            entity.Property(e => e.VatRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.UnitCode).HasMaxLength(10);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18,2)");
            
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => e.LucaDetailId);
            
            entity.HasOne(i => i.PurchaseOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique(false);
        });

        
        modelBuilder.Entity<DataCorrectionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ValidationError).HasMaxLength(500);
            entity.Property(e => e.CorrectionReason).HasMaxLength(500);
            entity.HasIndex(e => new { e.SourceSystem, e.EntityType, e.IsApproved });
            entity.HasIndex(e => e.IsSynced);
        });

        // Koza Depo Mapping
        modelBuilder.Entity<LocationKozaDepotMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaLocationId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KozaDepoKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KatanaLocationName).HasMaxLength(200);
            entity.Property(e => e.KozaDepoTanim).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
            
            // KatanaLocationId unique olmalı (her location sadece bir Koza depo'ya map olur)
            entity.HasIndex(e => e.KatanaLocationId).IsUnique();
            
            // KozaDepoKodu'na göre arama için index
            entity.HasIndex(e => e.KozaDepoKodu);
            
            // KozaDepoId'ye göre arama için index
            entity.HasIndex(e => e.KozaDepoId);
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });

        // Koza Supplier Cari Mapping
        modelBuilder.Entity<SupplierKozaCariMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaSupplierId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KozaCariKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KatanaSupplierName).HasMaxLength(200);
            entity.Property(e => e.KozaCariTanim).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
            
            // KatanaSupplierId unique olmalı
            entity.HasIndex(e => e.KatanaSupplierId).IsUnique();
            
            // KozaCariKodu'na göre arama için index
            entity.HasIndex(e => e.KozaCariKodu);
            
            // KozaFinansalNesneId'ye göre arama için index
            entity.HasIndex(e => e.KozaFinansalNesneId);
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });

        // Koza Customer Cari Mapping
        modelBuilder.Entity<CustomerKozaCariMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaCustomerId).IsRequired();
            entity.Property(e => e.KozaCariKodu).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KatanaCustomerName).HasMaxLength(200);
            entity.Property(e => e.KozaCariTanim).HasMaxLength(200);
            entity.Property(e => e.KatanaCustomerTaxNo).HasMaxLength(11);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
            
            // KatanaCustomerId unique olmalı
            entity.HasIndex(e => e.KatanaCustomerId).IsUnique();
            
            // KozaCariKodu'na göre arama için index
            entity.HasIndex(e => e.KozaCariKodu);
            
            // KozaFinansalNesneId'ye göre arama için index
            entity.HasIndex(e => e.KozaFinansalNesneId);
            
            // Vergi numarasına göre arama için index (duplicate kontrolü)
            entity.HasIndex(e => e.KatanaCustomerTaxNo);
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });

        // Order Mappings - İdempotency için
        modelBuilder.Entity<Katana.Data.Models.OrderMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.LucaInvoiceId).IsRequired();
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExternalOrderId).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.SyncStatus).HasDefaultValue("SYNCED");
            
            // Her OrderId + EntityType kombinasyonu unique olmalı
            entity.HasIndex(e => new { e.OrderId, e.EntityType }).IsUnique();
            
            // LucaInvoiceId'ye göre arama için index
            entity.HasIndex(e => e.LucaInvoiceId);
            
            // ExternalOrderId'ye göre arama için index (Katana sipariş no ile mapping bulmak için)
            entity.HasIndex(e => e.ExternalOrderId);
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });

        // Product Luca Mapping
        modelBuilder.Entity<ProductLucaMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaProductId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KatanaSku).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LucaStockCode).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING");
            entity.Property(e => e.SyncedPrice).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Her Katana ürünü için sadece 1 aktif mapping olmalı
            entity.HasIndex(e => new { e.KatanaProductId, e.IsActive }).IsUnique();
            
            // Luca stock code'a göre arama için index
            entity.HasIndex(e => e.LucaStockCode);
            
            // Sync durumuna göre arama için index
            entity.HasIndex(e => e.SyncStatus);
            
            // Son sync zamanına göre arama için index
            entity.HasIndex(e => e.SyncedAt);
        });

        // Tax Rate Mapping
        modelBuilder.Entity<TaxRateMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaTaxRateId).IsRequired();
            entity.Property(e => e.KozaKdvOran).IsRequired().HasPrecision(5, 4); // 0.1800 format
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Her Katana tax_rate_id için sadece 1 mapping olmalı
            entity.HasIndex(e => e.KatanaTaxRateId).IsUnique();
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });

        // UoM Mapping
        modelBuilder.Entity<UoMMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KatanaUoMString).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KozaOlcumBirimiId).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.SyncStatus).HasDefaultValue("PENDING").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Her UoM string için sadece 1 mapping olmalı (case-insensitive)
            entity.HasIndex(e => e.KatanaUoMString).IsUnique();
            
            // Sync tracking için index
            entity.HasIndex(e => e.SyncStatus);
            entity.HasIndex(e => e.LastSyncAt);
        });
        
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            
            entity.HasIndex(e => new { e.Level, e.CreatedAt })
                .HasDatabaseName("IX_ErrorLogs_Level_CreatedAt");
        });

        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            
            entity.HasIndex(a => new { a.EntityName, a.ActionType, a.Timestamp })
                .HasDatabaseName("IX_AuditLogs_EntityName_ActionType_Timestamp");
        });

        
        
    }
    private static void SeedData(ModelBuilder modelBuilder)
    {
        var seedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<MappingTable>().HasData(
            new MappingTable
            {
                Id = 1,
                MappingType = "SKU_ACCOUNT",
                SourceValue = "DEFAULT",
                TargetValue = "600.01",
                Description = "Default account code for unmapped products",
                IsActive = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            },
            new MappingTable
            {
                Id = 2,
                MappingType = "LOCATION_WAREHOUSE",
                SourceValue = "DEFAULT",
                TargetValue = "MAIN",
                Description = "Default warehouse code for unmapped locations",
                IsActive = true,
                CreatedAt = seedTimestamp,
                UpdatedAt = seedTimestamp
            }
        );
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified or EntityState.Added or EntityState.Deleted)
            .Where(e => e.Entity is not AuditLogEntity && e.Entity is not ErrorLog)
            .ToList();

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case Product product:
                    if (entry.State == EntityState.Modified) product.UpdatedAt = now;
                    break;
                case Category category:
                    if (entry.State == EntityState.Modified) category.UpdatedAt = now;
                    break;
                case User user:
                    if (entry.State == EntityState.Modified) user.UpdatedAt = now;
                    break;
                case Customer customer:
                    if (entry.State == EntityState.Modified) customer.UpdatedAt = now;
                    break;
                case Invoice invoice:
                    if (entry.State == EntityState.Modified) invoice.UpdatedAt = now;
                    break;
                case MappingTable mapping:
                    if (entry.State == EntityState.Modified) mapping.UpdatedAt = now;
                    break;
                case Supplier supplier:
                    if (entry.State == EntityState.Modified) supplier.UpdatedAt = now;
                    break;
                case PurchaseOrder po:
                    if (entry.State == EntityState.Modified) po.UpdatedAt = now;
                    break;
            }

            var (entityId, display) = GetEntityIdAndDisplay(entry);
            var action = MapStateToAction(entry.State);
            var changes = BuildChanges(entry);
            var performedBy = _currentUser?.Username ?? "System";
            var ip = _currentUser?.IpAddress;
            var ua = _currentUser?.UserAgent;

            var details = BuildDetails(entry.Entity.GetType().Name, entityId, display, action, changes);

            AuditLogs.Add(new AuditLogEntity
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entityId,
                ActionType = action,
                PerformedBy = performedBy,
                Timestamp = now,
                Details = details,
                Changes = changes,
                IpAddress = ip,
                UserAgent = ua
            });
        }

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            
            if (_pendingQueue != null)
            {
                try
                {
                    var audits = ChangeTracker.Entries()
                        .Where(e => e.Entity is AuditLogEntity)
                        .Select(e => e.Entity as AuditLogEntity)
                        .Where(a => a != null)
                        .ToList()!;

                    foreach (var a in audits)
                    {
                        var dto = new Katana.Core.Services.PendingAuditInfo
                        {
                            ActionType = a!.ActionType,
                            EntityName = a.EntityName,
                            EntityId = a.EntityId,
                            PerformedBy = a.PerformedBy,
                            Timestamp = a.Timestamp,
                            Details = a.Details,
                            Changes = a.Changes,
                            IpAddress = a.IpAddress,
                            UserAgent = a.UserAgent
                        };
                        _pendingQueue.EnqueueAudit(dto);
                    }
                }
                catch {  }
            }
            return 0; 
        }
    }

    public override int SaveChanges()
    {
        
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static string MapStateToAction(EntityState state)
        => state switch
        {
            EntityState.Added => "CREATE",
            EntityState.Modified => "UPDATE",
            EntityState.Deleted => "DELETE",
            _ => state.ToString().ToUpperInvariant()
        };

    private static string? BuildChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.State == EntityState.Modified)
        {
            var pairs = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => $"{p.Metadata.Name}: {FormatVal(p.OriginalValue)} -> {FormatVal(p.CurrentValue)}")
                .ToList();
            return pairs.Count > 0 ? string.Join(", ", pairs) : null;
        }
        if (entry.State == EntityState.Added)
        {
            
            var keys = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).ToHashSet() ?? new HashSet<string>();
            var samples = entry.Properties
                .Where(p => !keys.Contains(p.Metadata.Name))
                .Where(p => p.Metadata.Name is "Name" or "SKU" or "InvoiceNo" or "ExternalOrderId" or "Price" or "Stock")
                .Select(p => $"{p.Metadata.Name}: {FormatVal(p.CurrentValue)}")
                .ToList();
            return samples.Count > 0 ? string.Join(", ", samples) : null;
        }
        if (entry.State == EntityState.Deleted)
        {
            var keys = entry.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name) ?? Enumerable.Empty<string>();
            var keyPairs = keys
                .Select(k => $"{k}: {FormatVal(entry.Property(k).OriginalValue)}");
            return string.Join(", ", keyPairs);
        }
        return null;
    }

    private static string BuildDetails(string entityName, string? entityId, string? display, string action, string? changes)
    {
        var header = display != null
            ? $"{entityName} '{display}' ({entityId ?? "-"}) {ToTr(action)}"
            : $"{entityName}#{entityId ?? "-"} {ToTr(action)}";
        if (!string.IsNullOrWhiteSpace(changes))
        {
            return $"{header}. Değişiklikler: {changes}";
        }
        return header;
    }

    private static (string? id, string? display) GetEntityIdAndDisplay(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        string? id = null;
        var key = entry.Metadata.FindPrimaryKey();
        if (key != null)
        {
            var parts = key.Properties
                .Select(p => entry.Property(p.Name))
                .Select(p => p.CurrentValue ?? p.OriginalValue)
                .Where(v => v != null)
                .Select(v => v!.ToString());
            id = string.Join("-", parts);
        }

        
        string? display = null;
        foreach (var name in new[] { "Name", "SKU", "InvoiceNo", "ExternalOrderId", "Title" })
        {
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == name);
            if (prop != null)
            {
                display = (prop.CurrentValue ?? prop.OriginalValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(display)) break;
            }
        }
        return (string.IsNullOrWhiteSpace(id) ? null : id, display);
    }

    private static string ToTr(string action)
        => action switch
        {
            "CREATE" => "oluşturuldu",
            "UPDATE" => "güncellendi",
            "DELETE" => "silindi",
            _ => action.ToLowerInvariant()
        };

    private static string FormatVal(object? v)
    {
        if (v == null) return "null";
        return v is DateTime dt ? dt.ToString("s") : v.ToString() ?? string.Empty;
    }
}
