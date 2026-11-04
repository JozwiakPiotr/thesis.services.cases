using Microsoft.EntityFrameworkCore;
using PaymentsApp.ApiService.Domain;

namespace PaymentsApp.ApiService.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    public DbSet<FinancialAccount> Accounts => Set<FinancialAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<FinancialAccount>();

        entity.ToTable("financial_accounts");
        entity.HasKey(account => account.AccountId);
        entity.HasIndex(account => account.CaseId).IsUnique();
        entity.Property(account => account.Currency)
            .HasMaxLength(3)
            .IsRequired();
        entity.Property(account => account.PrincipalOriginal).HasPrecision(18, 2);
        entity.Property(account => account.PrincipalPaid).HasPrecision(18, 2);
        entity.Property(account => account.InterestAccrued).HasPrecision(18, 2);
        entity.Property(account => account.InterestPaid).HasPrecision(18, 2);
        entity.Property(account => account.AdditionalCosts).HasPrecision(18, 2);
        entity.Property(account => account.AdditionalCostsPaid).HasPrecision(18, 2);
    }
}
