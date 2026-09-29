using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();

        string connectionString = builder.Configuration.GetConnectionString("ExpenseHub")
            ?? "Data Source=expensehub.db";
        builder.Services.AddDbContext<ExpenseHubDbContext>(options => options.UseSqlite(connectionString));

        WebApplication app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            using IServiceScope scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ExpenseHubDbContext>().Database.Migrate();
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");

        app.Run();
    }
}
