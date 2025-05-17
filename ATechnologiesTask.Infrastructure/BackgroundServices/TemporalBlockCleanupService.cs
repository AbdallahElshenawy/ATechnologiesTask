using ATechnologiesTask.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace ATechnologiesTask.Infrastructure.BackgroundServices;

public class TemporalBlockCleanupService(IBlockedCountryRepository blockedCountryRepository, 
    ILogger<TemporalBlockCleanupService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TemporalBlockCleanupService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int removedCount = await blockedCountryRepository.RemoveExpiredTemporalBlocksAsync();
                if (removedCount > 0)
                {
                    logger.LogInformation("Removed {RemovedCount} expired temporal blocks.", removedCount);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while removing expired temporal blocks.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("TemporalBlockCleanupService is stopping.");
    }
}