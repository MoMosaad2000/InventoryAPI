using InventoryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Data
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseStock> WarehouseStocks { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StockInVoucher> StockInVouchers { get; set; }
        public DbSet<StockOutVoucher> StockOutVouchers { get; set; }
        public DbSet<StockInVoucherItem> StockInVoucherItems { get; set; }
        public DbSet<StockOutVoucherItem> StockOutVoucherItems { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<OperationOrders> OperationOrders { get; set; }
        public DbSet<SalesOrder> SalesOrder { get; set; }
        public DbSet<SalesOrderItem> SalesOrderItem { get; set; }
        public DbSet<FinalProduct> FinalProducts { get; set; }
        public DbSet<InvoiceAttachment> InvoiceAttachments { get; set; }

        public DbSet<FinalProductComponent> FinalProductComponents { get; set; }
        public DbSet<IndirectCost> IndirectCosts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Category>().ToTable("Categories");

            modelBuilder.Entity<StockInVoucher>()
                .HasMany(siv => siv.Items)
                .WithOne(item => item.StockInVoucher)
                .HasForeignKey(item => item.StockInVoucherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockInVoucherItem>()
                .HasOne(item => item.Supplier)
                .WithMany()
                .HasForeignKey(item => item.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInVoucherItem>()
                .HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockInVoucherItem>()
                .HasOne(item => item.Warehouse)
                .WithMany()
                .HasForeignKey(item => item.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockOutVoucher>()
                .HasOne(s => s.Customer)
                .WithMany(c => c.StockOutVouchers)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}