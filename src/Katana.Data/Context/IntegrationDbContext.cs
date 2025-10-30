﻿using Katana.Core.Entities;
using Katana.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace Katana.Data.Context;
public class IntegrationDbContext : DbContext
{
    private readonly Katana.Core.Services.PendingDbWriteQueue? _pendingQueue;

    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options, Katana.Core.Services.PendingDbWriteQueue? pendingQueue = null) : base(options)
    {
        _pendingQueue = pendingQueue;
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
    public DbSet<PendingStockAdjustment> PendingStockAdjustments { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
     public DbSet<OrderItem> OrderItems { get; set; }
     public DbSet<Supplier> Suppliers { get; set; }
      public DbSet<SupplierPrice> SupplierPrices { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<Katana.Core.Entities.User> Users { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    var isSqlite = Database.IsSqlite();

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
        // Category hierarchy + constraints
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
            // Same parent cannot have duplicate category names
            entity.HasIndex(e => new { e.ParentId, e.Name }).IsUnique();
        });

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
            entity.HasIndex(e => e.ActionType);
            entity.Property(e => e.Timestamp).IsRequired();
        });

        // PendingStockAdjustment configuration
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

        // ErrorLog configuration ✅
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Category);
            // SQLite 'nvarchar(max)' desteklemez; TEXT kullan
            if (isSqlite)
            {
                entity.Property(e => e.StackTrace).HasColumnType("TEXT");
            }
            else
            {
                entity.Property(e => e.StackTrace).HasColumnType("nvarchar(max)");
            }
            entity.Property(e => e.ContextData).HasMaxLength(1000);
        });

        // PendingStockAdjustment configuration
        modelBuilder.Entity<PendingStockAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalOrderId).IsUnique();
            entity.Property(e => e.ExternalOrderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Sku).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.RequestedBy).HasMaxLength(100).IsRequired();
        });

        // Supplier configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // SupplierPrice relationships
        modelBuilder.Entity<SupplierPrice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(sp => sp.Supplier)
                .WithMany(s => s.PriceList)
                .HasForeignKey(sp => sp.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PurchaseOrder relationships
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AccountingRecord>(entity =>
        {
            if (!isSqlite)
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            }
            // SQLite tarafında varsayılan numeric mapping yeterlidir
        });

        // User configuration
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

        // Seed data
        // SeedData(modelBuilder); // ❌ Geçici olarak kapalı
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

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            // If SQL Server is unavailable or login failed, queue the audit logs (if any) and swallow the exception
                if (_pendingQueue != null)
                {
                    // Extract pending audit logs we added earlier in this method and convert to DTO
                    var audits = ChangeTracker.Entries()
                        .Where(e => e.Entity is AuditLog)
                        .Select(e => e.Entity as AuditLog)
                        .Where(a => a != null)
                        .ToList()!;

                    foreach (var a in audits)
                    {
                        try
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
                        catch { /* ignore */ }
                    }
                }

            // Log and swallow so higher-level operations continue (they should handle eventual consistency)
            return 0;
        }
        catch (Exception)
        {
            throw;
        }
    }
}
