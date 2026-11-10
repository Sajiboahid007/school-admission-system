using AdmissionApi.Common;
using AdmissionApi.Data;
using AdmissionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AdmissionApi.Services;

public class HoldExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<HoldExpiryWorker> _logger;

    public HoldExpiryWorker(IServiceScopeFactory scopeFactory, IClock clock, ILogger<HoldExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                var now = _clock.UtcNow;

                var count = await db.Holds
                    .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
                    .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), stoppingToken);

                if (count > 0)
                    _logger.LogInformation("Marked {Count} holds as expired", count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Expiry job failed, will retry");
            }
        }
    }
}
