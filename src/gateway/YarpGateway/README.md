# YARP API Gateway

This is the API Gateway for the microservices architecture using YARP (Yet Another Reverse Proxy) with .NET Aspire integration.

## Features

- ? **YARP Reverse Proxy**: Routes requests to microservices
- ? **Service Discovery**: Automatic service discovery with Aspire
- ? **Health Checks**: Monitors gateway and downstream services
- ? **Health Checks UI**: Visual dashboard at `/healthchecks-ui`
- ? **Swagger/OpenAPI**: API documentation at `/swagger`
- ? **Azure Key Vault**: Secure configuration management
- ? **Load Balancing**: Built-in load balancing capabilities
- ? **Active Health Checks**: Monitors backend service health

## Endpoints

### API Routes

The gateway routes requests to the following microservices:

| Route Pattern | Target Service | Description |
|--------------|----------------|-------------|
| `/api/orders/**` | Order Service | Order management operations |
| `/api/customers/**` | Customer Service | Customer management operations |
| `/api/products/**` | Product Service | Product management operations |

### Health Check Endpoints

| Endpoint | Description |
|----------|-------------|
| `/health` | Overall gateway health status |
| `/ready` | Readiness probe for Kubernetes |
| `/live` | Liveness probe for Kubernetes |
| `/healthchecks-ui` | Visual health checks dashboard |
| `/healthchecks-api` | Health checks API for UI |

### Documentation

| Endpoint | Description |
|----------|-------------|
| `/swagger` | Swagger UI for API documentation |

## Configuration

### YARP Configuration (appsettings.json)

The gateway is configured via `appsettings.json` with routes and clusters:

```json
{
  "ReverseProxy": {
    "Routes": {
      "order-route": {
        "ClusterId": "order-cluster",
        "Match": {
          "Path": "/api/orders/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "order-cluster": {
        "Destinations": {
          "order-service": {
            "Address": "http://orderservice"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Path": "/health"
          }
        }
      }
    }
  }
}
```

### Service Discovery

The gateway uses Aspire's service discovery to automatically resolve service addresses. Services are referenced by their logical names (e.g., `orderservice`, `customerservice`, `productservice`).

### Environment Variables

Key environment variables:

- `ASPNETCORE_ENVIRONMENT`: Application environment (Development/Staging/Production)
- `ASPNETCORE_URLS`: URLs the gateway listens on

## Development

### Running Locally

1. Ensure all microservices are running
2. Run the gateway:
   ```bash
   dotnet run
   ```
3. Access the gateway at `http://localhost:5000`

### Running with Aspire

The gateway is automatically configured when running through Aspire AppHost:

```bash
dotnet run --project aspire/Microservice.AppHost
```

## Architecture

```
???????????????
?   Client    ?
???????????????
       ?
       ?
???????????????????????????????????????
?         YARP Gateway                ?
?  - Service Discovery                ?
?  - Load Balancing                   ?
?  - Health Checks                    ?
?  - Request Routing                  ?
??????????????????????????????????????
    ?          ?          ?
    ?          ?          ?
??????????? ???????????? ???????????
? Order   ? ?Customer  ? ?Product  ?
?Service  ? ?Service   ? ?Service  ?
??????????? ???????????? ???????????
```

## Load Balancing

The gateway supports multiple load balancing policies:

- **RoundRobin** (default): Distributes requests evenly
- **LeastRequests**: Routes to the service with fewest active requests
- **Random**: Random distribution

Configure in `appsettings.json`:

```json
{
  "LoadBalancingPolicy": {
    "Mode": "RoundRobin"
  }
}
```

## Health Checks

### Gateway Health

The gateway monitors its own health including:
- Memory usage
- Self-health status

### Downstream Services Health

Active health checks monitor backend services:
- Interval: 30 seconds
- Timeout: 10 seconds
- Path: `/health`

Unhealthy services are automatically removed from the load balancing pool.

## Monitoring

### Health Checks UI

Access the Health Checks UI at `/healthchecks-ui` to view:
- Real-time health status of all services
- Historical health data
- Response times
- Failure notifications

## Security

- **HTTPS**: Redirects HTTP to HTTPS in production
- **CORS**: Configurable CORS policies
- **Azure Key Vault**: Secure storage of secrets and configuration

## Production Considerations

1. **Rate Limiting**: Add rate limiting middleware for production
2. **Authentication**: Implement OAuth2/JWT authentication
3. **Caching**: Add response caching for frequently accessed data
4. **Logging**: Configure structured logging (Application Insights, Seq, etc.)
5. **Metrics**: Enable OpenTelemetry for distributed tracing
6. **Circuit Breaker**: Implement circuit breaker pattern for resilience

## Troubleshooting

### Service Not Reachable

1. Check service discovery is enabled
2. Verify service names match in AppHost configuration
3. Check health check endpoints return 200 OK

### Gateway Returning 503

1. Check downstream services are running
2. Verify health check paths are correct
3. Review logs for connection errors

### Performance Issues

1. Monitor health checks UI for slow services
2. Check load balancing configuration
3. Review gateway logs for bottlenecks

## References

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
