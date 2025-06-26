using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        // الجداول الأساسية
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }

        // جداول الفواتير والمستندات
        public DbSet<StockInVoucher> StockInVouchers { get; set; }
        public DbSet<StockOutVoucher> StockOutVouchers { get; set; }
        public DbSet<StockInVoucherItem> StockInVoucherItems { get; set; }
        public DbSet<StockOutVoucherItem> StockOutVoucherItems { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<InvoiceAttachment> InvoiceAttachments { get; set; }

        // جداول المبيعات
        public DbSet<SalesOrder> SalesOrder { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItem { get; set; }

        // جداول العمليات
        public DbSet<OperationOrders> OperationOrders { get; set; }
        public DbSet<OperationOrderItem> OperationOrderItems { get; set; }

        // جداول المنتجات النهائية
        public DbSet<FinalProduct> FinalProducts { get; set; }
        public DbSet<FinalProductComponent> FinalProductComponents { get; set; }
        public DbSet<IndirectCost> IndirectCosts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تهيئة العلاقات الأساسية
            modelBuilder.Entity<StockInVoucher>()
                .HasMany(siv => siv.Items)
                .WithOne()
                .HasForeignKey(item => item.StockInVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockOutVoucher>()
                .HasMany(sov => sov.Items)
                .WithOne()
                .HasForeignKey(item => item.StockOutVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrder>()
                .HasMany(so => so.Items)
                .WithOne()
                .HasForeignKey(item => item.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationOrders>()
                .HasMany(oo => oo.Items)
                .WithOne()
                .HasForeignKey(item => item.OperationOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockTransfer>()
                .HasMany(st => st.Items)
                .WithOne()
                .HasForeignKey(item => item.StockTransferId)
                .OnDelete(DeleteBehavior.Cascade);

            // تهيئة جداول المنتج النهائي
            modelBuilder.Entity<FinalProduct>()
                .HasMany(p => p.Components)
                .WithOne()
                .HasForeignKey(c => c.FinalProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FinalProduct>()
                .HasMany(p => p.IndirectCosts)
                .WithOne()
                .HasForeignKey(ic => ic.FinalProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationOrders>()
              .HasMany(o => o.Items)
              .WithOne()
              .HasForeignKey(i => i.OperationOrderId)
             .OnDelete(DeleteBehavior.Cascade);

        }
    }
}