using BackupServices.Models;
using BackupServices.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BackupServices.Functions;

public class ScheduledBackupFunction
{
    private readonly ILogger<ScheduledBackupFunction> _logger;
    private readonly IEnumerable<ISyncService> _syncServices;

    public ScheduledBackupFunction(
        ILogger<ScheduledBackupFunction> logger,
        IEnumerable<ISyncService> syncServices)
    {
        _logger = logger;
        _syncServices = syncServices;
    }

    [Function("ScheduledSync")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Scheduled sync started at {Time}", DateTime.UtcNow);
        _logger.LogInformation("========================================");

        var results = new List<SyncResult>();

        foreach (var service in _syncServices)
        {
            try
            {
                _logger.LogInformation("Running scheduled sync for {ServiceName}...", service.ServiceName);

                var result = await service.SyncAsync(forceFullSync: false);
                results.Add(result);

                if (result.Status == SyncStatus.Completed)
                {
                    _logger.LogInformation(
                        "✓ {ServiceName} sync completed. Inserted: {Inserted}, Updated: {Updated}, Deleted: {Deleted}, Skipped: {Skipped}",
                        service.ServiceName,
                        result.InsertedCount,
                        result.UpdatedCount,
                        result.DeletedCount,
                        result.SkippedCount);
                }
                else
                {
                    _logger.LogError(
                        "✗ {ServiceName} sync failed: {Error}",
                        service.ServiceName,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled sync for {ServiceName}", service.ServiceName);
                results.Add(new SyncResult
                {
                    ServiceName = service.ServiceName,
                    Status = SyncStatus.Failed,
                    ErrorMessage = ex.Message,
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
            }
        }

        var successCount = results.Count(r => r.Status == SyncStatus.Completed);
        var failCount = results.Count(r => r.Status == SyncStatus.Failed);
        var totalInserted = results.Sum(r => r.InsertedCount);
        var totalUpdated = results.Sum(r => r.UpdatedCount);
        var totalDeleted = results.Sum(r => r.DeletedCount);

        _logger.LogInformation("========================================");
        _logger.LogInformation("Scheduled sync summary:");
        _logger.LogInformation("  Success: {SuccessCount}, Failed: {FailCount}", successCount, failCount);
        _logger.LogInformation("  Total Inserted: {Inserted}, Updated: {Updated}, Deleted: {Deleted}",
            totalInserted, totalUpdated, totalDeleted);
        _logger.LogInformation("========================================");

        if (timerInfo.ScheduleStatus != null)
        {
            _logger.LogInformation("Next scheduled sync at {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
