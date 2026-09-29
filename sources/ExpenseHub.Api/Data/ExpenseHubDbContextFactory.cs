using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseHub.Api.Data;

/// <summary>
/// Fábrica usada apenas pelas ferramentas de design do EF Core (migrations).
/// </summary>
public sealed class ExpenseHubDbContextFactory : IDesignTimeDbContextFactory<ExpenseHubDbContext>
{
    /// <inheritdoc />
    public ExpenseHubDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<ExpenseHubDbContext> options = new DbContextOptionsBuilder<ExpenseHubDbContext>()
            .UseSqlite("Data Source=expensehub.db")
            .Options;

        return new ExpenseHubDbContext(options);
    }
}
