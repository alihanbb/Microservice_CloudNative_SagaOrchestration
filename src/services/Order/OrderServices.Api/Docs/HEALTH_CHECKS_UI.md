# ?? Health Checks UI - Order Service

## ?? Genel Bakýþ

Order Service, **ASP.NET Core Health Checks UI** kullanarak profesyonel bir health monitoring sistemi içerir.

## ?? Endpoints

### 1. **Health Check Endpoint** - `/health`
Tüm health check'leri çalýþtýrýr ve detaylý JSON response döner.

**Response Example:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "API is running",
      "duration": "00:00:00.0001234",
      "tags": ["api", "ready"]
    },
    "memory": {
      "status": "Healthy",
      "description": "Memory usage: 256 MB",
      "duration": "00:00:00.0000123",
      "tags": ["api", "memory"]
    },
    "cosmosdb": {
      "status": "Healthy",
      "description": "Cosmos DB is responding",
      "duration": "00:00:00.0234567",
      "tags": ["database", "cosmosdb", "ready"]
    }
  }
}
```

### 2. **Readiness Probe** - `/ready`
Kubernetes readiness probe için kullanýlýr. Sadece kritik baðýmlýlýklarý kontrol eder.

**Tags:** `ready`
- ? Self check
- ? Cosmos DB

### 3. **Liveness Probe** - `/live`
Kubernetes liveness probe için kullanýlýr. API'nin çalýþýp çalýþmadýðýný kontrol eder.

**Tags:** `api`
- ? Self check
- ? Memory check

### 4. **Health Checks UI** - `/healthchecks-ui`
Görsel health monitoring dashboard.

**Features:**
- ?? Real-time health status
- ?? Historical data (last 50 entries)
- ?? Response times
- ?? Webhook notifications (optional)
- ?? Beautiful UI

### 5. **Health Checks API** - `/healthchecks-api`
Health Checks UI tarafýndan kullanýlan JSON API endpoint.

## ?? Kullaným

### cURL Commands

```bash
# Health Check - Detailed
curl http://localhost:5001/health

# Readiness Probe
curl http://localhost:5001/ready

# Liveness Probe
curl http://localhost:5001/live

# Pretty JSON Output
curl http://localhost:5001/health | jq
```

### PowerShell Commands

```powershell
# Health Check
Invoke-RestMethod -Uri "http://localhost:5001/health" | ConvertTo-Json -Depth 10

# Readiness
Invoke-RestMethod -Uri "http://localhost:5001/ready"

# Liveness
Invoke-RestMethod -Uri "http://localhost:5001/live"
```

### Browser Access

- **Health Checks UI**: http://localhost:5001/healthchecks-ui
- **Health JSON**: http://localhost:5001/health
- **Swagger**: http://localhost:5001/swagger

## ??? Architecture

```
???????????????????????????????????????????????????
?           Health Check System                   ?
???????????????????????????????????????????????????
?                                                  ?
?  ????????????????  ????????????????            ?
?  ?   Self Check ?  ? Memory Check ?            ?
?  ?  (API Ready) ?  ? (< 1GB RAM)  ?            ?
?  ????????????????  ????????????????            ?
?                                                  ?
?  ????????????????????????????????               ?
?  ?    Cosmos DB Health Check    ?               ?
?  ?  - Connection test           ?               ?
?  ?  - Database availability     ?               ?
?  ?  - 10 second timeout         ?               ?
?  ????????????????????????????????               ?
?                                                  ?
?  ????????????????????????????????               ?
?  ?    Health Checks UI          ?               ?
?  ?  - Real-time monitoring      ?               ?
?  ?  - Historical data           ?               ?
?  ?  - Response time tracking    ?               ?
?  ?  - 30 second refresh         ?               ?
?  ????????????????????????????????               ?
???????????????????????????????????????????????????
```

## ?? Configuration

### appsettings.json

```json
{
  "HealthChecksUI": {
    "HealthChecks": [
      {
        "Name": "OrderService API",
        "Uri": "/health"
      }
    ],
    "EvaluationTimeInSeconds": 30,
    "MinimumSecondsBetweenFailureNotifications": 60
  }
}
```

### Extension Method

```csharp
// Program.cs
builder.Services.AddOrderServiceHealthChecks(builder.Configuration);
builder.Services.AddOrderServiceHealthChecksUI();

