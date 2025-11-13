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
    }

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Users> Users { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<LoginHistory> LoginHistories { get; set; }
    public DbSet<LoginRequest> LoginRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplySoftDeleteGlobalFilter();
        modelBuilder.ApplyAuditPrecision();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        DbSeedData.Seed(modelBuilder);
    }
}