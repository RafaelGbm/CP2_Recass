using System;
using ExpenseHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core do ExpenseHub, incluindo as tabelas do Identity.
/// </summary>
/// <param name="options">Opções do contexto, incluindo o provider relacional.</param>
public class ExpenseHubDbContext(DbContextOptions<ExpenseHubDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    /// <summary>Reembolsos.</summary>
    public DbSet<Expense> Expenses => Set<Expense>();

    /// <summary>Categorias de reembolso.</summary>
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    /// <summary>Histórico de ações sobre os reembolsos.</summary>
    public DbSet<ExpenseHistory> ExpenseHistories => Set<ExpenseHistory>();

    /// <summary>Pagamentos registrados.</summary>
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.Entity<ExpenseCategory>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(ExpenseCategory.NameMaxLength);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasData(
                new ExpenseCategory { Id = 1, Name = "Transporte" },
                new ExpenseCategory { Id = 2, Name = "Alimentação" },
                new ExpenseCategory { Id = 3, Name = "Hospedagem" },
                new ExpenseCategory { Id = 4, Name = "Outros" });
        });

        builder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Status);
        });

        builder.Entity<ExpenseHistory>(entity =>
        {
            entity.Property(h => h.Action).IsRequired().HasMaxLength(ExpenseHistory.ActionMaxLength);
            entity.Property(h => h.ActorId).IsRequired().HasMaxLength(450);
            entity.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.Justification).HasMaxLength(ExpenseHistory.JustificationMaxLength);
            entity.Property(h => h.Changes).HasMaxLength(ExpenseHistory.ChangesMaxLength);
            entity.HasOne<Expense>().WithMany().HasForeignKey(h => h.ExpenseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(h => h.ActorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(h => h.ExpenseId);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.Property(p => p.PaidById).IsRequired().HasMaxLength(450);
            entity.HasOne<Expense>().WithMany().HasForeignKey(p => p.ExpenseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(p => p.PaidById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.ExpenseId).IsUnique();
        });
    }
}
