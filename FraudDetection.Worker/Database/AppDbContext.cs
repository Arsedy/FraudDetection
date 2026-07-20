using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AuthorizationTransaction> AuthorizationTransactions { get; set; } = null!;
    public DbSet<FraudAlert> FraudAlerts { get; set; } = null!;
    public DbSet<Rule> Rules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Define relationship between FraudAlert and AuthorizationTransaction
        modelBuilder.Entity<FraudAlert>()
            .HasOne(f => f.Transaction)
            .WithMany()
            .HasForeignKey(f => f.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Define relationship between FraudAlert and Rule
        modelBuilder.Entity<FraudAlert>()
            .HasOne(f => f.Rule)
            .WithMany()
            .HasForeignKey(f => f.RuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mark F37_RRN as unique index in AuthorizationTransactions
        //F37—RETRIEVAL REFERENCE NUMBER
        modelBuilder.Entity<AuthorizationTransaction>()
            .HasIndex(a => a.F37_RRN)
            .IsUnique();

        // Configure indexes on AuthorizationTransactions for performance
        modelBuilder.Entity<AuthorizationTransaction>()
            .HasIndex(a => new { a.F2_PAN, a.F7_TxnDateTime });//Primary Account Number

    }
}
