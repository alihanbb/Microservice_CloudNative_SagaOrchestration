# ?? Health Checks Quick Reference

## ?? URLs

| Endpoint | URL | Purpose |
|----------|-----|---------|
| **Health Checks UI** | http://localhost:5001/healthchecks-ui | Visual Dashboard |
| **Health JSON** | http://localhost:5001/health | Full health report |
| **Readiness Probe** | http://localhost:5001/ready | K8s readiness |
| **Liveness Probe** | http://localhost:5001/live | K8s liveness |
| **Swagger** | http://localhost:5001/swagger | API Documentation |

## ?? Health Check Status

| Status | Color | Meaning |
|--------|-------|---------|
| ? Healthy | Green | All systems operational |
| ?? Degraded | Yellow | Working but performance issues |
| ? Unhealthy | Red | System not working |

## ?? Components Monitored

### Self Check ?
- **Purpose**: API availability
- **Status**: Always healthy
- **Tags**: `api`, `ready`

### Memory Check ??
- **Purpose**: Memory usage monitoring
- **Threshold**: 1 GB
- **Tags**: `api`, `memory`
- **Status**: Healthy < 1GB, Degraded > 1GB

### Cosmos DB Check ???
- **Purpose**: Database connectivity
- **Timeout**: 10 seconds
- **Tags**: `database`, `cosmosdb`, `ready`
- **Status**: Healthy if connected, Unhealthy if failed

## ? Quick Commands

### cURL
```bash
# All health checks
curl http://localhost:5001/health

# Pretty print
curl http://localhost:5001/health | jq

# Readiness only
curl http://localhost:5001/ready

# Liveness only
curl http://localhost:5001/live
```

### PowerShell
```powershell
# Health check
Invoke-RestMethod -Uri http://localhost:5001/health

# Format JSON
(Invoke-RestMethod -Uri http://localhost:5001/health) | ConvertTo-Json -Depth 10
```

### Watch Health Status
```bash
# Linux/Mac - refresh every 5 seconds
watch -n 5 curl -s http://localhost:5001/health | jq

# Windows PowerShell
while($true) { 
    Clear-Host
    Invoke-RestMethod http://localhost:5001/health | ConvertTo-Json
    Start-Sleep -Seconds 5
}
```

## ?? Docker

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD curl -f http://localhost:5001/health || exit 1
```

## ?? Kubernetes

```yaml
livenessProbe:
  httpGet:
    path: /live
    port: 5001
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /ready
    port: 5001
  initialDelaySeconds: 10
  periodSeconds: 5
```

## ?? Response Example

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "API is running",
      "tags": ["api", "ready"]
    },
    "memory": {
      "status": "Healthy",
      "description": "Memory usage: 256 MB",
      "tags": ["api", "memory"]
    },
    "cosmosdb": {
      "status": "Healthy",
      "description": "Cosmos DB is responding",
      "tags": ["database", "cosmosdb", "ready"]
    }
  }
}
```

## ?? Configuration

**Refresh Interval**: 30 seconds  
**History Entries**: 50 per endpoint  
**Cosmos DB Timeout**: 10 seconds  
**Memory Threshold**: 1 GB  

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| UI not loading | Check `/healthchecks-ui` path |
| Cosmos DB unhealthy | Verify connection string |
| High memory | Check for memory leaks |
| Slow response | Increase timeout settings |

## ?? Support

For issues, check:
1. Application logs
2. `/health` endpoint directly
3. Cosmos DB connection
4. Memory usage

---

**Last Updated**: 2024
