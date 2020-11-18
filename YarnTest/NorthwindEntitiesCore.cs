using Yarn.Test.Models.EF;
using Microsoft.EntityFrameworkCore;

namespace Yarn.Test
{
    public class NorthwindEntitiesCore : DbContext
    {  
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=localhost;Initial Catalog=Northwind;User Id=sa;Password=Passw0rd;;App=EFCore");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<CustomerDemographic>();
            modelBuilder.Ignore<Territory>();
            modelBuilder.Entity<Customer>().Ignore(c => c.CustomerDemographics);
            
            modelBuilder.Entity<Order_Detail>(b =>
            {
                b.ToTable("Order Details");
                b.HasKey(d => new { d.OrderID, d.ProductID });

                b.HasOne(d => d.Order)
                   .WithMany(p => p.Order_Details)
                   .HasForeignKey(d => d.OrderID)
                   .OnDelete(DeleteBehavior.ClientSetNull);

                b.HasOne(d => d.Product)
                    .WithMany(p => p.Order_Details)
                    .HasForeignKey(d => d.ProductID)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Order>(b =>
            {
                b.HasOne(d => d.Customer)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.CustomerID);

                b.HasOne(d => d.Employee)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.EmployeeID);

                b.HasOne(d => d.Shipper)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.ShipVia);
            });
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<CustomerDemographic> CustomerDemographics { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Order_Detail> Order_Details { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Shipper> Shippers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Territory> Territories { get; set; }
    }
}
