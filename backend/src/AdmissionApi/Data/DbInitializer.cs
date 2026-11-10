using Microsoft.EntityFrameworkCore;

namespace AdmissionApi.Data;

public static class DbInitializer
{
    // In Docker the backend can start before Postgres accepts connections, so retry.
    public static async Task MigrateAsync(IServiceProvider services, ILogger logger)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                await db.Database.MigrateAsync();
                logger.LogInformation("Database is up to date");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
