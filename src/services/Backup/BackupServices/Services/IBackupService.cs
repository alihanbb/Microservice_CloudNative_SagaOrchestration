using BackupServices.Models;

namespace BackupServices.Services;

public interface ISyncService
{
    Task<SyncResult> SyncAsync(bool forceFullSync = false, CancellationToken cancellationToken = default);
    
    Task InitializeBackupDatabaseAsync(CancellationToken cancellationToken = default);
    
    string ServiceName { get; }
}
