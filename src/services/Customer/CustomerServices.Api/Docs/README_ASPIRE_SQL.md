# ?? CustomerService - Azure SQL & Aspire Configuration

## ?? Overview

CustomerService API configured with:
- ? **Azure SQL Database** (SQL Server)
- ? **Azure Key Vault**
- ? **Aspire Orchestration**
- ? **Adminer SQL UI**
- ? **CloudBeaver SQL UI** (Alternative)
- ? **Health Checks**
- ? **Entity Framework Core**

## ??? Architecture

```
???????????????????????????????????????????????????
?              Aspire AppHost                     ?
?  ????????????????????????????????????????????  ?
?  ?                                           ?  ?
?  ?   ????????????????    ????????????????  ?  ?
?  ?   ?  Customer    ??????  SQL Server  ?  ?  ?
?  ?   ?   Service    ?    ?   (Azure)    ?  ?  ?
?  ?   ????????????????    ????????????????  ?  ?
?  ?          ?                    ?          ?  ?
?  ?          ?             ????????????????  ?  ?
?  ?          ???????????????  Key Vault   ?  ?  ?
?  ?                        ?   (Azure)    ?  ?  ?
?  ?                        ????????????????  ?  ?
?  ?                                           ?  ?
?  ?   ????????????????  ????????????????    ?  ?
?  ?   ?   Adminer    ?  ? CloudBeaver  ?    ?  ?
?  ?   ?   (SQL UI)   ?  ?   (SQL UI)   ?    ?  ?
?  ?   ????????????????  ????????????????    ?  ?
?  ????????????????????????????????????????????  ?
???????????????????????????????????????????????????
```

## ?? Quick Start

### Option 1: Using Aspire AppHost (Recommended)

```bash
# 1. Set SQL Server password
cd aspire/Microservice.AppHost
dotnet user-secrets set "Parameters:sql-password" "YourStrong@Passw0rd"

# 2. Run Aspire AppHost
dotnet run

# Aspire will automatically:
# - Start SQL Server container
# - Start Adminer UI
# - Configure CustomerService
# - Apply database migrations
```

### Option 2: Using Docker Compose

```bash
# 1. Start SQL Server and UIs
cd aspire/Microservice.AppHost
docker-compose -f docker-compose.sqlserver.yml up -d

# 2. Run CustomerService
cd ../../src/services/Customer/CustomerServices.Api
dotnet run
```

## ?? Access URLs

| Service | URL | Description |
|---------|-----|-------------|
| **Aspire Dashboard** | http://localhost:15888 | Monitoring Dashboard |
| **CustomerService API** | http://localhost:5003 | REST API (HTTP) |
| **CustomerService HTTPS** | https://localhost:5004 | REST API (HTTPS) |
| **Swagger UI** | http://localhost:5003/swagger | API Documentation |
| **Adminer (SQL UI)** | http://localhost:8082 | SQL Management UI |
| **CloudBeaver** | http://localhost:8083 | Advanced SQL IDE |
| **Health Check** | http://localhost:5003/health | Health Endpoint |

## ??? SQL Server Connection

### Local Development
```
Server: localhost,1433
Database: customerdb
User: sa
Password: YourStrong@Passw0rd
```

### Adminer Login
```
System: MS SQL
Server: sqlserver (or localhost)
Username: sa
Password: YourStrong@Passw0rd
Database: customerdb
```

### CloudBeaver First-Time Setup
1. Navigate to http://localhost:8083
2. Create admin credentials
3. Add new connection:
   - **Driver**: Microsoft SQL Server
   - **Host**: sqlserver (or localhost)
   - **Port**: 1433
   - **Database**: customerdb
   - **Username**: sa
   - **Password**: YourStrong@Passw0rd

## ?? NuGet Packages

### CustomerServices.Api
```xml
<PackageReference Include="Aspire.Microsoft.EntityFrameworkCore.SqlServer" Version="9.5.0" />
<PackageReference Include="Aspire.Azure.Security.KeyVault" Version="9.5.0" />
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.5.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.11" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="9.0.11" />
```

### Aspire AppHost
```xml
<PackageReference Include="Aspire.Hosting.SqlServer" Version="9.5.0" />
```

## ?? Health Checks

### Endpoints
- `/health` - Full health report (self, memory, SQL Server)
- `/ready` - Kubernetes readiness probe
- `/live` - Kubernetes liveness probe

### Test Commands
```bash
# All health checks
curl http://localhost:5003/health

# Readiness
curl http://localhost:5003/ready

# Liveness
curl http://localhost:5003/live

# Pretty JSON
curl http://localhost:5003/health | jq
```

### Health Check Components
1. **Self Check**: API availability
2. **Memory Check**: Memory usage (threshold: 1GB)
3. **SQL Server Check**: Database connectivity via EF Core

## ??? Database Management

### Entity Framework Core Migrations

```bash
cd src/services/Customer/CustomerServices.Api

# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove

# List migrations
dotnet ef migrations list
```

### SQL Server Management

