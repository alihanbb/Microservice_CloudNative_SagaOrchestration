 # 💳 Payment Service - Clean Architecture ile Hibrit Yaklaşım

Bu dokümantasyon, **ASP.NET Core Web API** (Clean Architecture) ve **Azure Functions** kombinasyonu ile Payment Service'in detaylı mimarisini ve proje yapısını açıklar.

---

## 📋 İçindekiler

1. [Clean Architecture Nedir?](#clean-architecture-nedir)
2. [Proje Yapısı](#proje-yapısı)
3. [Katman Detayları](#katman-detayları)
4. [Implementasyon](#implementasyon)
5. [Azure Functions Entegrasyonu](#azure-functions-entegrasyonu)
6. [Docker Yapılandırması](#docker-yapılandırması)

---

## Clean Architecture Nedir?

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              CLEAN ARCHITECTURE                                          │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                          │
│                          ┌─────────────────────────────┐                                │
│                          │      API / Workers          │                                │
│                          │     (Presentation)          │                                │
│                          │  Controllers, Functions     │                                │
│                          └─────────────┬───────────────┘                                │
│                                        │                                                 │
│                                        ▼                                                 │
│                     ┌──────────────────────────────────────┐                            │
│                     │         APPLICATION                   │                            │
│                     │  Commands, Queries, Handlers          │                            │
│                     │  DTOs, Validators, Behaviors          │                            │
│                     └─────────────────┬────────────────────┘                            │
│                                       │                                                  │
│                                       ▼                                                  │
│                  ┌─────────────────────────────────────────────┐                        │
│                  │              DOMAIN                          │                        │
│                  │   Entities, Value Objects, Aggregates        │                        │
│                  │   Domain Services, Domain Events             │                        │
│                  │   Repository Interfaces                      │                        │
│                  └──────────────────────────────────────────────┘                        │
│                                       ▲                                                  │
│                                       │                                                  │
│                     ┌─────────────────┴────────────────────┐                            │
│                     │       INFRASTRUCTURE                  │                            │
│                     │  DbContext, Repositories              │                            │
│                     │  External Services, Messaging         │                            │
│                     └──────────────────────────────────────┘                            │
│                                                                                          │
│   BAĞIMLILIK KURALI: İç katmanlar dış katmanları bilmez!                                │
│   Domain → hiçbir şeye bağımlı değil                                                    │
│   Application → sadece Domain'e bağımlı                                                 │
│   Infrastructure/API → Application ve Domain'e bağımlı                                  │
│                                                                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Proje Yapısı

```
src/services/Payment/
│
├── PaymentService.sln
│
├── PaymentService.Domain/                      # 🔶 DOMAIN LAYER (Merkez)
│   ├── PaymentService.Domain.csproj
│   │
│   ├── Aggregates/
│   │   └── PaymentAggregate/
│   │       ├── Payment.cs                      # Aggregate Root
│   │       ├── PaymentId.cs                    # Strongly Typed ID
│   │       ├── Transaction.cs                  # Entity
│   │       └── Refund.cs                       # Entity
│   │
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   ├── CardInfo.cs
│   │   ├── PaymentProvider.cs
│   │   └── PaymentResult.cs
│   │
│   ├── Enums/
│   │   ├── PaymentStatus.cs
│   │   ├── PaymentMethod.cs
│   │   └── TransactionType.cs
│   │
│   ├── Events/
│   │   ├── PaymentCreatedDomainEvent.cs
│   │   ├── PaymentCompletedDomainEvent.cs
│   │   ├── PaymentFailedDomainEvent.cs
│   │   └── PaymentRefundedDomainEvent.cs
│   │
│   ├── Repositories/
│   │   └── IPaymentRepository.cs               # Repository Interface
│   │
│   ├── Services/
│   │   └── IPaymentDomainService.cs            # Domain Service Interface
│   │
│   └── Exceptions/
│       ├── PaymentDomainException.cs
│       ├── InsufficientFundsException.cs
│       └── InvalidPaymentStateException.cs
│
├── PaymentService.Application/                 # 🔷 APPLICATION LAYER
│   ├── PaymentService.Application.csproj
│   │
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IPaymentProvider.cs             # External Provider Interface
│   │   │   ├── IPaymentProviderFactory.cs
│   │   │   └── IServiceBusPublisher.cs
│   │   │
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   ├── LoggingBehavior.cs
│   │   │   └── TransactionBehavior.cs
│   │   │
│   │   └── Mappings/
│   │       └── PaymentMappingProfile.cs
│   │
│   ├── Payments/
│   │   ├── Commands/
│   │   │   ├── ProcessPayment/
│   │   │   │   ├── ProcessPaymentCommand.cs
│   │   │   │   ├── ProcessPaymentCommandHandler.cs
│   │   │   │   └── ProcessPaymentCommandValidator.cs
│   │   │   │
│   │   │   ├── RefundPayment/
│   │   │   │   ├── RefundPaymentCommand.cs
│   │   │   │   ├── RefundPaymentCommandHandler.cs
│   │   │   │   └── RefundPaymentCommandValidator.cs
│   │   │   │
│   │   │   └── UpdatePaymentStatus/
│   │   │       ├── UpdatePaymentStatusCommand.cs
│   │   │       └── UpdatePaymentStatusCommandHandler.cs
│   │   │
│   │   ├── Queries/
│   │   │   ├── GetPaymentById/
│   │   │   │   ├── GetPaymentByIdQuery.cs
│   │   │   │   ├── GetPaymentByIdQueryHandler.cs
│   │   │   │   └── PaymentDto.cs
│   │   │   │
│   │   │   └── GetPaymentsByOrder/
│   │   │       ├── GetPaymentsByOrderQuery.cs
│   │   │       └── GetPaymentsByOrderQueryHandler.cs
│   │   │
│   │   └── EventHandlers/
│   │       ├── PaymentCompletedEventHandler.cs
│   │       └── PaymentFailedEventHandler.cs
│   │
│   ├── Saga/
│   │   └── Contracts/
│   │       ├── ProcessPaymentSagaCommand.cs
│   │       ├── RefundPaymentSagaCommand.cs
│   │       └── PaymentProcessedReply.cs
│   │
│   └── DependencyInjection.cs
│
├── PaymentService.Infrastructure/              # 🔶 INFRASTRUCTURE LAYER
│   ├── PaymentService.Infrastructure.csproj
│   │
│   ├── Persistence/
│   │   ├── PaymentDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── PaymentConfiguration.cs
│   │   │   ├── TransactionConfiguration.cs
│   │   │   └── RefundConfiguration.cs
│   │   │
│   │   ├── Repositories/
│   │   │   └── PaymentRepository.cs
│   │   │
│   │   └── Migrations/
│   │       └── ...
│   │
│   ├── Providers/
│   │   ├── DummyPaymentProvider.cs
│   │   ├── StripePaymentProvider.cs
│   │   ├── IyzicoPaymentProvider.cs
│   │   └── PaymentProviderFactory.cs
│   │
│   ├── ServiceBus/
│   │   ├── ServiceBusPublisher.cs
│   │   └── Consumers/
│   │       └── PaymentStatusChangedConsumer.cs
│   │
│   └── DependencyInjection.cs
│
├── PaymentService.Api/                         # 🔷 PRESENTATION LAYER (API)
│   ├── PaymentService.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   │
│   ├── Controllers/
│   │   └── PaymentsController.cs
│   │
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── CorrelationIdMiddleware.cs
│   │
│   └── Dockerfile
│
├── PaymentService.Workers/                     # 🔷 PRESENTATION LAYER (Functions)
│   ├── PaymentService.Workers.csproj
│   ├── Program.cs
│   ├── host.json
│   │
│   ├── Functions/
│   │   ├── SagaPaymentFunction.cs
│   │   ├── WebhookFunction.cs
│   │   └── ReconciliationFunction.cs
│   │
│   └── Dockerfile
│
└── PaymentService.Tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    └── Integration.Tests/
```

---

## Katman Detayları

### 1. Domain Layer

```csharp
// Domain/Aggregates/PaymentAggregate/Payment.cs

namespace PaymentService.Domain.Aggregates.PaymentAggregate;

public class Payment : Entity, IAggregateRoot
{
    public PaymentId Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private readonly List<Transaction> _transactions = new();
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private readonly List<Refund> _refunds = new();
    public IReadOnlyCollection<Refund> Refunds => _refunds.AsReadOnly();

    private Payment() { } // EF Core için

    public static Payment Create(
        Guid orderId,
        Guid customerId,
        Money amount,
        PaymentMethod method,
        PaymentProvider provider)
    {
        var payment = new Payment
        {
            Id = PaymentId.CreateUnique(),
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            Method = method,
            Provider = provider,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        payment.AddDomainEvent(new PaymentCreatedDomainEvent(payment));
        return payment;
    }

    public void MarkAsProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidPaymentStateException("Only pending payments can be processed");

        Status = PaymentStatus.Processing;
    }

    public void MarkAsCompleted(string providerTransactionId)
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidPaymentStateException("Only processing payments can be completed");

        Status = PaymentStatus.Completed;
        ProviderTransactionId = providerTransactionId;
        ProcessedAt = DateTime.UtcNow;

        _transactions.Add(Transaction.Create(this, TransactionType.Charge, Amount));
        AddDomainEvent(new PaymentCompletedDomainEvent(this));
    }

    public void MarkAsFailed(string errorCode, string errorMessage)
    {
        Status = PaymentStatus.Failed;
        AddDomainEvent(new PaymentFailedDomainEvent(this, errorCode, errorMessage));
    }

    public Refund RequestRefund(Money amount, string reason)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidPaymentStateException("Only completed payments can be refunded");

        if (amount > GetRefundableAmount())
            throw new PaymentDomainException("Refund amount exceeds refundable amount");

        var refund = Refund.Create(this, amount, reason);
        _refunds.Add(refund);

        if (GetRefundableAmount() == Money.Zero(Amount.Currency))
        {
            Status = PaymentStatus.Refunded;
        }
        else
        {
            Status = PaymentStatus.PartialRefund;
        }

        AddDomainEvent(new PaymentRefundedDomainEvent(this, refund));
        return refund;
    }

    public Money GetRefundableAmount()
    {
        var refundedTotal = _refunds
            .Where(r => r.Status == RefundStatus.Completed)
            .Sum(r => r.Amount.Value);

        return Money.Create(Amount.Value - refundedTotal, Amount.Currency);
    }
}
```

### 2. Value Objects

```csharp
// Domain/ValueObjects/Money.cs

namespace PaymentService.Domain.ValueObjects;

public record Money
{
    public decimal Value { get; }
    public string Currency { get; }

    private Money(decimal value, string currency)
    {
        if (value < 0)
            throw new PaymentDomainException("Money value cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new PaymentDomainException("Currency is required");

        Value = value;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Create(decimal value, string currency) => new(value, currency);
    public static Money Zero(string currency) => new(0, currency);
    public static Money TRY(decimal value) => new(value, "TRY");
    public static Money USD(decimal value) => new(value, "USD");

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Value + b.Value, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Value - b.Value, a.Currency);
    }

    public static bool operator >(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Value > b.Value;
    }

    public static bool operator <(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Value < b.Value;
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new PaymentDomainException("Cannot operate on different currencies");
    }
}
```

```csharp
// Domain/ValueObjects/PaymentId.cs

namespace PaymentService.Domain.ValueObjects;

public record PaymentId
{
    public Guid Value { get; }

    private PaymentId(Guid value)
    {
        Value = value;
    }

    public static PaymentId CreateUnique() => new(Guid.NewGuid());
    public static PaymentId Create(Guid value) => new(value);

    public static implicit operator Guid(PaymentId id) => id.Value;
}
```

### 3. Application Layer - Commands

```csharp
// Application/Payments/Commands/ProcessPayment/ProcessPaymentCommand.cs

namespace PaymentService.Application.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand : IRequest<ProcessPaymentResult>
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public PaymentMethod Method { get; init; }
    public string? ProviderName { get; init; }
    
    // Kart bilgileri (nullable - farklı metodlar için)
    public string? CardNumber { get; init; }
    public string? ExpiryMonth { get; init; }
    public string? ExpiryYear { get; init; }
    public string? Cvv { get; init; }
    public string? CardHolderName { get; init; }
    
    // Müşteri bilgileri
    public string? CustomerEmail { get; init; }
    public string? CustomerIp { get; init; }
}

public record ProcessPaymentResult(
    bool Success,
    Guid? PaymentId,
    string? TransactionId,
    PaymentStatus Status,
    string? ErrorCode,
    string? ErrorMessage);
```

```csharp
// Application/Payments/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs

namespace PaymentService.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler 
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IServiceBusPublisher _publisher;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository repository,
        IPaymentProviderFactory providerFactory,
        IServiceBusPublisher publisher,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _repository = repository;
        _providerFactory = providerFactory;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ProcessPaymentResult> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment for order: {OrderId}", request.OrderId);

        // 1. Domain entity oluştur
        var payment = Payment.Create(
            orderId: request.OrderId,
            customerId: request.CustomerId,
            amount: Money.Create(request.Amount, request.Currency),
            method: request.Method,
            provider: PaymentProvider.Create(request.ProviderName ?? "Dummy"));

        await _repository.AddAsync(payment, cancellationToken);

        try
        {
            // 2. Processing durumuna geçir
            payment.MarkAsProcessing();

            // 3. Ödeme sağlayıcısından işle
            var provider = _providerFactory.GetProvider(payment.Provider.Name);
            var providerRequest = MapToProviderRequest(request, payment);
            var response = await provider.ProcessAsync(providerRequest, cancellationToken);

            // 4. Sonuca göre domain durumunu güncelle
            if (response.Success)
            {
                payment.MarkAsCompleted(response.TransactionId!);
            }
            else
            {
                payment.MarkAsFailed(response.ErrorCode!, response.ErrorMessage!);
            }

            await _repository.UpdateAsync(payment, cancellationToken);

            // 5. Domain eventleri publish et
            await _publisher.PublishDomainEventsAsync(payment, cancellationToken);

            return new ProcessPaymentResult(
                Success: response.Success,
                PaymentId: payment.Id.Value,
                TransactionId: response.TransactionId,
                Status: payment.Status,
                ErrorCode: response.ErrorCode,
                ErrorMessage: response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment processing failed: {PaymentId}", payment.Id);

            payment.MarkAsFailed("SYSTEM_ERROR", ex.Message);
            await _repository.UpdateAsync(payment, cancellationToken);

            return new ProcessPaymentResult(
                Success: false,
                PaymentId: payment.Id.Value,
                TransactionId: null,
                Status: PaymentStatus.Failed,
                ErrorCode: "SYSTEM_ERROR",
                ErrorMessage: ex.Message);
        }
    }

    private PaymentProviderRequest MapToProviderRequest(
        ProcessPaymentCommand command, 
        Payment payment)
    {
        return new PaymentProviderRequest(
            PaymentId: payment.Id.Value.ToString(),
            Amount: command.Amount,
            Currency: command.Currency,
            CardNumber: command.CardNumber,
            ExpiryMonth: command.ExpiryMonth,
            ExpiryYear: command.ExpiryYear,
            Cvv: command.Cvv,
            CardHolderName: command.CardHolderName,
            CustomerEmail: command.CustomerEmail,
            CustomerIp: command.CustomerIp,
            Description: $"Order-{command.OrderId}");
    }
}
```

### 4. Application Layer - Queries

```csharp
// Application/Payments/Queries/GetPaymentById/GetPaymentByIdQuery.cs

namespace PaymentService.Application.Payments.Queries.GetPaymentById;

public record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto?>;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    string Method,
    string Provider,
    string? TransactionId,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    decimal RefundableAmount,
    IEnumerable<RefundDto> Refunds);

public record RefundDto(
    Guid Id,
    decimal Amount,
    string Reason,
    string Status,
    DateTime CreatedAt);
```

```csharp
// Application/Payments/Queries/GetPaymentById/GetPaymentByIdQueryHandler.cs

namespace PaymentService.Application.Payments.Queries.GetPaymentById;

public class GetPaymentByIdQueryHandler 
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto?>
{
    private readonly IPaymentRepository _repository;

    public GetPaymentByIdQueryHandler(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaymentDto?> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(
            PaymentId.Create(request.PaymentId),
            cancellationToken);

        if (payment is null)
            return null;

        return new PaymentDto(
            Id: payment.Id.Value,
            OrderId: payment.OrderId,
            CustomerId: payment.CustomerId,
            Amount: payment.Amount.Value,
            Currency: payment.Amount.Currency,
            Status: payment.Status.ToString(),
            Method: payment.Method.ToString(),
            Provider: payment.Provider.Name,
            TransactionId: payment.ProviderTransactionId,
            CreatedAt: payment.CreatedAt,
            ProcessedAt: payment.ProcessedAt,
            RefundableAmount: payment.GetRefundableAmount().Value,
            Refunds: payment.Refunds.Select(r => new RefundDto(
                Id: r.Id,
                Amount: r.Amount.Value,
                Reason: r.Reason,
                Status: r.Status.ToString(),
                CreatedAt: r.CreatedAt)));
    }
}
```

### 5. Application Layer - Validators

```csharp
// Application/Payments/Commands/ProcessPayment/ProcessPaymentCommandValidator.cs

namespace PaymentService.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Valid currency code is required");

        When(x => x.Method == PaymentMethod.CreditCard, () =>
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .Must(BeValidCardNumber)
                .WithMessage("Valid card number is required");

            RuleFor(x => x.ExpiryMonth)
                .NotEmpty()
                .Must(BeValidMonth)
                .WithMessage("Valid expiry month is required");

            RuleFor(x => x.ExpiryYear)
                .NotEmpty()
                .Must(BeValidYear)
                .WithMessage("Valid expiry year is required");

            RuleFor(x => x.Cvv)
                .NotEmpty()
                .Length(3, 4)
                .WithMessage("Valid CVV is required");

            RuleFor(x => x.CardHolderName)
                .NotEmpty()
                .WithMessage("Card holder name is required");
        });
    }

    private bool BeValidCardNumber(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        var cleaned = cardNumber.Replace(" ", "").Replace("-", "");
        return cleaned.Length >= 13 && cleaned.Length <= 19 && cleaned.All(char.IsDigit);
    }

    private bool BeValidMonth(string? month)
    {
        if (!int.TryParse(month, out var m))
            return false;
        return m >= 1 && m <= 12;
    }

    private bool BeValidYear(string? year)
    {
        if (!int.TryParse(year, out var y))
            return false;
        return y >= DateTime.UtcNow.Year % 100;
    }
}
```

### 6. Infrastructure Layer - Repository

```csharp
// Infrastructure/Persistence/PaymentDbContext.cs

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Refund> Refunds => Set<Refund>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

```csharp
// Infrastructure/Persistence/Configurations/PaymentConfiguration.cs

namespace PaymentService.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => PaymentId.Create(value));

        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("Amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        builder.OwnsOne(p => p.Provider, provider =>
        {
            provider.Property(pr => pr.Name)
                .HasColumnName("Provider")
                .HasMaxLength(50);
        });

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(p => p.Transactions)
            .WithOne()
            .HasForeignKey("PaymentId");

        builder.HasMany(p => p.Refunds)
            .WithOne()
            .HasForeignKey("PaymentId");

        builder.Ignore(p => p.DomainEvents);
    }
}
```

```csharp
// Infrastructure/Persistence/Repositories/PaymentRepository.cs

namespace PaymentService.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(
        PaymentId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Transactions)
            .Include(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Refunds)
            .Where(p => p.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 7. API Layer - Controller

```csharp
// Api/Controllers/PaymentsController.cs

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Yeni ödeme işlemi başlatır
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProcessPaymentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentCommand command,
        CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new { result.ErrorCode, result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Ödeme detaylarını getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct)
    {
        var query = new GetPaymentByIdQuery(id);
        var result = await _mediator.Send(query, ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Siparişe ait ödemeleri listeler
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsByOrder(Guid orderId, CancellationToken ct)
    {
        var query = new GetPaymentsByOrderQuery(orderId);
        var result = await _mediator.Send(query, ct);

        return Ok(result);
    }

    /// <summary>
    /// İade işlemi başlatır
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(RefundResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundPaymentRequest request,
        CancellationToken ct)
    {
        var command = new RefundPaymentCommand(id, request.Amount, request.Reason);
        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new { Error = result.ErrorMessage });
        }

        return Ok(result);
    }
}
```

---

## Azure Functions Entegrasyonu

```csharp
// Workers/Functions/SagaPaymentFunction.cs

namespace PaymentService.Workers.Functions;

public class SagaPaymentFunction
{
    private readonly IMediator _mediator;
    private readonly ServiceBusSender _replySender;
    private readonly ILogger<SagaPaymentFunction> _logger;

    public SagaPaymentFunction(
        IMediator mediator,
        ServiceBusClient serviceBusClient,
        ILogger<SagaPaymentFunction> logger)
    {
        _mediator = mediator;
        _replySender = serviceBusClient.CreateSender("saga-reply-queue");
        _logger = logger;
    }

    [Function("HandleProcessPaymentSaga")]
    public async Task HandleProcessPaymentSaga(
        [ServiceBusTrigger("process-payment-queue")] ProcessPaymentSagaCommand sagaCommand)
    {
        _logger.LogInformation("Saga payment command received: {SagaId}", sagaCommand.SagaId);

        // Saga command'ı Application command'a dönüştür
        var command = new ProcessPaymentCommand
        {
            OrderId = sagaCommand.OrderId,
            CustomerId = sagaCommand.CustomerId,
            Amount = sagaCommand.Amount,
            Currency = sagaCommand.Currency,
            Method = sagaCommand.Method,
            CardNumber = sagaCommand.CardNumber,
            ExpiryMonth = sagaCommand.ExpiryMonth,
            ExpiryYear = sagaCommand.ExpiryYear,
            Cvv = sagaCommand.Cvv,
            CardHolderName = sagaCommand.CardHolderName,
            CustomerEmail = sagaCommand.CustomerEmail,
            CustomerIp = sagaCommand.CustomerIp
        };

        // MediatR üzerinden handler'a gönder
        var result = await _mediator.Send(command);

        // Saga reply oluştur ve gönder
        var reply = new PaymentProcessedReply(
            SagaId: sagaCommand.SagaId,
            Success: result.Success,
            PaymentId: result.PaymentId,
            TransactionId: result.TransactionId,
            Error: result.ErrorMessage);

        await SendReplyAsync(reply);
    }

    [Function("HandleRefundPaymentSaga")]
    public async Task HandleRefundPaymentSaga(
        [ServiceBusTrigger("refund-payment-queue")] RefundPaymentSagaCommand sagaCommand)
    {
        _logger.LogInformation("Saga refund command received: {SagaId}", sagaCommand.SagaId);

        var command = new RefundPaymentCommand(
            sagaCommand.PaymentId,
            sagaCommand.Amount,
            sagaCommand.Reason ?? "Saga compensation");

        await _mediator.Send(command);
    }

    private async Task SendReplyAsync(PaymentProcessedReply reply)
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(reply))
        {
            ContentType = "application/json"
        };
        message.ApplicationProperties["MessageType"] = nameof(PaymentProcessedReply);

        await _replySender.SendMessageAsync(message);
    }
}
```

### Workers Program.cs

```csharp
// Workers/Program.cs

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Application Layer DI
        services.AddApplication();

        // Infrastructure Layer DI
        services.AddInfrastructure(configuration);

        // Service Bus
        services.AddSingleton(new ServiceBusClient(
            configuration["ServiceBusConnection"]));

        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(ProcessPaymentCommand).Assembly));
    })
    .Build();

host.Run();
```

---

## Docker Yapılandırması

```yaml
# docker-compose.yml - Payment Service

  payment-api:
    build:
      context: ./src/services/Payment
      dockerfile: PaymentService.Api/Dockerfile
    container_name: payment-api
    ports:
      - "5080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__PaymentDb=Server=payment-db;Database=PaymentDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
      - ServiceBusConnection=${SERVICEBUS_CONNECTION}
    depends_on:
      - payment-db
    networks:
      - saga-network

  payment-workers:
    build:
      context: ./src/services/Payment
      dockerfile: PaymentService.Workers/Dockerfile
    container_name: payment-workers
    ports:
      - "7073:80"
    environment:
      - AzureWebJobsStorage=UseDevelopmentStorage=true
      - ConnectionStrings__PaymentDb=Server=payment-db;Database=PaymentDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true
      - ServiceBusConnection=${SERVICEBUS_CONNECTION}
    depends_on:
      - payment-api
      - azurite
    networks:
      - saga-network

  payment-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: payment-db
    ports:
      - "1475:1433"
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    networks:
      - saga-network
```

---

## Özet

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                           CLEAN ARCHITECTURE KATMANLARI                                  │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                          │
│   DOMAIN (Merkez)                                                                       │
│   ├── Entities (Payment, Transaction, Refund)                                           │
│   ├── Value Objects (Money, PaymentId, PaymentProvider)                                  │
│   ├── Domain Events                                                                      │
│   ├── Repository Interfaces                                                              │
│   └── Domain Exceptions                                                                  │
│                                                                                          │
│   APPLICATION                                                                            │
│   ├── Commands + Handlers (CQRS)                                                         │
│   ├── Queries + Handlers                                                                 │
│   ├── Validators (FluentValidation)                                                      │
│   ├── Behaviors (Logging, Transaction)                                                   │
│   └── Saga Contracts                                                                     │
│                                                                                          │
│   INFRASTRUCTURE                                                                         │
│   ├── DbContext + Configurations                                                         │
│   ├── Repository Implementations                                                         │
│   ├── Payment Providers (Stripe, Iyzico, Dummy)                                          │
│   └── Service Bus Publisher                                                              │
│                                                                                          │
│   PRESENTATION                                                                           │
│   ├── API (Controllers)                                                                  │
│   └── Workers (Azure Functions)                                                          │
│                                                                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

| Katman | Proje | İçerik |
|--------|-------|--------|
| Domain | `PaymentService.Domain` | Entities, Value Objects, Events |
| Application | `PaymentService.Application` | CQRS, Validators, Behaviors |
| Infrastructure | `PaymentService.Infrastructure` | DB, Providers, Messaging |
| Presentation | `PaymentService.Api` | REST API Controllers |
| Presentation | `PaymentService.Workers` | Azure Functions |
