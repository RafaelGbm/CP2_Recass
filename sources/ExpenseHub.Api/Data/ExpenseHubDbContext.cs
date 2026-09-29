using System;
using ExpenseHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Contexto do Entity Framework Core do ExpenseHub.
/// </summary>
/// <param name="options">Opções do contexto, incluindo o provider relacional.</param>
public class ExpenseHubDbContext(DbContextOptions<ExpenseHubDbContext> options) : DbContext(options)
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
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExpenseCategory>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(ExpenseCategory.NameMaxLength);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasData(
                new ExpenseCategory { Id = 1, Name = "Transporte" },
                new ExpenseCategory { Id = 2, Name = "Alimentação" },
                new ExpenseCategory { Id = 3, Name = "Hospedagem" },
                new ExpenseCategory { Id = 4, Name = "Outros" });
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.Category).WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<ExpenseHistory>(entity =>
        {
            entity.Property(h => h.Action).IsRequired().HasMaxLength(ExpenseHistory.ActionMaxLength);
            entity.Property(h => h.ActorId).IsRequired().HasMaxLength(450);
            entity.Property(h => h.PreviousStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.Justification).HasMaxLength(ExpenseHistory.JustificationMaxLength);
            entity.HasOne<Expense>().WithMany().HasForeignKey(h => h.ExpenseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(h => h.ExpenseId);
        });

        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.Property(p => p.PaidById).IsRequired().HasMaxLength(450);
            entity.HasOne<Expense>().WithMany().HasForeignKey(p => p.ExpenseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.ExpenseId).IsUnique();
        });
    }
}
