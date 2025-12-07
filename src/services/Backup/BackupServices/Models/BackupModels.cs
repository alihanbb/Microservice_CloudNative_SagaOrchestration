namespace BackupServices.Models;

public class SyncResult
{
    public Guid SyncId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public SyncStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
    
    public int TotalProcessed => InsertedCount + UpdatedCount + DeletedCount;
}

public enum SyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class SyncRequest
{
    public string Service { get; set; } = "all";
    
    public bool ForceFullSync { get; set; } = false;
}

public class SyncHistoryRecord
{
    public Guid SyncId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime SyncedAt { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerSyncEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneCountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int Version { get; set; }
}

public class OrderSyncEntity
{
    public string Id { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Type { get; set; } = "Order";
    public DateTime? LastModified { get; set; }
    public List<OrderItemSyncEntity> OrderItems { get; set; } = new();
}

public class OrderItemSyncEntity
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
