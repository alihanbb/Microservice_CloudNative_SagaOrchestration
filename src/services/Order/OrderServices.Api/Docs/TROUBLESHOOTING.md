# ?? Troubleshooting Guide - OrderService

## ?? Yaygýn Hatalar ve Çözümleri

### 1. ? CosmosClient Configuration Error

**Hata:**
```
System.InvalidOperationException: A CosmosClient could not be configured. 
Ensure valid connection information was provided in 'ConnectionStrings:cosmos'
```

**Sebep:** `appsettings.json` dosyasýnda `ConnectionStrings:cosmos` eksik.

**Çözüm:**

#### Option 1: appsettings.json (Önerilen - Development)
```json
{
  "ConnectionStrings": {
    "cosmos": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

#### Option 2: User Secrets (Güvenli)
```bash
dotnet user-secrets set "ConnectionStrings:cosmos" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
```

#### Option 3: Environment Variables
```bash
# Windows (PowerShell)
$env:ConnectionStrings__cosmos = "AccountEndpoint=https://localhost:8081/;AccountKey=..."

# Linux/Mac
export ConnectionStrings__cosmos="AccountEndpoint=https://localhost:8081/;AccountKey=..."
```

---

### 2. ? Cosmos DB Emulator SSL Certificate Error

**Hata:**
```
The SSL connection could not be established
```

**Sebep:** Cosmos DB Emulator self-signed certificate kullanýr.

**Çözüm:**

#### Program.cs'de SSL Bypass (Development)
```csharp
builder.AddAzureCosmosClient("cosmos",
    configureClientOptions: clientOptions =>
    {
        if (builder.Environment.IsDevelopment())
        {
            clientOptions.HttpClientFactory = () =>
            {
                HttpMessageHandler httpMessageHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(httpMessageHandler);
            };
        }
    });
```

---

### 3. ? Cosmos DB Emulator Not Running

**Hata:**
```
No connection could be made because the target machine actively refused it (localhost:8081)
```

**Sebep:** Cosmos DB Emulator çalýþmýyor.

**Çözüm:**

#### Docker ile Baþlat
```bash
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

#### Docker Compose ile
```bash
cd aspire/Microservice.AppHost
docker-compose -f docker-compose.cosmos.yml up -d
```

#### Kontrol Et
```bash
# Docker container durumu
docker ps | grep cosmos

# Cosmos DB Explorer eriþimi
curl http://localhost:8081/_explorer/index.html
```

---

### 4. ? Port Already in Use

**Hata:**
```
Failed to bind to address http://localhost:5001: address already in use
```

**Sebep:** Port baþka bir uygulama tarafýndan kullanýlýyor.

**Çözüm:**

#### Windows (PowerShell)
```powershell
# Port'u kullanan process'i bul
netstat -ano | findstr :5001

# Process'i sonlandýr
taskkill /PID [PID_NUMBER] /F
```

#### Linux/Mac
```bash
# Port'u kullanan process'i bul
lsof -i :5001

# Process'i sonlandýr
kill -9 [PID]
```

#### Port Deðiþtir (launchSettings.json)
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5003"  // Farklý port
    }
  }
}
```

---

### 5. ? Database/Container Not Found

**Hata:**
```
Resource Not Found. Learn more: https://aka.ms/cosmosdb-tsg-not-found
```

**Sebep:** Database veya container oluþturulmamýþ.

**Çözüm:**

#### Auto-Create ile (CosmosDbInitializer)
Program.cs'de `CosmosDbInitializer` zaten kayýtlý:
```csharp
builder.Services.AddHostedService<CosmosDbInitializer>();
```

Bu servis otomatik olarak:
- ? Database oluþturur (`orderdb`)
- ? Container oluþturur (`orders`)
- ? Partition key yapýlandýrýr (`/customerId`)

#### Manuel Oluþturma (Azure Portal veya CLI)
```bash
# Azure CLI
az cosmosdb sql database create \
  --account-name cosmos-saga-orders \
  --resource-group rg-saga-orchestration \
  --name orderdb

az cosmosdb sql container create \
  --account-name cosmos-saga-orders \
  --resource-group rg-saga-orchestration \
  --database-name orderdb \
  --name orders \
  --partition-key-path "/customerId"
