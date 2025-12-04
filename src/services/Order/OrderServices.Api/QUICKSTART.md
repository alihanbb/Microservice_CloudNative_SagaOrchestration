# ? HIZLI BAÞLANGIÇ - OrderService

## ?? 3 Adýmda Çalýþtýr

### 1?? Cosmos DB Emulator Baþlat

```bash
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  --name cosmos-emulator \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

?? **30 saniye bekleyin** (Emulator baþlamasý için)

### 2?? Connection String Kontrol

**Dosya:** `src/services/Order/OrderServices.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "cosmos": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  }
}
```

? **Zaten eklendi!** (Yukarýdaki deðiþiklikle)

### 3?? Uygulamayý Çalýþtýr

```bash
cd src/services/Order/OrderServices.Api
dotnet run
```

---

## ?? Baþarýlý!

### ?? Eriþim

- **API**: http://localhost:5001
- **Swagger**: http://localhost:5001/swagger
- **Health**: http://localhost:5001/health
- **Cosmos DB UI**: http://localhost:8081/_explorer

### ?? Test Et

```bash
# Health check
curl http://localhost:5001/health

# Swagger JSON
curl http://localhost:5001/swagger/v1/swagger.json
```

---

## ?? Sorun mu Var?

### ? "CosmosClient could not be configured"

**Çözüm:** `appsettings.json`'da `ConnectionStrings:cosmos` ekleyin (Zaten eklendi ?)

### ? "Connection refused (localhost:8081)"

**Çözüm:** Cosmos DB emulator çalýþmýyor
```bash
docker ps | grep cosmos
# Yoksa tekrar baþlat
docker restart cosmos-emulator
```

### ? "SSL connection could not be established"

**Çözüm:** `Program.cs`'de SSL bypass eklendi (Development ortamýnda otomatik ?)

### ?? Detaylý Troubleshooting

[TROUBLESHOOTING.md](Docs/TROUBLESHOOTING.md)

---

## ?? Ýleri Seviye

### Aspire Dashboard ile Çalýþtýr

```bash
cd aspire/Microservice.AppHost
dotnet run
```

Dashboard: http://localhost:15888

### Docker Compose ile Çalýþtýr

```bash
cd aspire/Microservice.AppHost
docker-compose -f docker-compose.cosmos.yml up
```

---

**Son Güncelleme:** 2024  
**Durum:** ? Production Ready
