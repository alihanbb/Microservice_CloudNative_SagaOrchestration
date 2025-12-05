# ?? Order Service

Order Service, Saga Orchestration tabanlý microservice mimarisinin sipariþ yönetim servisidir. **Domain-Driven Design (DDD)**, **CQRS**, **Vertical Slice Architecture** ve **Minimal API** pattern'leri kullanýlarak geliþtirilmiþtir.

## ??? Mimari

```
???????????????????????????????????????????????????????????????????????
?                         Order Service                                ?
???????????????????????????????????????????????????????????????????????
?  ???????????????  ???????????????  ???????????????  ??????????????? ?
?  ?     Api     ?  ? Application ?  ?   Domain    ?  ?    Infra    ? ?
?  ?  (Minimal   ????   (CQRS)    ????   (DDD)     ????  (EF Core)  ? ?
?  ?    API)     ?  ?             ?  ?             ?  ?             ? ?
?  ???????????????  ???????????????  ???????????????  ??????????????? ?
???????????????????????????????????????????????????????????????????????
```

### Katman Yapýsý

| Katman | Proje | Sorumluluk |
|--------|-------|------------|
| **Api** | `OrderServices.Api` | HTTP endpoints, Middleware, Request/Response |
| **Application** | `OrderServices.Application` | Use cases, Commands, Queries, Validators |
| **Domain** | `OrderServices.Domain` | Business logic, Entities, Value Objects, Events |
| **Infrastructure** | `OrderServices.Infra` | Database, Repositories, External services |

## ?? Proje Yapýsý

```
src/services/Order/
??? OrderServices.Api/
?   ??? Contracts/
?   ?   ??? Requests/           # Request DTOs
?   ?   ??? Responses/          # Response DTOs
?   ??? Endpoints/
?   ?   ??? Orders/             # Minimal API Endpoints
?   ??? Extensions/             # Service extensions
?   ??? Middleware/             # Global exception handling
?   ??? Configuration/          # App configuration
?   ??? Program.cs
?
??? OrderServices.Application/
?   ??? Common/
?   ?   ??? Interfaces/         # ICommand, IQuery
?   ?   ??? Exceptions/         # Application exceptions
?   ?   ??? Result.cs           # Result pattern
?   ??? Behaviors/              # MediatR pipeline behaviors
?   ?   ??? LoggingBehavior.cs
?   ?   ??? ValidationBehavior.cs
?   ?   ??? TransactionBehavior.cs
?   ??? Orders/                 # Vertical Slices
?       ??? CreateOrder/
?       ??? GetOrderById/
?       ??? CancelOrder/
?       ??? ConfirmOrder/
?       ??? DomainEventHandlers/
?
??? OrderServices.Domain/
?   ??? Aggregate/
?   ?   ??? Order.cs            # Aggregate Root
?   ?   ??? OrderItem.cs        # Entity
?   ?   ??? OrderStatus.cs      # Value Object
?   ?   ??? IOrderRepository.cs
?   ??? Events/                 # Domain Events
?   ??? Exceptions/             # Domain Exceptions
?   ??? SeedWork/               # Base classes
?
??? OrderServices.Infra/
    ??? EntityConfigurations/   # EF Core configurations
    ??? Idempotency/            # Idempotent request handling
    ??? OrderDbContext.cs
    ??? OrderRepository.cs
    ??? DependencyInjection.cs
```

## ?? Teknolojiler

| Teknoloji | Versiyon | Kullaným |
|-----------|----------|----------|
| .NET | 9.0 | Runtime |
| Entity Framework Core | 9.0 | ORM |
| MediatR | 14.0 | CQRS & Mediator |
| FluentValidation | 11.11 | Validation |
| SQL Server | - | Database |
| Azure Cosmos DB | - | Document store (optional) |
| Azure Key Vault | - | Secrets management |
| Swagger/OpenAPI | - | API documentation |

## ?? API Endpoints

### Orders

