using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Stock)
            .WithOne(s => s.Product)
            .HasForeignKey<Stock>(s => s.ProductCode);

        modelBuilder.Entity<Stock>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0");
            t.HasCheckConstraint("CK_Stock_Reserved", "[Reserved] >= 0 AND [Reserved] <= [Quantity]");
        });

        modelBuilder.Entity<CartItem>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductCode);

        modelBuilder.Entity<CartItem>()
            .HasIndex(c => new { c.CartId, c.ProductCode })
            .IsUnique();

        // Mock up Product data
        modelBuilder.Entity<Product>().HasData(
            new Product { ProductCode = "P001", Name = "Mechanical Keyboard", UnitPrice = 2590.00m },
            new Product { ProductCode = "P002", Name = "Wireless Mouse", UnitPrice = 890.00m },
            new Product { ProductCode = "P003", Name = "27\" Monitor", UnitPrice = 6990.00m },
            new Product { ProductCode = "P004", Name = "USB-C Hub", UnitPrice = 1250.00m },
            new Product { ProductCode = "P005", Name = "Noise Cancelling Headphones", UnitPrice = 4990.00m },
            new Product { ProductCode = "P006", Name = "Webcam 1080p", UnitPrice = 1490.00m }
        );

        // Mock up Stock data
        modelBuilder.Entity<Stock>().HasData(
            new Stock { ProductCode = "P001", Quantity = 10 },
            new Stock { ProductCode = "P002", Quantity = 25 },
            new Stock { ProductCode = "P003", Quantity = 3 },
            new Stock { ProductCode = "P004", Quantity = 15 },
            new Stock { ProductCode = "P005", Quantity = 5 },
            new Stock { ProductCode = "P006", Quantity = 0 }
        );
    }
}
