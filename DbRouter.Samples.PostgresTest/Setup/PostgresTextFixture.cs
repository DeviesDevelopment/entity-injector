using DbRouter.Samples.PostgresTest.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DbRouter.Samples.PostgresTest.Setup;

public class PostgresTestFixture : IAsyncLifetime
{
    public TestDbContext DbContext { get; private set; } = default!;
    public Guid SeedId { get; private set; }

    public async Task InitializeAsync()
    {
        var connectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres";

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        DbContext = new TestDbContext(options);

        // Wait for the database to be ready (retry loop)
        var attempts = 0;
        while (attempts++ < 10)
        {
            try
            {
                await DbContext.Users.FirstOrDefaultAsync(); // minimal smoke test
                break;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        // Optionally grab an existing user's ID to use in tests
        var firstUser = await DbContext.Users.FirstOrDefaultAsync();
        SeedId = firstUser?.Id ?? Guid.Empty;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