| Method | Endpoint | Açýklama |
|--------|----------|----------|
| `POST` | `/api/orders` | Yeni sipariþ oluþtur |
| `GET` | `/api/orders/{id}` | Sipariþ detayý getir |
| `GET` | `/api/orders/customer/{customerId}` | Müþteri sipariþlerini getir |
| `GET` | `/api/orders/status/{status}` | Status'a göre sipariþler |
| `POST` | `/api/orders/{id}/confirm` | Sipariþ onayla |
| `POST` | `/api/orders/{id}/cancel` | Sipariþ iptal et |
| `PUT` | `/api/orders/{id}/status` | Status güncelle |

### Order Items

| Method | Endpoint | Açýklama |
|--------|----------|----------|
| `POST` | `/api/orders/{id}/items` | Sipariþe ürün ekle |
| `DELETE` | `/api/orders/{id}/items/{productId}` | Sipariþten ürün çýkar |

### Health & Info

| Method | Endpoint | Açýklama |
|--------|----------|----------|
| `GET` | `/` | Service bilgisi |
| `GET` | `/health` | Health check |
| `GET` | `/healthchecks-ui` | Health checks UI |
| `GET` | `/swagger` | API dokümantasyonu |

## ?? Order Status Workflow

```
???????????    ?????????????    ????????    ???????????    ?????????????
? Pending ?????? Confirmed ?????? Paid ?????? Shipped ?????? Delivered ?
???????????    ?????????????    ????????    ???????????    ?????????????
     ?                                            
     ?         ?????????????                      
     ??????????? Cancelled ?                      
               ?????????????                      
```

**Valid Status Transitions:**
- `Pending` ? `Confirmed` | `Cancelled`
- `Confirmed` ? `Paid` | `Cancelled`
- `Paid` ? `Shipped` | `Cancelled`
- `Shipped` ? `Delivered` | `Cancelled`
- `Delivered` ? ? (final state)
- `Cancelled` ? ? (final state)

## ?? Kurulum

### Gereksinimler

- .NET 9.0 SDK
- SQL Server (LocalDB veya full instance)
- (Opsiyonel) Azure Cosmos DB Emulator
- (Opsiyonel) Azure Key Vault

### Yapýlandýrma

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OrderDb": "Server=(localdb)\\mssqllocaldb;Database=OrderDb;Trusted_Connection=True;"
  },
  "CosmosDb": {
    "DatabaseName": "OrdersDb",
    "ContainerName": "Orders"
  },
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  }
}
```

### Çalýþtýrma

```bash
# Proje dizinine git
cd src/services/Order/OrderServices.Api

# Restore packages
dotnet restore

# Database migration (opsiyonel)
dotnet ef database update --project ../OrderServices.Infra

# Çalýþtýr
dotnet run

# Veya watch mode
dotnet watch run
```

## ?? Kullaným Örnekleri

### Sipariþ Oluþturma

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerName": "John Doe",
    "items": [
      {
        "productId": "550e8400-e29b-41d4-a716-446655440000",
        "productName": "Laptop",
        "quantity": 1,
        "unitPrice": 1299.99
      },
      {
        "productId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
        "productName": "Mouse",
        "quantity": 2,
        "unitPrice": 29.99
      }
    ]
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "orderId": 1,
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerName": "John Doe",
    "orderDate": "2024-01-15T10:30:00Z",
    "status": "Pending",
    "totalAmount": 1359.97,
    "items": [
      {
        "productId": "550e8400-e29b-41d4-a716-446655440000",
        "productName": "Laptop",
        "quantity": 1,
        "unitPrice": 1299.99,
        "totalPrice": 1299.99
      },
      {
        "productId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
        "productName": "Mouse",
        "quantity": 2,
        "unitPrice": 29.99,
        "totalPrice": 59.98
      }
    ]
  },
  "message": null
}
```

### Sipariþ Onaylama

```bash
curl -X POST http://localhost:5000/api/orders/1/confirm
```

### Sipariþ Ýptal Etme

```bash
curl -X POST http://localhost:5000/api/orders/1/cancel \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Customer requested cancellation"
  }'
```

