using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResponsabiliMano.Infrastructure.Data;

namespace ResponsabiliMano.Infrastructure.Tests.TestHelpers;

/// <summary>
/// Builds an <see cref="AppDbContext"/> backed by a private SQLite in-memory
/// database. SQLite is relational, so it honours cascade deletes and unique
/// constraints faithfully (unlike the EF Core in-memory provider).
/// </summary>
/// <remarks>
/// The connection is opened before it is handed to EF. EF only closes
/// connections it opens itself, so the connection (and therefore the in-memory
/// database) stays alive for the whole lifetime of the context. Each call
/// creates an isolated database.
/// </remarks>
internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
