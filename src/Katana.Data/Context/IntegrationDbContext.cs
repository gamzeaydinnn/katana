using Katana.Core.Entities;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace Katana.Data.Context;
public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options)
    {
    }
    // Core entities
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Stock> Stocks { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<SyncOperationLog> SyncOperationLogs { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
    public DbSet<AccountingRecord> AccountingRecords { get; set; } = null!;

    // Integration specific entities
    public DbSet<IntegrationLog> IntegrationLogs { get; set; } = null!;
    public DbSet<MappingTable> MappingTables { get; set; } = null!;
    public DbSet<FailedSyncRecord> FailedSyncRecords { get; set; } = null!;
    //public DbSet<SyncLog> SyncLogs { get; set; } = null!;
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!; // ✅ Audit kayıtları için
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
     public DbSet<OrderItem> OrderItems { get; set; }
     public DbSet<Supplier> Suppliers { get; set; }
      public DbSet<SupplierPrice> SupplierPrices { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<Katana.Core.Entities.User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ✅ SyncLog configuration
    modelBuilder.Entity<SyncOperationLog>(entity =>
    {
        entity.ToTable("SyncLogs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.SyncType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        entity.Property(e => e.StartTime).IsRequired();
        entity.Property(e => e.Details).HasColumnType("TEXT");
    });

    // Product configuration
    modelBuilder.Entity<Product>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => e.SKU).IsUnique();
        entity.Property(e => e.Price).HasPrecision(18, 2);
        
        entity.HasMany(e => e.StockMovements)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    });


        // Stock configuration
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProductId, e.Timestamp });
            entity.HasIndex(e => e.IsSynced);
        });
        //kategori 
        modelBuilder.Entity<Category>()
    .HasMany(c => c.Children)
    .WithOne(c => c.Parent)
    .HasForeignKey(c => c.ParentId)
    .OnDelete(DeleteBehavior.Restrict);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TaxNo).IsUnique();

            entity.HasMany(e => e.Invoices)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice configuration
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

        // InvoiceItem configuration
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

        // IntegrationLog configuration
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

        // MappingTable configuration
        modelBuilder.Entity<MappingTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MappingType, e.SourceValue }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // FailedSyncRecord configuration
        modelBuilder.Entity<FailedSyncRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RecordType, e.Status });
            entity.HasIndex(e => e.NextRetryAt);
        });

        // AuditLog configuration ✅
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityName);
            entity.HasIndex(e => e.Action);
            entity.Property(e => e.Timestamp).IsRequired();
        });

        // Seed data
        SeedData(modelBuilder);
    }
    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MappingTable>().HasData(
            new MappingTable
            {
                Id = 1,
                MappingType = "SKU_ACCOUNT",
                SourceValue = "DEFAULT",
                TargetValue = "600.01",
                Description = "Default account code for unmapped products",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MappingTable
            {
                Id = 2,
                MappingType = "LOCATION_WAREHOUSE",
                SourceValue = "DEFAULT",
                TargetValue = "MAIN",
                Description = "Default warehouse code for unmapped locations",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified or EntityState.Added or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case Product product:
                    if (entry.State == EntityState.Modified) product.UpdatedAt = now;
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
            }

            // ✅ Audit log kaydı oluştur
            var audit = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                ActionType = entry.State.ToString(),
                Timestamp = now,
                Changes = string.Join(", ", entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => $"{p.Metadata.Name}: {p.OriginalValue} -> {p.CurrentValue}")
                )
            };

            AuditLogs.Add(audit);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
