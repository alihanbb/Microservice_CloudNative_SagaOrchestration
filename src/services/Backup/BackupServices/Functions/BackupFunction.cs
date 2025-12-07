using BackupServices.Models;
using BackupServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BackupServices.Functions;

public class BackupFunction
{
    private readonly ILogger<BackupFunction> _logger;
    private readonly IEnumerable<ISyncService> _syncServices;

    public BackupFunction(
        ILogger<BackupFunction> logger,
        IEnumerable<ISyncService> syncServices)
    {
        _logger = logger;
        _syncServices = syncServices;
    }

    [Function("TriggerSync")]
    public async Task<IActionResult> TriggerSync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "backup/sync")] HttpRequest req)
    {
        _logger.LogInformation("Sync triggered at {Time}", DateTime.UtcNow);

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var syncRequest = string.IsNullOrEmpty(requestBody)
                ? new SyncRequest()
                : JsonConvert.DeserializeObject<SyncRequest>(requestBody) ?? new SyncRequest();

            var results = new List<SyncResult>();
            var servicesToSync = GetServicesToSync(syncRequest.Service);

            foreach (var service in servicesToSync)
            {
                _logger.LogInformation("Starting sync for service: {ServiceName}", service.ServiceName);
                var result = await service.SyncAsync(syncRequest.ForceFullSync);
                results.Add(result);
            }

            var allSucceeded = results.All(r => r.Status == SyncStatus.Completed);

            return new OkObjectResult(new
            {
                Success = allSucceeded,
                Message = allSucceeded
                    ? "All syncs completed successfully"
                    : "Some syncs failed",
                Results = results.Select(r => new
                {
                    r.SyncId,
                    r.ServiceName,
                    Status = r.Status.ToString(),
                    r.InsertedCount,
                    r.UpdatedCount,
                    r.DeletedCount,
                    r.SkippedCount,
                    r.TotalProcessed,
                    r.StartedAt,
                    r.CompletedAt,
                    r.ErrorMessage
                }),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync operation");
            return new ObjectResult(new
            {
                Success = false,
                Message = "Sync operation failed",
                Error = ex.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("SyncCustomer")]
    public async Task<IActionResult> SyncCustomer(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "backup/sync/customer")] HttpRequest req)
    {
        _logger.LogInformation("Customer sync triggered at {Time}", DateTime.UtcNow);

        try
        {
            var forceFullSync = req.Query["force"].FirstOrDefault()?.ToLower() == "true";
            
            var customerService = _syncServices.FirstOrDefault(s => s.ServiceName == "customer");
            if (customerService == null)
            {
                return new NotFoundObjectResult(new { Error = "Customer sync service not configured" });
            }

            var result = await customerService.SyncAsync(forceFullSync);

            return new OkObjectResult(new
            {
                Success = result.Status == SyncStatus.Completed,
                result.SyncId,
                result.ServiceName,
                Status = result.Status.ToString(),
                result.InsertedCount,
                result.UpdatedCount,
                result.DeletedCount,
                result.SkippedCount,
                result.TotalProcessed,
                result.StartedAt,
                result.CompletedAt,
                result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer sync");
            return new ObjectResult(new { Error = ex.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("SyncOrder")]
    public async Task<IActionResult> SyncOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "backup/sync/order")] HttpRequest req)
    {
        _logger.LogInformation("Order sync triggered at {Time}", DateTime.UtcNow);

        try
        {
            var forceFullSync = req.Query["force"].FirstOrDefault()?.ToLower() == "true";
            
            var orderService = _syncServices.FirstOrDefault(s => s.ServiceName == "order");
            if (orderService == null)
            {
                return new NotFoundObjectResult(new { Error = "Order sync service not configured" });
            }

            var result = await orderService.SyncAsync(forceFullSync);

            return new OkObjectResult(new
            {
                Success = result.Status == SyncStatus.Completed,
                result.SyncId,
                result.ServiceName,
                Status = result.Status.ToString(),
                result.InsertedCount,
                result.UpdatedCount,
                result.DeletedCount,
                result.SkippedCount,
                result.TotalProcessed,
                result.StartedAt,
                result.CompletedAt,
                result.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during order sync");
            return new ObjectResult(new { Error = ex.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    [Function("InitializeBackup")]
    public async Task<IActionResult> InitializeBackup(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "backup/initialize")] HttpRequest req)
    {
        _logger.LogInformation("Backup database initialization triggered at {Time}", DateTime.UtcNow);

        try
        {
            var results = new List<object>();

            foreach (var service in _syncServices)
            {
                try
                {
                    await service.InitializeBackupDatabaseAsync();
                    results.Add(new { Service = service.ServiceName, Status = "Initialized" });
                }
                catch (Exception ex)
                {
                    results.Add(new { Service = service.ServiceName, Status = "Failed", Error = ex.Message });
                }
            }

            return new OkObjectResult(new
            {
                Success = results.All(r => ((dynamic)r).Status == "Initialized"),
                Results = results,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup initialization");
            return new ObjectResult(new { Error = ex.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    private IEnumerable<ISyncService> GetServicesToSync(string service)
    {
        return service.ToLower() switch
        {
            "customer" => _syncServices.Where(s => s.ServiceName == "customer"),
            "order" => _syncServices.Where(s => s.ServiceName == "order"),
            _ => _syncServices // "all" or default
        };
    }
}
