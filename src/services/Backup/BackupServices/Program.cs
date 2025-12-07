using BackupServices.Configuration;
using BackupServices.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<SourceCustomerDbConfiguration>(
            configuration.GetSection(SourceCustomerDbConfiguration.SectionName));
        services.Configure<SourceOrderDbConfiguration>(
            configuration.GetSection(SourceOrderDbConfiguration.SectionName));

        services.Configure<BackupCustomerDbConfiguration>(
            configuration.GetSection(BackupCustomerDbConfiguration.SectionName));
        services.Configure<BackupOrderDbConfiguration>(
            configuration.GetSection(BackupOrderDbConfiguration.SectionName));

        services.Configure<BackupScheduleConfiguration>(
            configuration.GetSection(BackupScheduleConfiguration.SectionName));

        services.AddScoped<ISyncService, CustomerSyncService>();
        services.AddScoped<ISyncService, OrderSyncService>();
    })
    .Build();

host.Run();
