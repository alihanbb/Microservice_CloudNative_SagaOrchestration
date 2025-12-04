# ?? CustomerService Quick Reference

## ?? URLs

| Service | URL |
|---------|-----|
| **Aspire Dashboard** | http://localhost:15888 |
| **Customer API** | http://localhost:5003 |
| **Swagger** | http://localhost:5003/swagger |
| **Health Check** | http://localhost:5003/health |
| **Adminer (SQL UI)** | http://localhost:8082 |
| **CloudBeaver** | http://localhost:8083 |

## ??? SQL Server Connection

```
Server: localhost,1433
Database: customerdb
Username: sa
Password: YourStrong@Passw0rd
```

## ? Quick Commands

### Start Services
```bash
# Aspire (Recommended)
cd aspire/Microservice.AppHost
dotnet user-secrets set "Parameters:sql-password" "YourStrong@Passw0rd"
dotnet run

# Docker Compose
docker-compose -f docker-compose.sqlserver.yml up -d
```

### Health Checks
```bash
curl http://localhost:5003/health
curl http://localhost:5003/ready
curl http://localhost:5003/live
```

### EF Core Migrations
```bash
cd src/services/Customer/CustomerServices.Api

dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet ef migrations list
```

### SQL Queries
```bash
# Via Docker
docker exec -it sqlserver-customer /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" -C \
  -Q "SELECT name FROM sys.databases"
```

## ?? Key Packages

- Aspire.Microsoft.EntityFrameworkCore.SqlServer (9.5.0)
- Aspire.Azure.Security.KeyVault (9.5.0)
- Microsoft.EntityFrameworkCore.Design (9.0.11)

## ?? Health Check Status

| Check | Description | Tag |
|-------|-------------|-----|
| self | API running | api, ready |
| memory | Memory < 1GB | api, memory |
| sqlserver | DB connectivity | database, ready |

## ?? Docker Commands

```bash
# Start
docker-compose -f docker-compose.sqlserver.yml up -d

# Stop
docker-compose -f docker-compose.sqlserver.yml down

# Logs
docker logs sqlserver-customer -f

# Restart
docker restart sqlserver-customer
```

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| Can't connect to SQL | `docker restart sqlserver-customer` |
| Port 1433 in use | Check `netstat -ano \| findstr :1433` |
| Migration error | `dotnet ef database drop && dotnet ef database update` |
| Adminer won't load | Clear browser cache, restart container |

---

**Last Updated**: 2024
