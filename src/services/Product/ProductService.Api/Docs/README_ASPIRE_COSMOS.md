# ?? ProductService - Azure Cosmos DB & Aspire Configuration

## ?? Overview

ProductService API configured with:
- ? **Azure Cosmos DB (NoSQL)** - Document-based storage
- ? **Azure Key Vault** - Secrets management
- ? **Aspire Orchestration** - Container orchestration
- ? **Cosmos DB UI** - Database explorer
- ? **Health Checks** - Monitoring endpoints
- ? **Swagger/OpenAPI** - API documentation

## ??? Architecture

```
???????????????????????????????????????????????????
?              Aspire AppHost                     ?
?  ????????????????????????????????????????????  ?
?  ?                                           ?  ?
?  ?   ????????????????    ????????????????  ?  ?
?  ?   ?   Product    ??????  Cosmos DB   ?  ?  ?
?  ?   ?   Service    ?    ?  (productdb) ?  ?  ?
?  ?   ????????????????    ????????????????  ?  ?
?  ?          ?                    ?          ?  ?
?  ?          ?             ????????????????  ?  ?
?  ?          ???????????????  Key Vault   ?  ?  ?
?  ?                        ?   (Azure)    ?  ?  ?
?  ?                        ????????????????  ?  ?
?  ?                                           ?  ?
?  ?   ????????????????????????????????????   ?  ?
?  ?   ?       Cosmos DB UI               ?   ?  ?
?  ?   ?  (Shared with OrderService)      ?   ?  ?
?  ?   ????????????????????????????????????   ?  ?
?  ????????????????????????????????????????????  ?
???????????????????????????????????????????????????
```

## ??? Database Design

### Cosmos DB Configuration
- **Database**: `productdb`
- **Container**: `products`
- **Partition Key**: `/categoryId`
- **Throughput**: 400 RU/s (adjustable)
- **Consistency**: Session level

### Why Partition Key: /categoryId?
? Products are often queried by category  
? Even data distribution across categories  
? Efficient range queries within categories  
? Scalable horizontal partitioning  

### Sample Document Structure
```json
{
  "id": "product-001",
  "categoryId": "electronics",
  "name": "Laptop Pro 15",
  "description": "High-performance laptop",
  "price": 1299.99,
  "stock": 50,
  "brand": "TechBrand",
  "specifications": {
    "cpu": "Intel i7",
    "ram": "16GB",
    "storage": "512GB SSD"
  },
  "images": [
    "https://example.com/laptop1.jpg",
    "https://example.com/laptop2.jpg"
  ],
  "tags": ["laptop", "electronics", "computers"],
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z",
  "_etag": "..."
}
```

## ?? Quick Start

### Option 1: Using Aspire AppHost (Recommended)

```bash
# Navigate to AppHost
cd aspire/Microservice.AppHost

# Set Cosmos DB connection (one-time)
dotnet user-secrets set "ConnectionStrings:cosmos" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

# Run Aspire (starts everything)
dotnet run

# Aspire will automatically:
# - Start Cosmos DB emulator (if not running)
# - Configure ProductService
# - Apply database/container creation
# - Setup health checks
```

### Option 2: Standalone

```bash
# 1. Start Cosmos DB Emulator
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# 2. Run ProductService
cd src/services/Product/ProductService.Api
dotnet run
```

## ?? Access URLs

| Service | URL | Description |
|---------|-----|-------------|
| **Aspire Dashboard** | http://localhost:15888 | Monitoring Dashboard |
| **ProductService API** | http://localhost:5005 | REST API (HTTP) |
| **ProductService HTTPS** | https://localhost:5006 | REST API (HTTPS) |
| **Swagger UI** | http://localhost:5005/swagger | API Documentation |
| **Cosmos DB Explorer** | http://localhost:8081/_explorer | Database UI |
| **Health Check** | http://localhost:5005/health | Health Endpoint |

## ??? Cosmos DB Connection

### Local Development (Emulator)
```
Endpoint: https://localhost:8081/
Primary Key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
Database: productdb
Container: products
Partition Key: /categoryId
```

### Cosmos DB Explorer Access
1. Navigate to http://localhost:8081/_explorer
2. Click on "productdb" database
3. Click on "products" container
4. Browse, query, and manage documents

## ?? NuGet Packages

```xml
<PackageReference Include="Aspire.Microsoft.Azure.Cosmos" Version="9.5.0" />
<PackageReference Include="Aspire.Azure.Security.KeyVault" Version="9.5.0" />
<PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="9.5.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
<PackageReference Include="AspNetCore.HealthChecks.CosmosDb" Version="9.0.0" />
```

## ?? Health Checks

### Endpoints
- `/health` - Full health report (self, memory, Cosmos DB)
- `/ready` - Kubernetes readiness probe
- `/live` - Kubernetes liveness probe

### Test Commands
```bash
# All health checks
curl http://localhost:5005/health

# Readiness
curl http://localhost:5005/ready

# Liveness
curl http://localhost:5005/live

# Pretty JSON
curl http://localhost:5005/health | jq
```

### Health Check Components
1. **Self Check**: API availability
2. **Memory Check**: Memory usage (threshold: 1GB)
3. **Cosmos DB Check**: Database configuration validation

## ?? Configuration

### appsettings.json (Production)
```json
{
  "CosmosDb": {
    "DatabaseName": "productdb",
    "ContainerName": "products",
    "PartitionKey": "/categoryId"
  },
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  }
}
```

### appsettings.Development.json
```json
{
  "CosmosDb": {
    "DatabaseName": "productdb",
    "ContainerName": "products",
    "PartitionKey": "/categoryId",
    "UseEmulator": true
  },
  "KeyVault": {
    "UseLocalSecrets": true
  }
}
```

