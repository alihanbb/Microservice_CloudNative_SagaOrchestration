using BackupServices.Configuration;
using BackupServices.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackupServices.Services;

public class CustomerSyncService : ISyncService
{
    private readonly SourceCustomerDbConfiguration _sourceConfig;
    private readonly BackupCustomerDbConfiguration _backupConfig;
    private readonly ILogger<CustomerSyncService> _logger;

    public string ServiceName => "customer";

    public CustomerSyncService(
        IOptions<SourceCustomerDbConfiguration> sourceConfig,
        IOptions<BackupCustomerDbConfiguration> backupConfig,
        ILogger<CustomerSyncService> logger)
    {
        _sourceConfig = sourceConfig.Value;
        _backupConfig = backupConfig.Value;
        _logger = logger;
    }

    public async Task InitializeBackupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Customer backup database schema...");

        await using var connection = new SqlConnection(_backupConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var createDbSql = @"
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CustomerDb_Backup')
            BEGIN
                CREATE DATABASE CustomerDb_Backup
            END";
        
        await using (var cmd = new SqlCommand(createDbSql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await connection.ChangeDatabaseAsync("CustomerDb_Backup", cancellationToken);

        var createSchemaSql = @"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'customer')
            BEGIN
                EXEC('CREATE SCHEMA customer')
            END";
        
        await using (var cmd = new SqlCommand(createSchemaSql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers' AND schema_id = SCHEMA_ID('customer'))
            BEGIN
                CREATE TABLE customer.Customers (
                    Id INT PRIMARY KEY,
                    FirstName NVARCHAR(100) NOT NULL,
                    LastName NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(256) NOT NULL,
                    PhoneCountryCode NVARCHAR(10) NULL,
                    PhoneNumber NVARCHAR(20) NULL,
                    Street NVARCHAR(200) NULL,
                    City NVARCHAR(100) NULL,
                    State NVARCHAR(100) NULL,
                    Country NVARCHAR(100) NULL,
                    ZipCode NVARCHAR(20) NULL,
                    StatusId INT NOT NULL,
                    StatusName NVARCHAR(50) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    ModifiedAt DATETIME2 NULL,
                    Version INT NOT NULL,
                    SyncedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                )
            END";
        
        await using (var cmd = new SqlCommand(createTableSql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        var createHistorySql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SyncHistory' AND schema_id = SCHEMA_ID('customer'))
            BEGIN
                CREATE TABLE customer.SyncHistory (
                    SyncId UNIQUEIDENTIFIER PRIMARY KEY,
                    SyncedAt DATETIME2 NOT NULL,
                    InsertedCount INT NOT NULL,
                    UpdatedCount INT NOT NULL,
                    DeletedCount INT NOT NULL,
                    Success BIT NOT NULL,
                    ErrorMessage NVARCHAR(MAX) NULL
                )
            END";
        
        await using (var cmd = new SqlCommand(createHistorySql, connection))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("Customer backup database schema initialized");
    }

    public async Task<SyncResult> SyncAsync(bool forceFullSync = false, CancellationToken cancellationToken = default)
    {
        var syncId = Guid.NewGuid();
        var result = new SyncResult
        {
            SyncId = syncId,
            ServiceName = ServiceName,
            Status = SyncStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting Customer incremental sync. SyncId: {SyncId}, ForceFullSync: {ForceFullSync}",
                syncId, forceFullSync);

            await InitializeBackupDatabaseAsync(cancellationToken);

            var sourceCustomers = await GetSourceCustomersAsync(cancellationToken);
            _logger.LogInformation("Found {Count} customers in source database", sourceCustomers.Count);

            var backupCustomers = await GetBackupCustomersAsync(cancellationToken);
            var backupDict = backupCustomers.ToDictionary(c => c.Id);

            await using var backupConnection = new SqlConnection(_backupConfig.ConnectionString);
            await backupConnection.OpenAsync(cancellationToken);
            await backupConnection.ChangeDatabaseAsync("CustomerDb_Backup", cancellationToken);

            foreach (var customer in sourceCustomers)
            {
                if (backupDict.TryGetValue(customer.Id, out var existing))
                {
                    if (customer.Version > existing.Version || forceFullSync)
                    {
                        await UpdateCustomerAsync(backupConnection, customer, cancellationToken);
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                    backupDict.Remove(customer.Id);
                }
                else
                {
                    await InsertCustomerAsync(backupConnection, customer, cancellationToken);
                    result.InsertedCount++;
                }
            }

            foreach (var deleted in backupDict.Values)
            {
                await DeleteCustomerAsync(backupConnection, deleted.Id, cancellationToken);
                result.DeletedCount++;
            }

            await RecordSyncHistoryAsync(backupConnection, result, cancellationToken);

            result.Status = SyncStatus.Completed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Customer sync completed. Inserted: {Inserted}, Updated: {Updated}, Deleted: {Deleted}, Skipped: {Skipped}",
                result.InsertedCount, result.UpdatedCount, result.DeletedCount, result.SkippedCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer sync failed. SyncId: {SyncId}", syncId);
            result.Status = SyncStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }
    }

    private async Task<List<CustomerSyncEntity>> GetSourceCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = new List<CustomerSyncEntity>();

        await using var connection = new SqlConnection(_sourceConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var query = @"
            SELECT 
                Id, FirstName, LastName, Email,
                PhoneCountryCode, PhoneNumber,
                Street, City, State, Country, ZipCode,
                StatusId, StatusName,
                CreatedAt, ModifiedAt, Version
            FROM customer.Customers
            ORDER BY Id";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(MapCustomerFromReader(reader));
        }

        return customers;
    }

    private async Task<List<CustomerSyncEntity>> GetBackupCustomersAsync(CancellationToken cancellationToken)
    {
        var customers = new List<CustomerSyncEntity>();

        try
        {
            await using var connection = new SqlConnection(_backupConfig.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await connection.ChangeDatabaseAsync("CustomerDb_Backup", cancellationToken);

            var query = "SELECT Id, FirstName, LastName, Email, PhoneCountryCode, PhoneNumber, Street, City, State, Country, ZipCode, StatusId, StatusName, CreatedAt, ModifiedAt, Version FROM customer.Customers";

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                customers.Add(MapCustomerFromReader(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read backup customers (table may not exist yet)");
        }

        return customers;
    }

    private CustomerSyncEntity MapCustomerFromReader(SqlDataReader reader)
    {
        return new CustomerSyncEntity
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Email = reader.GetString(3),
            PhoneCountryCode = reader.IsDBNull(4) ? null : reader.GetString(4),
            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
            Street = reader.IsDBNull(6) ? null : reader.GetString(6),
            City = reader.IsDBNull(7) ? null : reader.GetString(7),
            State = reader.IsDBNull(8) ? null : reader.GetString(8),
            Country = reader.IsDBNull(9) ? null : reader.GetString(9),
            ZipCode = reader.IsDBNull(10) ? null : reader.GetString(10),
            StatusId = reader.GetInt32(11),
            StatusName = reader.GetString(12),
            CreatedAt = reader.GetDateTime(13),
            ModifiedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
            Version = reader.GetInt32(15)
        };
    }

    private async Task InsertCustomerAsync(SqlConnection connection, CustomerSyncEntity customer, CancellationToken cancellationToken)
    {
        var sql = @"
            INSERT INTO customer.Customers 
            (Id, FirstName, LastName, Email, PhoneCountryCode, PhoneNumber, Street, City, State, Country, ZipCode, StatusId, StatusName, CreatedAt, ModifiedAt, Version, SyncedAt)
            VALUES 
            (@Id, @FirstName, @LastName, @Email, @PhoneCountryCode, @PhoneNumber, @Street, @City, @State, @Country, @ZipCode, @StatusId, @StatusName, @CreatedAt, @ModifiedAt, @Version, GETUTCDATE())";

        await using var cmd = new SqlCommand(sql, connection);
        AddCustomerParameters(cmd, customer);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateCustomerAsync(SqlConnection connection, CustomerSyncEntity customer, CancellationToken cancellationToken)
    {
        var sql = @"
            UPDATE customer.Customers SET
                FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                PhoneCountryCode = @PhoneCountryCode,
                PhoneNumber = @PhoneNumber,
                Street = @Street,
                City = @City,
                State = @State,
                Country = @Country,
                ZipCode = @ZipCode,
                StatusId = @StatusId,
                StatusName = @StatusName,
                CreatedAt = @CreatedAt,
                ModifiedAt = @ModifiedAt,
                Version = @Version,
                SyncedAt = GETUTCDATE()
            WHERE Id = @Id";

        await using var cmd = new SqlCommand(sql, connection);
        AddCustomerParameters(cmd, customer);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeleteCustomerAsync(SqlConnection connection, int id, CancellationToken cancellationToken)
    {
        var sql = "DELETE FROM customer.Customers WHERE Id = @Id";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddCustomerParameters(SqlCommand cmd, CustomerSyncEntity customer)
    {
        cmd.Parameters.AddWithValue("@Id", customer.Id);
        cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
        cmd.Parameters.AddWithValue("@LastName", customer.LastName);
        cmd.Parameters.AddWithValue("@Email", customer.Email);
        cmd.Parameters.AddWithValue("@PhoneCountryCode", (object?)customer.PhoneCountryCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PhoneNumber", (object?)customer.PhoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Street", (object?)customer.Street ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@City", (object?)customer.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@State", (object?)customer.State ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Country", (object?)customer.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ZipCode", (object?)customer.ZipCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StatusId", customer.StatusId);
        cmd.Parameters.AddWithValue("@StatusName", customer.StatusName);
        cmd.Parameters.AddWithValue("@CreatedAt", customer.CreatedAt);
        cmd.Parameters.AddWithValue("@ModifiedAt", (object?)customer.ModifiedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Version", customer.Version);
    }

    private async Task RecordSyncHistoryAsync(SqlConnection connection, SyncResult result, CancellationToken cancellationToken)
    {
        var sql = @"
            INSERT INTO customer.SyncHistory (SyncId, SyncedAt, InsertedCount, UpdatedCount, DeletedCount, Success, ErrorMessage)
            VALUES (@SyncId, GETUTCDATE(), @InsertedCount, @UpdatedCount, @DeletedCount, @Success, @ErrorMessage)";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@SyncId", result.SyncId);
        cmd.Parameters.AddWithValue("@InsertedCount", result.InsertedCount);
        cmd.Parameters.AddWithValue("@UpdatedCount", result.UpdatedCount);
        cmd.Parameters.AddWithValue("@DeletedCount", result.DeletedCount);
        cmd.Parameters.AddWithValue("@Success", result.Status == SyncStatus.Completed);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)result.ErrorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
