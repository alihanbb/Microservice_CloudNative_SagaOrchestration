namespace BackupServices.Configuration;

public class SourceCustomerDbConfiguration
{
    public const string SectionName = "SourceCustomerDb";
    
    public string ConnectionString { get; set; } = string.Empty;
}

public class BackupCustomerDbConfiguration
{
    public const string SectionName = "BackupCustomerDb";
    
    public string ConnectionString { get; set; } = string.Empty;
}

public class SourceOrderDbConfiguration
{
    public const string SectionName = "SourceOrderDb";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "orderdb";
    public string ContainerName { get; set; } = "orders";
}

public class BackupOrderDbConfiguration
{
    public const string SectionName = "BackupOrderDb";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "orderdb-backup";
    public string ContainerName { get; set; } = "orders";
}

public class BackupScheduleConfiguration
{
    public const string SectionName = "BackupSchedule";
    
    public string CronExpression { get; set; } = "0 0 0 * * *";
    
    public bool Enabled { get; set; } = true;
}

public class BackupTrackingConfiguration
{
    public const string SectionName = "BackupTracking";
    
    public int RetentionDays { get; set; } = 30;
}
