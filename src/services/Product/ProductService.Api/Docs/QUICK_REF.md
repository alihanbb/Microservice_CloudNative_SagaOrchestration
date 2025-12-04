# ?? ProductService Quick Reference

## ?? URLs

| Service | URL |
|---------|-----|
| **Aspire Dashboard** | http://localhost:15888 |
| **Product API** | http://localhost:5005 |
| **Swagger** | http://localhost:5005/swagger |
| **Health Check** | http://localhost:5005/health |
| **Cosmos DB UI** | http://localhost:8081/_explorer |

## ??? Cosmos DB Connection

```
Endpoint: https://localhost:8081/
Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
Database: productdb
Container: products
Partition Key: /categoryId
```

## ? Quick Commands

### Start Services
```bash
# Aspire (Recommended)
cd aspire/Microservice.AppHost
dotnet run

# Standalone Cosmos DB
docker run -d -p 8081:8081 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

### Health Checks
```bash
curl http://localhost:5005/health
curl http://localhost:5005/ready
curl http://localhost:5005/live
```

### Cosmos DB Queries
```sql
-- All products
SELECT * FROM products

-- By category
SELECT * FROM products p WHERE p.categoryId = 'electronics'

-- Price range
SELECT * FROM p WHERE p.price BETWEEN 100 AND 500
```

## ?? Key Packages

- Aspire.Microsoft.Azure.Cosmos (9.5.0)
- Aspire.Azure.Security.KeyVault (9.5.0)
- Swashbuckle.AspNetCore (7.2.0)

## ?? Health Check Status

| Check | Description | Tag |
|-------|-------------|-----|
| self | API running | api, ready |
| memory | Memory < 1GB | api, memory |
| cosmosdb | DB configured | database, ready |

## ?? Docker Commands

```bash
# Start
docker run -d -p 8081:8081 --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator

# Logs
docker logs cosmos-emulator -f

# Restart
docker restart cosmos-emulator

# Stop
docker stop cosmos-emulator
```

## ?? Troubleshooting

| Issue | Solution |
|-------|----------|
| Can't connect to Cosmos | `docker restart cosmos-emulator` |
| Port 8081 in use | Check `netstat -ano \| findstr :8081` |
| SSL error | Use `TrustServerCertificate=true` |

## ?? Database Info

**Database**: productdb  
**Container**: products  
**Partition Key**: /categoryId  
**Throughput**: 400 RU/s  
**Consistency**: Session  

---

**Last Updated**: 2024
