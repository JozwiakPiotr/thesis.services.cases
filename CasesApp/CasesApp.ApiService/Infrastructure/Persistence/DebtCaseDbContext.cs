using CasesApp.ApiService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CasesApp.ApiService.Infrastructure.Persistence;

public sealed class DebtCaseDbContext(DbContextOptions<DebtCaseDbContext> options)
    : DbContext(options)
{
    public DbSet<DebtCase> DebtCases => Set<DebtCase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<DebtCase>();

        entity.ToTable("debt_cases");
        entity.HasKey(debtCase => debtCase.Id);
        entity.Property(debtCase => debtCase.Id)
            .ValueGeneratedNever();
        entity.Property(debtCase => debtCase.CaseNumber)
            .HasMaxLength(200)
            .IsRequired();
        entity.Property(debtCase => debtCase.ExternalReferenceNumber).IsRequired();
        entity.Property(debtCase => debtCase.DocumentNumber).IsRequired();
        entity.Property(debtCase => debtCase.LifecycleStatus)
            .HasConversion<int>()
            .IsRequired();
        entity.Property(debtCase => debtCase.OperationalStatus)
            .HasConversion<int>()
            .IsRequired();
        entity.OwnsOne(debtCase => debtCase.Debtor, debtor =>
        {
            debtor.ToTable("debtors");
            debtor.HasKey(item => item.Id);
            debtor.OwnsMany(item => item.Addresses, address =>
            {
                address.ToTable("addresses");
                address.HasKey(item => item.Id);
            });
        });
    }
}
