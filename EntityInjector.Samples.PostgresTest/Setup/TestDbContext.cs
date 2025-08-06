using EntityInjector.Samples.PostgresTest.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityInjector.Samples.PostgresTest.Setup;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Product>().ToTable("products");
    }
}