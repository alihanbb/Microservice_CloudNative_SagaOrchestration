# 🗄️ Backup Service - Kapsamlı Dokümantasyon

Bu dokümantasyon, **Customer** ve **Order** servislerinin verilerini ayrı backup veritabanlarına **artırımlı (incremental)** olarak senkronize eden Backup Service'in detaylı açıklamasını içerir.

---

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Mimari](#mimari)
3. [Docker Compose Altyapısı](#docker-compose-altyapısı)
4. [Proje Yapısı](#proje-yapısı)
5. [Konfigürasyon](#konfigürasyon)
6. [Artırımlı Sync Algoritması](#artırımlı-sync-algoritması)
7. [Azure Functions](#azure-functions)
8. [API Endpoints](#api-endpoints)
9. [Zamanlanmış Yedekleme](#zamanlanmış-yedekleme)
10. [Kurulum ve Çalıştırma](#kurulum-ve-çalıştırma)
11. [Test Senaryoları](#test-senaryoları)

---

## Genel Bakış

### Amaç

Bu servis, mikroservis mimarisindeki verileri **veritabanı seviyesinde** yedeklemek için tasarlanmıştır. Geleneksel dosya tabanlı yedekleme yerine, **ayrı veritabanı container'larına** artırımlı senkronizasyon yapılır.

### Avantajları

| Avantaj | Açıklama |
|---------|----------|
| **Hızlı Restore** | Veriler zaten veritabanı formatında, direkt kullanılabilir |
| **Sorgulanabilir Backup** | Backup veritabanına sorgu atabilirsiniz |
| **Disaster Recovery** | Ana DB çökerse backup DB hemen devreye girebilir |
| **Artırımlı Sync** | Sadece değişen veriler senkronize edilir (bandwidth tasarrufu) |
| **Version Tracking** | Her kayıt için versiyon kontrolü yapılır |

### Desteklenen Veritabanları

| Servis | Kaynak | Backup | Teknoloji |
|--------|--------|--------|-----------|
| Customer | Port 1473 | Port 1474 | SQL Server 2022 |
| Order | Port 8081 | Port 8082 | Azure CosmosDB Emulator |

---

## Mimari

### Sistem Mimarisi

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            BackupServices                                    │
│                         (Azure Functions v4)                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌────────────────────┐         ┌────────────────────┐                     │
│   │   BackupFunction   │         │  ScheduledSync     │                     │
│   │   (HTTP Trigger)   │         │  (Timer: 00:00)    │                     │
│   │                    │         │                    │                     │
│   │ • /backup/sync     │         │ CRON: 0 0 0 * * *  │                     │
│   │ • /backup/sync/    │         │ (Her gece yarısı)  │                     │
│   │   customer         │         │                    │                     │
│   │ • /backup/sync/    │         │                    │                     │
│   │   order            │         │                    │                     │
│   │ • /backup/         │         │                    │                     │
│   │   initialize       │         │                    │                     │
│   └─────────┬──────────┘         └──────────┬─────────┘                     │
│             │                               │                                │
│             └───────────────┬───────────────┘                                │
│                             ▼                                                │
│             ┌───────────────────────────────┐                                │
│             │        ISyncService           │                                │
│             ├───────────────────────────────┤                                │
│             │  CustomerSyncService          │                                │
│             │  (SQL Server → SQL Server)    │                                │
│             ├───────────────────────────────┤                                │
│             │  OrderSyncService             │                                │
│             │  (CosmosDB → CosmosDB)        │                                │
│             └───────────────────────────────┘                                │
│                                                                              │
└──────────────────────────────────┬───────────────────────────────────────────┘
                                   │
          ┌────────────────────────┼────────────────────────┐
          │                        │                        │
          ▼                        ▼                        ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│   Azurite        │    │   SQL Server     │    │   CosmosDB       │
│   (Storage)      │    │   Containers     │    │   Containers     │
│                  │    │                  │    │                  │
│ Port: 10000      │    │ Source: 1473     │    │ Source: 8081     │
│ (Blob)           │    │ Backup: 1474     │    │ Backup: 8082     │
│                  │    │                  │    │                  │
│ Timer Trigger    │    │                  │    │                  │
│ için gerekli     │    │                  │    │                  │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

### Veri Akışı

```
                    KAYNAK VERİTABANLARI
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
┌──────────────────┐              ┌──────────────────┐
│   CustomerDb     │              │    orderdb       │
│   (SQL Server)   │              │   (CosmosDB)     │
│   Port: 1473     │              │   Port: 8081     │
└────────┬─────────┘              └────────┬─────────┘
         │                                 │
         │ SELECT * FROM                   │ SELECT * FROM c
         │ customer.Customers              │ WHERE c.type = 'Order'
         │                                 │
         ▼                                 ▼
┌──────────────────────────────────────────────────────┐
│                    SYNC SERVİSLERİ                   │
├──────────────────────────────────────────────────────┤
│                                                      │
│  1. Kaynak veritabanından tüm kayıtları oku          │
│  2. Backup veritabanından mevcut kayıtları oku       │
│  3. Karşılaştır:                                     │
│     • Yeni kayıt → INSERT                            │
│     • Değişen kayıt (version/lastModified) → UPDATE  │
│     • Silinen kayıt → DELETE                         │
│     • Aynı kayıt → SKIP                              │
│  4. Sync history'ye kaydet                           │
│                                                      │
└──────────────────────────────────────────────────────┘
         │                                 │
         ▼                                 ▼
┌──────────────────┐              ┌──────────────────┐
│ CustomerDb_Backup│              │  orderdb-backup  │
│   (SQL Server)   │              │   (CosmosDB)     │
│   Port: 1474     │              │   Port: 8082     │
└──────────────────┘              └──────────────────┘
                    BACKUP VERİTABANLARI
```

---

## Docker Compose Altyapısı

### Container'lar

```yaml
# docker-compose.yml içinde tanımlanan servisler:

services:
  # ═══════════════════════════════════════
  # ANA VERİTABANLARI
  # ═══════════════════════════════════════
  
  sqlserver:           # Customer veritabanı
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports: 1473:1433
    
  cosmosdb:            # Order veritabanı  
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
    ports: 8081:8081
  
  # ═══════════════════════════════════════
  # BACKUP VERİTABANLARI
  # ═══════════════════════════════════════
  
  sqlserver-backup:    # Customer backup veritabanı
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports: 1474:1433
    
  cosmosdb-backup:     # Order backup veritabanı
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
    ports: 8082:8081
    
  # ═══════════════════════════════════════
  # DESTEK SERVİSLERİ
  # ═══════════════════════════════════════
  
  azurite:             # Azure Storage Emulator
    image: mcr.microsoft.com/azure-storage/azurite
    ports: 
      - 10000:10000    # Blob
      - 10001:10001    # Queue  
      - 10002:10002    # Table
```

### Container Detayları

| Container | Image | Port | Volume | Açıklama |
|-----------|-------|------|--------|----------|
| sqlserver | mssql/server:2022 | 1473 | sqlserver_data | Ana Customer DB |
| sqlserver-backup | mssql/server:2022 | 1474 | sqlserver_backup_data | Backup Customer DB |
| cosmosdb-emulator | azure-cosmos-emulator | 8081 | cosmosdb_data | Ana Order DB |
| cosmosdb-backup | azure-cosmos-emulator | 8082 | cosmosdb_backup_data | Backup Order DB |
| azurite | azurite | 10000-10002 | azurite_data | Timer Trigger için Azure Storage |

---

## Proje Yapısı

```
src/services/Backup/BackupServices/
├── Configuration/
│   └── BackupConfiguration.cs      # Tüm konfigürasyon sınıfları
│       ├── SourceCustomerDbConfiguration
│       ├── BackupCustomerDbConfiguration
│       ├── SourceOrderDbConfiguration
│       ├── BackupOrderDbConfiguration
│       └── BackupScheduleConfiguration
│
├── Functions/
│   ├── BackupFunction.cs           # HTTP API endpoints
│   │   ├── TriggerSync             # POST /api/backup/sync
│   │   ├── SyncCustomer            # POST /api/backup/sync/customer
│   │   ├── SyncOrder               # POST /api/backup/sync/order
│   │   └── InitializeBackup        # POST /api/backup/initialize
│   │
│   ├── ScheduledBackupFunction.cs  # Timer Trigger
│   │   └── ScheduledSync           # Her gece 00:00
│   │
│   └── HealthCheckFunction.cs      # GET /api/health
│
├── Models/
│   └── BackupModels.cs             # DTO'lar
│       ├── SyncResult              # Sync sonucu
│       ├── SyncStatus              # Enum: Pending, InProgress, Completed, Failed
│       ├── SyncRequest             # Sync isteği
│       ├── CustomerSyncEntity      # Customer veri modeli
│       └── OrderSyncEntity         # Order veri modeli
│
├── Services/
│   ├── IBackupService.cs           # ISyncService interface
│   ├── CustomerBackupService.cs    # SQL → SQL sync
│   └── OrderBackupService.cs       # CosmosDB → CosmosDB sync
│
├── Program.cs                      # DI konfigürasyonu
├── appsettings.json               # Uygulama ayarları
├── host.json                       # Azure Functions host ayarları
└── local.settings.json            # Yerel geliştirme ayarları
```

---

## Konfigürasyon

### appsettings.json

```json
{
  "SourceCustomerDb": {
    "ConnectionString": "Server=localhost,1473;Database=CustomerDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "BackupCustomerDb": {
    "ConnectionString": "Server=localhost,1474;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "SourceOrderDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "orderdb",
    "ContainerName": "orders"
  },
  "BackupOrderDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8082/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "orderdb-backup",
    "ContainerName": "orders"
  },
  "BackupSchedule": {
    "CronExpression": "0 0 0 * * *",
    "Enabled": true
  }
}
```

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

---

## Artırımlı Sync Algoritması

### Customer Sync (SQL Server)

```csharp
public async Task<SyncResult> SyncAsync(bool forceFullSync = false)
{
    // 1. Kaynak veritabanından tüm müşterileri oku
    var sourceCustomers = await GetSourceCustomersAsync();
    
    // 2. Backup veritabanından mevcut müşterileri oku
    var backupCustomers = await GetBackupCustomersAsync();
    var backupDict = backupCustomers.ToDictionary(c => c.Id);
    
    foreach (var customer in sourceCustomers)
    {
        if (backupDict.TryGetValue(customer.Id, out var existing))
        {
            // Kayıt mevcut - version karşılaştır
            if (customer.Version > existing.Version || forceFullSync)
            {
                // Version daha yüksek → UPDATE
                await UpdateCustomerAsync(customer);
                result.UpdatedCount++;
            }
            else
            {
                // Değişmemiş → SKIP
                result.SkippedCount++;
            }
            backupDict.Remove(customer.Id); // İşlendi olarak işaretle
        }
        else
        {
            // Yeni kayıt → INSERT
            await InsertCustomerAsync(customer);
            result.InsertedCount++;
        }
    }
    
    // 3. Kalan kayıtlar source'tan silinmiş
    foreach (var deleted in backupDict.Values)
    {
        await DeleteCustomerAsync(deleted.Id);
        result.DeletedCount++;
    }
    
    // 4. Sync history kaydet
    await RecordSyncHistoryAsync(result);
    
    return result;
}
```

### Order Sync (CosmosDB)

```csharp
public async Task<SyncResult> SyncAsync(bool forceFullSync = false)
{
    // 1. Kaynak container'dan tüm siparişleri oku
    var sourceOrders = await GetSourceOrdersAsync();
    
    // 2. Backup container'dan mevcut siparişleri oku
    var backupOrders = await GetBackupOrdersAsync();
    var backupDict = backupOrders.ToDictionary(o => o.Id);
    
    foreach (var order in sourceOrders)
    {
        if (backupDict.TryGetValue(order.Id, out var existing))
        {
            // Kayıt mevcut - LastModified karşılaştır
            if (order.LastModified > existing.LastModified || 
                order.Status != existing.Status ||
                forceFullSync)
            {
                // Değişmiş → UPSERT
                await UpsertOrderAsync(order);
                result.UpdatedCount++;
            }
            else
            {
                // Değişmemiş → SKIP
                result.SkippedCount++;
            }
            backupDict.Remove(order.Id);
        }
        else
        {
            // Yeni kayıt → UPSERT
            await UpsertOrderAsync(order);
            result.InsertedCount++;
        }
    }
    
    // 3. Kalan kayıtlar source'tan silinmiş
    foreach (var deleted in backupDict.Values)
    {
        await DeleteOrderAsync(deleted);
        result.DeletedCount++;
    }
    
    // 4. Sync history kaydet
    await RecordSyncHistoryAsync(result);
    
    return result;
}
```

### Karşılaştırma Kriterleri

| Veritabanı | Değişiklik Kriteri | Açıklama |
|------------|-------------------|----------|
| SQL Server (Customer) | `Version` alanı | Her güncellemede version artar |
| CosmosDB (Order) | `LastModified` alanı | Son değişiklik tarihi |

---

## Azure Functions

### HTTP Trigger Functions

| Function | Method | Route | Açıklama |
|----------|--------|-------|----------|
| TriggerSync | POST | /api/backup/sync | Tüm servisleri senkronize et |
| SyncCustomer | POST | /api/backup/sync/customer | Sadece Customer senkronize et |
| SyncOrder | POST | /api/backup/sync/order | Sadece Order senkronize et |
| InitializeBackup | POST | /api/backup/initialize | Backup veritabanlarını oluştur |
| HealthCheck | GET | /api/health | Sağlık kontrolü |

### Timer Trigger Function

```csharp
[Function("ScheduledSync")]
public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timerInfo)
{
    // Her gece 00:00:00 UTC'de çalışır
    foreach (var service in _syncServices)
    {
        await service.SyncAsync(forceFullSync: false);
    }
}
```

**CRON İfadesi:** `0 0 0 * * *`
- Saniye: 0
- Dakika: 0
- Saat: 0 (gece yarısı)
- Gün: * (her gün)
- Ay: * (her ay)
- Hafta günü: * (her gün)

---

## API Endpoints

### 1. Tüm Servisleri Senkronize Et

```http
POST /api/backup/sync
Content-Type: application/json

{
  "service": "all",        // "all", "customer", veya "order"
  "forceFullSync": false   // true ise tüm kayıtları günceller
}
```

**Yanıt:**
```json
{
  "Success": true,
  "Message": "All syncs completed successfully",
  "Results": [
    {
      "SyncId": "abc123...",
      "ServiceName": "customer",
      "Status": "Completed",
      "InsertedCount": 5,
      "UpdatedCount": 10,
      "DeletedCount": 2,
      "SkippedCount": 83,
      "TotalProcessed": 17,
      "StartedAt": "2024-12-07T00:00:00Z",
      "CompletedAt": "2024-12-07T00:00:05Z"
    },
    {
      "SyncId": "def456...",
      "ServiceName": "order",
      "Status": "Completed",
      "InsertedCount": 3,
      "UpdatedCount": 7,
      "DeletedCount": 0,
      "SkippedCount": 45,
      "TotalProcessed": 10
    }
  ],
  "Timestamp": "2024-12-07T00:00:05Z"
}
```

### 2. Sadece Customer Senkronize Et

```http
POST /api/backup/sync/customer
POST /api/backup/sync/customer?force=true  # Tam senkronizasyon
```

### 3. Sadece Order Senkronize Et

```http
POST /api/backup/sync/order
POST /api/backup/sync/order?force=true     # Tam senkronizasyon
```

### 4. Backup Veritabanlarını Oluştur

```http
POST /api/backup/initialize
```

**Yanıt:**
```json
{
  "Success": true,
  "Results": [
    { "Service": "customer", "Status": "Initialized" },
    { "Service": "order", "Status": "Initialized" }
  ],
  "Timestamp": "2024-12-07T00:00:00Z"
}
```

### 5. Sağlık Kontrolü

```http
GET /api/health
```

**Yanıt:**
```json
{
  "Status": "Healthy",
  "Service": "BackupServices",
  "Timestamp": "2024-12-07T00:00:00Z"
}
```

---

## Zamanlanmış Yedekleme

### Çalışma Zamanı

- **Saat:** Her gece 00:00:00 UTC
- **Frekans:** Günlük
- **CRON:** `0 0 0 * * *`

### Log Çıktısı

```
========================================
Scheduled sync started at 2024-12-07T00:00:00Z
========================================
Running scheduled sync for customer...
✓ customer sync completed. Inserted: 5, Updated: 10, Deleted: 2, Skipped: 83
Running scheduled sync for order...
✓ order sync completed. Inserted: 3, Updated: 7, Deleted: 0, Skipped: 45
========================================
Scheduled sync summary:
  Success: 2, Failed: 0
  Total Inserted: 8, Updated: 17, Deleted: 2
========================================
Next scheduled sync at 2024-12-08T00:00:00Z
```

---

## Kurulum ve Çalıştırma

### Ön Gereksinimler

- Docker Desktop
- .NET 8.0 SDK
- Azure Functions Core Tools v4

### Adım 1: Docker Container'ları Başlat

```bash
cd c:\Users\ACER\Desktop\Saga_Orchestratation\Microservice_CloudNative_SagaOrchestration

# Tüm container'ları başlat
docker-compose up -d

# Veya sadece backup için gerekli olanları
docker-compose up -d azurite sqlserver sqlserver-backup cosmosdb cosmosdb-backup
```

### Adım 2: Container Durumunu Kontrol Et

```bash
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

Beklenen çıktı:
```
NAMES               STATUS                  PORTS
azurite             Up 2 minutes            0.0.0.0:10000-10002->10000-10002/tcp
sqlserver           Up 2 minutes (healthy)  0.0.0.0:1473->1433/tcp
sqlserver-backup    Up 2 minutes (healthy)  0.0.0.0:1474->1433/tcp
cosmosdb-emulator   Up 2 minutes (healthy)  0.0.0.0:8081->8081/tcp
cosmosdb-backup     Up 2 minutes (healthy)  0.0.0.0:8082->8081/tcp
```

### Adım 3: Azure Functions'ı Başlat

```bash
cd src/services/Backup/BackupServices
func start
```

Functions başladığında:
```
Functions:
    HealthCheck: [GET] http://localhost:7071/api/health
    InitializeBackup: [POST] http://localhost:7071/api/backup/initialize
    SyncCustomer: [POST] http://localhost:7071/api/backup/sync/customer
    SyncOrder: [POST] http://localhost:7071/api/backup/sync/order
    TriggerSync: [POST] http://localhost:7071/api/backup/sync
    ScheduledSync: timerTrigger
```

### Adım 4: Backup Veritabanlarını Oluştur

```bash
curl -X POST http://localhost:7071/api/backup/initialize
```

### Adım 5: Manuel Sync Test Et

```bash
# Tüm servisleri senkronize et
curl -X POST http://localhost:7071/api/backup/sync

# Sadece customer
curl -X POST http://localhost:7071/api/backup/sync/customer

# Sadece order
curl -X POST http://localhost:7071/api/backup/sync/order
```

---

## Test Senaryoları

### Senaryo 1: İlk Senkronizasyon

1. Ana veritabanlarında veri var
2. Backup veritabanları boş
3. Sync çalıştır → Tüm veriler INSERT olarak eklenir

```bash
curl -X POST http://localhost:7071/api/backup/sync
```

Beklenen sonuç:
```json
{
  "InsertedCount": 100,
  "UpdatedCount": 0,
  "DeletedCount": 0,
  "SkippedCount": 0
}
```

### Senaryo 2: Artırımlı Senkronizasyon

1. İlk sync yapılmış
2. Ana veritabanında bazı kayıtlar güncellendi
3. Sync çalıştır → Sadece değişenler UPDATE olur

```bash
curl -X POST http://localhost:7071/api/backup/sync
```

Beklenen sonuç:
```json
{
  "InsertedCount": 0,
  "UpdatedCount": 5,
  "DeletedCount": 0,
  "SkippedCount": 95
}
```

### Senaryo 3: Silinen Kayıtlar

1. Ana veritabanından bazı kayıtlar silindi
2. Sync çalıştır → Backup'tan da silinir

```bash
curl -X POST http://localhost:7071/api/backup/sync
```

Beklenen sonuç:
```json
{
  "InsertedCount": 0,
  "UpdatedCount": 0,
  "DeletedCount": 3,
  "SkippedCount": 97
}
```

### Senaryo 4: Tam Senkronizasyon (Force)

```bash
curl -X POST "http://localhost:7071/api/backup/sync/customer?force=true"
```

Beklenen sonuç:
```json
{
  "InsertedCount": 0,
  "UpdatedCount": 100,  // Tüm kayıtlar güncellendi
  "DeletedCount": 0,
  "SkippedCount": 0
}
```

---

## Sync History

### Customer (SQL Server)

Backup veritabanında `customer.SyncHistory` tablosunda saklanır:

```sql
SELECT * FROM CustomerDb_Backup.customer.SyncHistory
ORDER BY SyncedAt DESC
```

| SyncId | SyncedAt | InsertedCount | UpdatedCount | DeletedCount | Success |
|--------|----------|---------------|--------------|--------------|---------|
| abc123 | 2024-12-07 00:00:00 | 5 | 10 | 2 | 1 |
| def456 | 2024-12-06 00:00:00 | 0 | 3 | 0 | 1 |

### Order (CosmosDB)

Backup veritabanında `sync-history` container'ında saklanır:

```json
{
  "id": "abc123",
  "serviceName": "order",
  "syncedAt": "2024-12-07T00:00:00Z",
  "insertedCount": 3,
  "updatedCount": 7,
  "deletedCount": 0,
  "success": true
}
```

---

## Sorun Giderme

### Timer Trigger Başlamıyor

**Hata:** `The listener for function 'Functions.ScheduledSync' was unable to start`

**Çözüm:** Azurite çalışmıyor. Docker container'ı başlatın:
```bash
docker-compose up -d azurite
```

### CosmosDB Bağlantı Hatası

**Hata:** `Connection refused (127.0.0.1:8081)`

**Çözüm:** 
1. CosmosDB Emulator'ün başlaması 2-3 dakika sürer
2. Container durumunu kontrol edin:
```bash
docker logs cosmosdb-emulator --tail 10
```

### SQL Server Bağlantı Hatası

**Hata:** `Cannot open database "CustomerDb_Backup"`

**Çözüm:** Önce initialize endpoint'ini çağırın:
```bash
curl -X POST http://localhost:7071/api/backup/initialize
```

---

## Özet

Bu Backup Service:

1. ✅ **Artırımlı (Incremental) Yedekleme** - Sadece değişen veriler senkronize edilir
2. ✅ **Veritabanı Seviyesinde Backup** - Ayrı container'larda tam veritabanı
3. ✅ **Zamanlanmış Çalışma** - Her gece 00:00'da otomatik
4. ✅ **Manuel Tetikleme** - HTTP API ile istediğiniz zaman
5. ✅ **Sync History** - Her işlem kaydedilir
6. ✅ **İki Veritabanı Desteği** - SQL Server ve CosmosDB
