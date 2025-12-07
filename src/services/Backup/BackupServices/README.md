# BackupServices - Database-Level Incremental Backup

Bu servis, **Customer** (SQL Server) ve **Order** (CosmosDB) verilerini ayrı backup veritabanlarına **artırımlı (incremental)** olarak senkronize eder.

## 🏗️ Mimari

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         BackupServices                                   │
│                      (Azure Functions v4)                                │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│   ┌──────────────────┐    ┌──────────────────┐                          │
│   │  BackupFunction  │    │  ScheduledSync   │                          │
│   │  (HTTP Trigger)  │    │ (Timer: 00:00)   │                          │
│   └────────┬─────────┘    └────────┬─────────┘                          │
│            │                       │                                     │
│            ▼                       ▼                                     │
│   ┌─────────────────────────────────────────┐                           │
│   │           ISyncService                   │                           │
│   ├─────────────────────────────────────────┤                           │
│   │  CustomerSyncService  │  OrderSyncService│                          │
│   │  (SQL → SQL)          │  (Cosmos→Cosmos) │                          │
│   └───────────┬───────────┴─────────┬───────┘                           │
│               │                     │                                    │
└───────────────┼─────────────────────┼────────────────────────────────────┘
                │                     │
     ┌──────────┴──────────┐ ┌────────┴────────┐
     ▼          ▼          ▼ ▼        ▼        ▼
┌─────────┐ ┌─────────┐  ┌─────────┐ ┌─────────┐
│  SQL    │ │  SQL    │  │ Cosmos  │ │ Cosmos  │
│ Server  │ │ Backup  │  │  DB     │ │ Backup  │
│ :1473   │ │ :1474   │  │ :8081   │ │ :8082   │
│(Source) │ │(Backup) │  │(Source) │ │(Backup) │
└─────────┘ └─────────┘  └─────────┘ └─────────┘
```

## 📊 Veritabanı Yapılandırması

| Rol | Servis | Teknoloji | Port |
|-----|--------|-----------|------|
| **Source** | Customer | SQL Server | 1473 |
| **Backup** | Customer | SQL Server | 1474 |
| **Source** | Order | CosmosDB | 8081 |
| **Backup** | Order | CosmosDB | 8082 |

## 🔄 Artırımlı (Incremental) Sync

Servis, her senkronizasyonda sadece **değişen kayıtları** işler:

| İşlem | Açıklama |
|-------|----------|
| **INSERT** | Source'ta yeni, Backup'ta olmayan kayıt |
| **UPDATE** | Source'ta güncellenen (version > backup version) kayıt |
| **DELETE** | Source'ta silinen, Backup'ta kalan kayıt |
| **SKIP** | Değişmemiş kayıtlar atlanır |

### Customer için karşılaştırma:
```csharp
// Version numarası ile karşılaştırma
if (source.Version > backup.Version)
    // UPDATE gerekli
```

### Order için karşılaştırma:
```csharp
// LastModified tarihi ile karşılaştırma
if (source.LastModified > backup.LastModified)
    // UPDATE gerekli
```

## 🚀 API Endpoints

### Senkronizasyon

```http
# Tüm servisleri senkronize et
POST /api/backup/sync
Content-Type: application/json
{
  "service": "all",        # "all", "customer", veya "order"
  "forceFullSync": false   # true ise tüm kayıtları günceller
}

# Sadece Customer senkronize et
POST /api/backup/sync/customer
POST /api/backup/sync/customer?force=true  # Tam senkronizasyon

# Sadece Order senkronize et
POST /api/backup/sync/order
POST /api/backup/sync/order?force=true     # Tam senkronizasyon
```

### Veritabanı Başlatma

```http
# Backup veritabanlarını oluştur (şema, tablolar)
POST /api/backup/initialize
```

### Sağlık Kontrolü

```http
GET /api/health
```

## ⏰ Zamanlanmış Senkronizasyon

Servis, her gün **gece yarısı (00:00 UTC)** otomatik olarak çalışır.

CRON ifadesi: `0 0 0 * * *`

## ⚙️ Konfigürasyon

### appsettings.json

```json
{
  "SourceCustomerDb": {
    "ConnectionString": "Server=localhost,1473;Database=CustomerDb;..."
  },
  "BackupCustomerDb": {
    "ConnectionString": "Server=localhost,1474;Database=master;..."
  },
  "SourceOrderDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8081/;...",
    "DatabaseName": "orderdb",
    "ContainerName": "orders"
  },
  "BackupOrderDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8082/;...",
    "DatabaseName": "orderdb-backup",
    "ContainerName": "orders"
  },
  "BackupSchedule": {
    "CronExpression": "0 0 0 * * *",
    "Enabled": true
  }
}
```

## 🐳 Docker Compose

Tüm veritabanları docker-compose ile çalışır:

```bash
# Tüm servisleri başlat
docker-compose up -d

# Sadece backup veritabanlarını başlat
docker-compose up -d sqlserver-backup cosmosdb-backup
```

### Container'lar:

| Container | Port | Açıklama |
|-----------|------|----------|
| sqlserver | 1473 | Ana Customer DB |
| sqlserver-backup | 1474 | Backup Customer DB |
| cosmosdb-emulator | 8081 | Ana Order DB |
| cosmosdb-backup | 8082 | Backup Order DB |

## 🛠️ Çalıştırma

### 1. Docker container'ları başlat

```bash
cd c:\Users\ACER\Desktop\Saga_Orchestratation\Microservice_CloudNative_SagaOrchestration
docker-compose up -d
```

### 2. Azure Functions'ı başlat

```bash
cd src/services/Backup/BackupServices
func start
```

### 3. Backup veritabanlarını oluştur

```bash
curl -X POST http://localhost:7071/api/backup/initialize
```

### 4. Manuel senkronizasyon

```bash
curl -X POST http://localhost:7071/api/backup/sync
```

## 📋 Sync Geçmişi

Her senkronizasyon kaydedilir:

### Customer (SQL Server)
```sql
SELECT * FROM CustomerDb_Backup.customer.SyncHistory
ORDER BY SyncedAt DESC
```

### Order (CosmosDB)
Container: `sync-history` içinde saklanır.

## 🔒 Önemli Notlar

1. **Backup DB'ler sadece okuma içindir**: Uygulama Backup DB'lere yazmamalı
2. **İlk senkronizasyon**: İlk çalıştırmada tüm veriler kopyalanır (INSERT)
3. **Silinen kayıtlar**: Source'tan silinen kayıtlar Backup'tan da silinir
4. **Veritabanı oluşturma**: `InitializeBackupDatabaseAsync` otomatik çağrılır

## 📈 Örnek Çıktı

```json
{
  "Success": true,
  "Results": [
    {
      "SyncId": "abc123...",
      "ServiceName": "customer",
      "Status": "Completed",
      "InsertedCount": 5,
      "UpdatedCount": 10,
      "DeletedCount": 2,
      "SkippedCount": 83,
      "TotalProcessed": 17
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
  ]
}
```