#### Option 1: Adminer (http://localhost:8082)
- Lightweight web-based tool
- Simple interface
- Query execution
- Table browsing

#### Option 2: CloudBeaver (http://localhost:8083)
- Advanced features
- Visual query builder
- ER diagrams
- Data export/import
- Multiple database support

#### Option 3: Azure Data Studio
```bash
# Download from: https://aka.ms/azuredatastudio
# Connect to: localhost,1433
# Database: customerdb
# Authentication: SQL Server
# Username: sa
# Password: YourStrong@Passw0rd
```

#### Option 4: SQL Server Management Studio (SSMS)
```bash
# Download from: https://aka.ms/ssmsfullsetup
# Server name: localhost,1433
# Authentication: SQL Server Authentication
# Login: sa
# Password: YourStrong@Passw0rd
```

## ?? Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "customerdb": ""
  },
  "KeyVault": {
    "VaultUri": ""
  }
}
```

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "customerdb": "Server=localhost,1433;Database=customerdb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  },
  "KeyVault": {
    "UseLocalSecrets": true
  }
}
```

### User Secrets
```bash
# SQL Server password for Aspire
dotnet user-secrets set "Parameters:sql-password" "YourStrong@Passw0rd"

# Connection string (alternative)
dotnet user-secrets set "ConnectionStrings:customerdb" "Server=localhost,1433;Database=customerdb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
```

## ?? Docker Commands

```bash
# Start services
docker-compose -f docker-compose.sqlserver.yml up -d

# Stop services
docker-compose -f docker-compose.sqlserver.yml down

# View logs
docker-compose -f docker-compose.sqlserver.yml logs -f

# Restart SQL Server
docker restart sqlserver-customer

# SQL Server status
docker ps | grep sqlserver

# Connect to SQL Server container
docker exec -it sqlserver-customer /bin/bash

# Run SQL query
docker exec -it sqlserver-customer /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "SELECT name FROM sys.databases"
```

## ?? Kubernetes Deployment

### SQL Server
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sqlserver
spec:
  replicas: 1
  template:
    spec:
      containers:
      - name: sqlserver
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sqlserver-secret
              key: sa-password
        ports:
        - containerPort: 1433
        volumeMounts:
        - name: sqlserver-data
          mountPath: /var/opt/mssql
```

### CustomerService
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: customerservice
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: customerservice
        image: customerservice:latest
        env:
        - name: ConnectionStrings__customerdb
          valueFrom:
            secretKeyRef:
              name: customerservice-secret
              key: connection-string
        ports:
        - containerPort: 5003
        livenessProbe:
          httpGet:
            path: /live
            port: 5003
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 5003
          initialDelaySeconds: 10
          periodSeconds: 5
```

## ?? Security

### Password Management
```bash
# Generate strong password
openssl rand -base64 32

# Store in Azure Key Vault
az keyvault secret set \
  --vault-name your-keyvault \
  --name SqlServerPassword \
  --value "YourStrong@Passw0rd"
```

### Connection String Encryption
```bash
# Encrypt connection string
dotnet user-secrets set "ConnectionStrings:customerdb" "encrypted-value"
```

## ?? Testing

### Integration Tests with TestContainers
```csharp
[Fact]
public async Task Database_ShouldConnect()
{
    var container = new MsSqlBuilder()
        .WithPassword("YourStrong@Passw0rd")
        .Build();
        
    await container.StartAsync();
    
    // Your tests here
}
```

## ?? Monitoring

### SQL Server Performance
```sql
-- Active connections
SELECT * FROM sys.dm_exec_connections

-- Database size
SELECT 
    DB_NAME(database_id) AS DatabaseName,
    SUM(size * 8 / 1024) AS SizeMB
FROM sys.master_files
GROUP BY database_id

-- Query performance
SELECT TOP 10 *
FROM sys.dm_exec_query_stats
ORDER BY total_elapsed_time DESC
```

### Aspire Dashboard Metrics
- Request count
- Response time
- Error rate
- Database query duration
- Memory usage

## ?? Troubleshooting

### SQL Server Connection Issues
```bash
# Test connection
docker exec -it sqlserver-customer /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "SELECT @@VERSION"

# Check logs
docker logs sqlserver-customer

# Restart container
docker restart sqlserver-customer
```

### Entity Framework Issues
```bash
# Clear migrations
dotnet ef database drop
dotnet ef migrations remove

# Regenerate
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Port Conflicts
```bash
# Check port 1433
netstat -ano | findstr :1433

# Kill process
taskkill /PID [PID] /F
```

## ?? Resources

- [SQL Server Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Aspire SQL Server Component](https://learn.microsoft.com/en-us/dotnet/aspire/database/sql-server-component)
- [Adminer Documentation](https://www.adminer.org/en/)
- [CloudBeaver Documentation](https://cloudbeaver.io/)

---

**Status**: ? **PRODUCTION READY**  
**Database**: SQL Server 2022  
**ORM**: Entity Framework Core 9.0  
**UI Tools**: Adminer, CloudBeaver
