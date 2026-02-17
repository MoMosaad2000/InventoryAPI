// Data/InventoryDbContext.cs
using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
            : base(options)
        {
        }

        // ====== الأساسيات ======
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }

        // ====== الحسابات والتكاليف ======
        public DbSet<Account> Accounts { get; set; }
        public DbSet<FinalProduct> FinalProducts { get; set; }
        public DbSet<FinalProductComponent> FinalProductComponents { get; set; }
        public DbSet<IndirectCost> IndirectCosts { get; set; }

        public DbSet<OperationCost> OperationCosts { get; set; }
        public DbSet<OperationCostItem> OperationCostItems { get; set; }

        // ====== أوامر التشغيل ======
        public DbSet<OperationOrders> OperationOrders { get; set; }
        public DbSet<OperationOrderItem> OperationOrderItems { get; set; }

        // ====== أوامر البيع ======
        public DbSet<SalesOrder> SalesOrder { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }   // عشان errors CS1061
        public DbSet<SalesOrderItem> SalesOrderItem { get; set; }

        // ====== ✅ عرض سعر (موديول جديد مستقل) ======
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationLine> QuotationLines { get; set; }

        // ✅ إيصالات قبض عروض الأسعار
        public DbSet<PaymentReceipt> PaymentReceipts { get; set; }

        // ====== المخزون / سندات الإضافة والصرف والتحويل ======
        public DbSet<StockInVoucher> StockInVouchers { get; set; }
        public DbSet<StockInVoucherItem> StockInVoucherItems { get; set; }

        public DbSet<StockOutVoucher> StockOutVouchers { get; set; }
        public DbSet<StockOutVoucherItem> StockOutVoucherItems { get; set; }

        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }

        public DbSet<WarehouseStock> WarehouseStocks { get; set; }

        // ====== فواتير الشراء ======
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<InvoiceAttachment> InvoiceAttachments { get; set; }

        // ====== سندات مواد التشغيل ======
        public DbSet<MaterialIssue> MaterialIssues { get; set; }
        public DbSet<MaterialIssueLine> MaterialIssueLines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinalProductComponent>()
                .HasOne(c => c.FinalProduct)
                .WithMany(p => p.Components)
                .HasForeignKey(c => c.FinalProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IndirectCost>()
                .HasOne(c => c.FinalProduct)
                .WithMany(p => p.IndirectCosts)
                .HasForeignKey(c => c.FinalProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationOrderItem>()
                .HasOne(i => i.OperationOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OperationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderItem>()
                .HasOne(i => i.SalesOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaterialIssueLine>()
                .HasOne(l => l.MaterialIssue)
                .WithMany(m => m.Lines)
                .HasForeignKey(l => l.MaterialIssueId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockTransferItem>()
                .HasOne(i => i.StockTransfer)
                .WithMany(t => t.Items)
                .HasForeignKey(i => i.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockInVoucherItem>()
                .HasOne(i => i.StockInVoucher)
                .WithMany(v => v.Items)
                .HasForeignKey(i => i.StockInVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockOutVoucherItem>()
                .HasOne(i => i.StockOutVoucher)
                .WithMany(v => v.Items)
                .HasForeignKey(i => i.StockOutVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            // ====== ✅ Quotation -> Lines ======
            modelBuilder.Entity<Quotation>()
                .HasMany(q => q.Lines)
                .WithOne(l => l.Quotation)
                .HasForeignKey(l => l.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quotation>()
                .Property(q => q.TaxRate)
                .HasDefaultValue(0.15m);

            // ====== ✅ Quotation (1) <-> (1) PaymentReceipt ======
            modelBuilder.Entity<Quotation>()
                .HasOne(q => q.PaymentReceipt)
                .WithOne(r => r.Quotation)
                .HasForeignKey<PaymentReceipt>(r => r.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentReceipt>()
                .HasIndex(r => r.QuotationId)
                .IsUnique();
        }
    }
}