// Middleware
app.UseOrderServiceHealthChecks();
```

## ?? Health Check Components

### 1. Self Check ?
```csharp
.AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
```
- Always returns healthy
- Indicates API is running

### 2. Memory Check ??
```csharp
.AddCheck("memory", () => {
    var allocated = GC.GetTotalMemory(false);
    var threshold = 1GB;
    return allocated < threshold ? Healthy : Degraded;
})
```
- Monitors memory usage
- Threshold: 1 GB
- Status: Healthy | Degraded

### 3. Cosmos DB Check ???
```csharp
.AddCosmosDb(
    connectionString: configuration.GetConnectionString("cosmos"),
    database: cosmosConfig.DatabaseName,
    timeout: TimeSpan.FromSeconds(10)
)
```
- Tests Cosmos DB connection
- Database availability check
- 10 second timeout

## ?? UI Features

### Dashboard Elements
- **Status Badge**: Healthy (Green), Degraded (Yellow), Unhealthy (Red)
- **Response Time**: Average, Min, Max
- **Success Rate**: % of successful checks
- **Last Check**: Timestamp of last execution
- **History Graph**: Visual timeline of health status

### Refresh Settings
- **Auto Refresh**: Every 30 seconds
- **Manual Refresh**: Click refresh button
- **History**: Last 50 entries per endpoint

## ?? Webhook Notifications (Optional)

```csharp
setup.AddWebhookNotification("slack-webhook",
    uri: "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
    payload: "{ \"text\": \"? OrderService is unhealthy!\" }",
    restorePayload: "{ \"text\": \"? OrderService is healthy again!\" }"
);
```

### Supported Integrations
- Slack
- Microsoft Teams
- Discord
- Custom webhooks
- Email (with additional packages)

## ?? Docker Health Checks

### Dockerfile
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5001/health || exit 1
```

### Docker Compose
```yaml
services:
  orderservice:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5001/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

## ?? Kubernetes Health Checks

### Deployment YAML
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: orderservice
spec:
  template:
    spec:
      containers:
      - name: orderservice
        image: orderservice:latest
        ports:
        - containerPort: 5001
        
        # Liveness probe
        livenessProbe:
          httpGet:
            path: /live
            port: 5001
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        # Readiness probe
        readinessProbe:
          httpGet:
            path: /ready
            port: 5001
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
```

## ?? Monitoring & Alerts

### Prometheus Integration (Future)
```csharp
builder.Services.AddHealthChecks()
    .ForwardToPrometheus();
```

### Application Insights (Future)
```csharp
builder.Services.AddHealthChecks()
    .AddApplicationInsightsPublisher();
```

## ?? Testing

### Integration Tests
```csharp
[Fact]
public async Task HealthCheck_ReturnsHealthy()
{
    var response = await _client.GetAsync("/health");
    response.EnsureSuccessStatusCode();
    
    var content = await response.Content.ReadAsStringAsync();
    var healthReport = JsonSerializer.Deserialize<HealthReport>(content);
    
    Assert.Equal(HealthStatus.Healthy, healthReport.Status);
}
```

## ?? Best Practices

1. **Timeout Settings**: Cosmos DB check has 10s timeout
2. **Tag Strategy**: Use tags for different probe types
3. **Memory Threshold**: Adjust based on container limits
4. **Refresh Interval**: 30s is optimal for most scenarios
5. **History Retention**: Keep last 50 entries
6. **Webhook Throttling**: 60s between failure notifications

## ?? Troubleshooting

### Health Check Fails
```bash
# Check Cosmos DB connection
curl http://localhost:5001/health | jq '.entries.cosmosdb'

# Check memory usage
curl http://localhost:5001/health | jq '.entries.memory'
```

### UI Not Loading
- Verify `/healthchecks-ui` path
- Check in-memory storage is registered
- Ensure middleware order is correct

### Cosmos DB Unhealthy
- Verify connection string
- Check network connectivity
- Confirm database/container exists
- Review timeout settings

## ?? Resources

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Health Checks UI](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
- [Kubernetes Health Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)

---

**Created with** ?? **for robust monitoring and observability**