## ?? Cosmos DB Queries

### Query by Category
```sql
SELECT * FROM products p 
WHERE p.categoryId = 'electronics'
```

### Query by Price Range
```sql
SELECT * FROM products p 
WHERE p.categoryId = 'electronics' 
AND p.price BETWEEN 500 AND 1500
```

### Full-Text Search
```sql
SELECT * FROM products p 
WHERE CONTAINS(p.name, 'laptop') 
OR CONTAINS(p.description, 'laptop')
```

### Aggregations
```sql
SELECT p.categoryId, 
       COUNT(1) as productCount,
       AVG(p.price) as avgPrice,
       SUM(p.stock) as totalStock
FROM products p
GROUP BY p.categoryId
```

## ?? Docker Commands

```bash
# Start Cosmos DB Emulator
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# Check status
docker ps | grep cosmos

# View logs
docker logs cosmos-emulator -f

# Restart
docker restart cosmos-emulator

# Stop
docker stop cosmos-emulator

# Remove
docker rm cosmos-emulator
```

## ?? Kubernetes Deployment

### ProductService Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: productservice
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: productservice
        image: productservice:latest
        env:
        - name: ConnectionStrings__cosmos
          valueFrom:
            secretKeyRef:
              name: cosmos-secret
              key: connection-string
        - name: CosmosDb__DatabaseName
          value: "productdb"
        - name: CosmosDb__ContainerName
          value: "products"
        ports:
        - containerPort: 5005
        livenessProbe:
          httpGet:
            path: /live
            port: 5005
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 5005
          initialDelaySeconds: 10
          periodSeconds: 5
```

## ?? Azure Production Setup

### 1. Create Cosmos DB Account
```bash
# Create Cosmos DB account
az cosmosdb create \
  --name cosmos-saga-products \
  --resource-group rg-saga-orchestration \
  --locations regionName=eastus \
  --default-consistency-level Session

# Create database
az cosmosdb sql database create \
  --account-name cosmos-saga-products \
  --resource-group rg-saga-orchestration \
  --name productdb

# Create container with partition key
az cosmosdb sql container create \
  --account-name cosmos-saga-products \
  --resource-group rg-saga-orchestration \
  --database-name productdb \
  --name products \
  --partition-key-path "/categoryId" \
  --throughput 400
```

### 2. Get Connection String
```bash
az cosmosdb keys list \
  --name cosmos-saga-products \
  --resource-group rg-saga-orchestration \
  --type connection-strings
```

### 3. Store in Key Vault
```bash
# Create Key Vault
az keyvault create \
  --name kv-saga-products \
  --resource-group rg-saga-orchestration \
  --location eastus

# Store Cosmos DB connection string
az keyvault secret set \
  --vault-name kv-saga-products \
  --name CosmosDbConnectionString \
  --value "[CONNECTION_STRING]"
```

## ?? Monitoring & Performance

### Request Units (RU/s)
- **Read**: ~1 RU per 1KB document
- **Write**: ~5 RU per 1KB document
- **Query**: Varies based on complexity

### Optimize Performance
1. **Use partition key in queries** - Avoid cross-partition queries
2. **Index only necessary fields** - Reduce indexing overhead
3. **Use point reads** - Most efficient (id + partition key)
4. **Batch operations** - Use bulk operations for multiple items
5. **Monitor RU consumption** - Adjust throughput as needed

### Cosmos DB Metrics
```csharp
// Track RU consumption
var response = await container.ReadItemAsync<Product>(id, new PartitionKey(categoryId));
var requestCharge = response.RequestCharge;
_logger.LogInformation($"Read operation consumed {requestCharge} RUs");
```

## ?? Testing

### Sample Product Creation
```bash
curl -X POST http://localhost:5005/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "id": "product-001",
    "categoryId": "electronics",
    "name": "Laptop Pro 15",
    "price": 1299.99,
    "stock": 50
  }'
```

### Query Products
```bash
# Get all products
curl http://localhost:5005/api/products

# Get product by ID
curl http://localhost:5005/api/products/product-001

# Get products by category
curl http://localhost:5005/api/products/category/electronics
```

## ?? Cosmos DB Features

### Advantages for Product Catalog
? **Schema flexibility** - Easy to add new product attributes  
? **Horizontal scaling** - Handle millions of products  
? **Global distribution** - Multi-region replication  
? **Low latency** - Single-digit millisecond reads  
? **Rich queries** - SQL-like query language  
? **JSON native** - No ORM mapping overhead  

### Best Practices
1. Choose partition key wisely (categoryId for products)
2. Keep documents < 2MB
3. Use bulk operations for batch inserts
4. Implement optimistic concurrency with _etag
5. Monitor and optimize RU consumption

## ?? Troubleshooting

### Cosmos DB Connection Issues
```bash
# Test emulator connection
curl https://localhost:8081/_explorer/index.html -k

# Check container status
docker logs cosmos-emulator | grep "Started"
```

### Common Errors
| Error | Solution |
|-------|----------|
| Request rate too large | Increase RU/s or implement retry policy |
| Partition key not found | Ensure every document has categoryId |
| SSL certificate error | Use TrustServerCertificate=true for emulator |

## ?? Resources

- [Cosmos DB Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/)
- [Aspire Cosmos DB Component](https://learn.microsoft.com/en-us/dotnet/aspire/database/azure-cosmos-db-component)
- [Cosmos DB Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/best-practice)
- [Partition Key Strategies](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview)

---

**Status**: ? **PRODUCTION READY**  
**Database**: Azure Cosmos DB (NoSQL)  
**Partition Strategy**: By Category  
**API Port**: 5005/5006  
**Cosmos DB UI**: Port 8081
