using InvoiceFlow.Intake.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace InvoiceFlow.Intake.Tests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        await using IntakeDbContext dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
    }

    internal IntakeDbContext CreateDbContext()
    {
        DbContextOptions<IntakeDbContext> options =
            new DbContextOptionsBuilder<IntakeDbContext>()
            .UseSqlServer(
                _sqlServer.GetConnectionString(),
                sql => sql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    IntakeDbContext.Schema))
            .Options;

        return new IntakeDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration tests";
}