```

---

### 6. ? Key Vault Connection Error

**Hata:**
```
Azure Key Vault could not be configured
```

**Sebep:** Key Vault connection string eksik (opsiyonel).

**Çözüm:**

#### Development'ta Geçici Çözüm
Key Vault kullanmýyorsanýz, Program.cs'den kaldýrýn:
```csharp
// Bu satýrý yoruma alýn veya silin
// builder.AddAzureKeyVaultClient("keyvault");
```

#### Veya Dummy Value Ekleyin
```json
{
  "ConnectionStrings": {
    "keyvault": "dummy-value-for-development"
  }
}
```

---

### 7. ? Health Check Fails

**Hata:**
```
Health check 'cosmosdb' failed with status Unhealthy
```

**Sebep:** Cosmos DB baðlantýsý yok.

**Çözüm:**

#### Health Check Endpoint Kontrol
```bash
curl http://localhost:5001/health | jq
```

#### Çýktý Analizi
```json
{
  "entries": {
    "cosmosdb": {
      "status": "Unhealthy",
      "description": "Connection failed"
    }
  }
}
```

**Aksiyonlar:**
1. Cosmos DB emulator çalýþýyor mu? ? `docker ps`
2. Connection string doðru mu? ? `appsettings.json`
3. SSL bypass yapýldý mý? ? `Program.cs`

---

### 8. ? Swagger UI Not Loading

**Hata:**
```
404 Not Found - /swagger
```

**Sebep:** Swagger middleware yanlýþ sýrada veya eksik.

**Çözüm:**

#### Program.cs Kontrol
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API V1");
        options.RoutePrefix = "swagger";  // Önemli!
    });
}
```

#### Eriþim
```bash
# Swagger UI
http://localhost:5001/swagger

# OpenAPI JSON
http://localhost:5001/swagger/v1/swagger.json
```

---

### 9. ? CORS Error

**Hata:**
```
Access to fetch at 'http://localhost:5001/api/...' has been blocked by CORS policy
```

**Sebep:** CORS policy eksik veya yanlýþ yapýlandýrýlmýþ.

**Çözüm:**

#### Program.cs'de CORS Ekleme
```csharp
// Service registration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Middleware
app.UseCors();  // UseRouting()'den ÖNCE!
```

---

### 10. ? No Action Descriptors Found

**Uyarý:**
```
No action descriptors found. This may indicate an incorrectly configured application
```

**Sebep:** Controller'lar bulunamýyor.

**Çözüm:**

#### Controllers Ekleme
```csharp
builder.Services.AddControllers();  // Service registration

app.MapControllers();  // Middleware
```

#### Controller Oluþturma
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok("Orders API");
}
```

---

## ?? Debug Checklist

Uygulama çalýþmýyorsa sýrayla kontrol edin:

- [ ] ? Cosmos DB Emulator çalýþýyor mu?
  ```bash
  docker ps | grep cosmos
  ```

- [ ] ? Connection string doðru mu?
  ```bash
  cat appsettings.json | grep cosmos
  ```

- [ ] ? Port boþ mu?
  ```bash
  netstat -ano | findstr :5001
  ```

- [ ] ? SSL bypass yapýldý mý? (Development)
  ```csharp
  ServerCertificateCustomValidationCallback = (_, _, _, _) => true
  ```

- [ ] ? NuGet paketleri restore edildi mi?
  ```bash
  dotnet restore
  ```

- [ ] ? Build baþarýlý mý?
  ```bash
  dotnet build
  ```

---

## ?? Diagnostic Commands

```bash
# Application logs
dotnet run --verbosity detailed

# Health check
curl http://localhost:5001/health

# Cosmos DB connectivity
curl http://localhost:8081/_explorer/index.html

# Docker logs
docker logs cosmos-emulator

# Process list
ps aux | grep OrderServices.Api
```

---

## ?? Quick Fixes

### Full Reset
```bash
# 1. Stop application
Ctrl+C

# 2. Clean build
dotnet clean
dotnet build

# 3. Restart Cosmos DB
docker restart cosmos-emulator

# 4. Run application
dotnet run
```

### Fresh Start
```bash
# 1. Remove containers
docker stop cosmos-emulator
docker rm cosmos-emulator

# 2. Recreate
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# 3. Wait 30 seconds for emulator startup
sleep 30

# 4. Run application
dotnet run
```

---

## ?? Support Resources

- [.NET Aspire Docs](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Cosmos DB Emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [ASP.NET Core Troubleshooting](https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot)

---

**Last Updated:** 2024  
**Status:** ? Active