### Status Güncelleme

```bash
curl -X PUT http://localhost:5000/api/orders/1/status \
  -H "Content-Type: application/json" \
  -d '{
    "status": "Paid"
  }'
```

## ??? Domain-Driven Design

### Aggregate Root: Order

```csharp
public class Order : OrderEntity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; }
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<OrderItem> OrderItems { get; }

    // Business methods
    public void AddOrderItem(Guid productId, string productName, int quantity, decimal unitPrice);
    public void RemoveOrderItem(Guid productId);
    public void ConfirmOrder();
    public void SetPaidStatus();
    public void SetShippedStatus();
    public void SetDeliveredStatus();
    public void CancelOrder(string reason);
}
```

### Domain Events

| Event | Tetiklenen Zaman |
|-------|------------------|
| `OrderStartedDomainEvent` | Sipariþ oluþturulduðunda |
| `OrderItemAddedDomainEvent` | Ürün eklendiðinde |
| `OrderStatusChangedDomainEvent` | Status deðiþtiðinde |
| `OrderCancelledDomainEvent` | Ýptal edildiðinde |

### Value Object: OrderStatus

```csharp
public class OrderStatus : ValueObject
{
    public static OrderStatus Pending = new(1, nameof(Pending));
    public static OrderStatus Confirmed = new(2, nameof(Confirmed));
    public static OrderStatus Paid = new(3, nameof(Paid));
    public static OrderStatus Shipped = new(4, nameof(Shipped));
    public static OrderStatus Delivered = new(5, nameof(Delivered));
    public static OrderStatus Cancelled = new(6, nameof(Cancelled));
    public static OrderStatus Refunded = new(7, nameof(Refunded));
}
```

## ?? CQRS Pattern

### Commands (Write Operations)

| Command | Açýklama |
|---------|----------|
| `CreateOrderCommand` | Yeni sipariþ oluþtur |
| `ConfirmOrderCommand` | Sipariþ onayla |
| `CancelOrderCommand` | Sipariþ iptal et |
| `UpdateOrderStatusCommand` | Status güncelle |
| `AddOrderItemCommand` | Ürün ekle |
| `RemoveOrderItemCommand` | Ürün çýkar |

### Queries (Read Operations)

| Query | Açýklama |
|-------|----------|
| `GetOrderByIdQuery` | ID ile sipariþ getir |
| `GetOrdersByCustomerQuery` | Müþteri sipariþleri |
| `GetOrdersByStatusQuery` | Status'a göre sipariþler |

## ??? Pipeline Behaviors

```
Request ? LoggingBehavior ? ValidationBehavior ? TransactionBehavior ? Handler ? Response
```

| Behavior | Sorumluluk |
|----------|------------|
| `LoggingBehavior` | Request/Response logging |
| `ValidationBehavior` | FluentValidation ile doðrulama |
| `TransactionBehavior` | Unit of Work yönetimi |

## ??? Database Schema

```sql
-- Orders table
CREATE TABLE ordering.orders (
    Id INT IDENTITY PRIMARY KEY,
    CustomerId UNIQUEIDENTIFIER NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    OrderDate DATETIME NOT NULL,
    StatusId INT NOT NULL,
    StatusName NVARCHAR(50) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL
);

-- OrderItems table
CREATE TABLE ordering.orderitems (
    Id INT IDENTITY PRIMARY KEY,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES ordering.orders(Id),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);

-- ClientRequests table (Idempotency)
CREATE TABLE ordering.clientrequests (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Time DATETIME NOT NULL
);
```

## ?? Test

```bash
# Unit tests
dotnet test

# Integration tests
dotnet test --filter Category=Integration
```

## ?? Ýlgili Dökümanlar

- [Domain-Driven Design](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://docs.fluentvalidation.net/)

## ?? Katkýda Bulunma

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## ?? Lisans

Bu proje MIT lisansý altýnda lisanslanmýþtýr.

---

**?? Happy Coding!**
