using Microsoft.EntityFrameworkCore;

namespace DiscountingEngine.Models
{
    public class OfferProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int? OfferProductId { get; set; }
        public OfferProduct? OfferProduct { get; set; }
    }

    public class DiscountingContext : DbContext
    {
        public DiscountingContext(DbContextOptions<DiscountingContext> options) : base(options)
        {
        }

        public DbSet<OfferProduct> OfferProducts { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.OfferProduct)
                .WithMany()
                .HasForeignKey(i => i.OfferProductId)
                .IsRequired(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}