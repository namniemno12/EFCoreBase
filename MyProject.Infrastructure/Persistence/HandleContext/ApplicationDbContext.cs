using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.HandleContext;

public class ApplicationDbContext : DbContext
{
    private static string connectionString = string.Empty;
    public ApplicationDbContext()
    {
        var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

        connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    public ApplicationDbContext([NotNull] DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        ChangeTracker.LazyLoadingEnabled = false;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.LogTo(Console.WriteLine);
        optionsBuilder.UseSqlServer("Server=DESKTOP-ADMIN\\SQLEXPRESS;Database=StreamieDB2;Trusted_Connection=True;");
    }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplySoftDeleteGlobalFilter();
        modelBuilder.ApplyAuditPrecision();

        //modelBuilder.Entity<NguyenEntity>().ToTable("NGUYEN");
        //modelBuilder.Entity<NguyenEntity>(entity => { entity.HasKey(c => c.Id); });

        //modelBuilder.Entity<Testtiep>().ToTable("EntityTest");
        //modelBuilder.Entity<Testtiep>(entity => { entity.HasKey(c => c.Id); });
    }
}