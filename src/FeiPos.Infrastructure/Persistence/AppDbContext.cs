using Microsoft.EntityFrameworkCore;
using FeiPos.Domain.Entities;

namespace FeiPos.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<HaciendaResponse> HaciendaResponses { get; set; }
        public DbSet<CashDrawerEntry> CashDrawerEntries { get; set; }
        public DbSet<DayClosure> DayClosures { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<CustomerCreditPayment> CustomerCreditPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuraciones específicas (Fluent API)
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.Items)
                .WithOne()
                .HasForeignKey("SaleId");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<CustomerCreditPayment>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId);
        }
    }
}
