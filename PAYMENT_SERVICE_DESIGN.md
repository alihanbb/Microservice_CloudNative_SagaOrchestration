# 💳 Payment Service Tasarım ve Implementasyon Kılavuzu

Bu dokümantasyon, mikroservis mimarisinde **Payment Service**'in nasıl oluşturulacağını detaylı olarak açıklar.

---

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Mimari Seçenekler](#mimari-seçenekler)
3. [Önerilen Yaklaşım](#önerilen-yaklaşım)
4. [Proje Yapısı](#proje-yapısı)
5. [Implementasyon](#implementasyon)
6. [Saga Entegrasyonu](#saga-entegrasyonu)
7. [Ödeme Sağlayıcı Entegrasyonu](#ödeme-sağlayıcı-entegrasyonu)
8. [Docker Yapılandırması](#docker-yapılandırması)
9. [Test ve Doğrulama](#test-ve-doğrulama)

---

## Genel Bakış

Payment Service, sipariş sürecinde ödeme işlemlerini yöneten kritik bir mikroservistir.

### Temel Sorumluluklar

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         PAYMENT SERVICE SORUMLULUKLARI                           │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ┌─────────────────────────────────────────────────────────────────────────┐   │
│   │  💳 ÖDEME İŞLEMLERİ                                                     │   │
│   │                                                                         │   │
│   │  • Ödeme başlatma (kredi kartı, havale, vb.)                           │   │
│   │  • Ödeme onaylama                                                       │   │
│   │  • Ödeme iptali (compensation için)                                     │   │
│   │  • İade işlemleri (refund)                                              │   │
│   │  • Ödeme durumu sorgulama                                               │   │
│   └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│   ┌─────────────────────────────────────────────────────────────────────────┐   │
│   │  🔗 SAGA ENTEGRASYONU                                                   │   │
│   │                                                                         │   │
│   │  • ProcessPaymentCommand dinleme                                        │   │
│   │  • PaymentProcessedReply gönderme                                       │   │
│   │  • RefundPaymentCommand dinleme (compensation)                          │   │
│   └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│   ┌─────────────────────────────────────────────────────────────────────────┐   │
│   │  🔐 GÜVENLİK                                                            │   │
│   │                                                                         │   │
│   │  • PCI DSS uyumluluğu                                                   │   │
│   │  • Kart bilgisi tokenizasyonu                                           │   │
│   │  • 3D Secure desteği                                                    │   │
│   │  • Fraud detection                                                      │   │
│   └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Mimari Seçenekler

### Seçenek 1: Azure Functions

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        AZURE FUNCTIONS YAKLAŞIMI                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ╔═══════════════════════════════════════════════════════════════════════════╗ │
│   ║                         AVANTAJLAR ✓                                      ║ │
│   ╠═══════════════════════════════════════════════════════════════════════════╣ │
│   ║                                                                           ║ │
│   ║  • Serverless - ölçekleme otomatik                                        ║ │
│   ║  • Event-driven - Service Bus trigger'ları kolay                          ║ │
│   ║  • Maliyet etkin - kullanıldığı kadar ödeme                               ║ │
│   ║  • Saga orkestrasyon için ideal                                           ║ │
│   ║  • Timer trigger ile zamanlanmış işler                                    ║ │
│   ║  • Notification/Backup servisleriyle tutarlı mimari                       ║ │
│   ║                                                                           ║ │
│   ╚═══════════════════════════════════════════════════════════════════════════╝ │
│                                                                                  │
│   ╔═══════════════════════════════════════════════════════════════════════════╗ │
│   ║                         DEZAVANTAJLAR ✗                                   ║ │
│   ╠═══════════════════════════════════════════════════════════════════════════╣ │
│   ║                                                                           ║ │
│   ║  • Karmaşık domain logic için sınırlı                                     ║ │
│   ║  • Cold start latency                                                     ║ │
│   ║  • Stateful işlemler için ek çaba                                         ║ │
│   ║  • Debugging daha zor                                                     ║ │
│   ║                                                                           ║ │
│   ╚═══════════════════════════════════════════════════════════════════════════╝ │
│                                                                                  │
│   NE ZAMAN TERCİH EDİLMELİ?                                                     │
│   • Basit ödeme işlemleri (gateway'e yönlendirme)                               │
│   • Event-driven mimari ağırlıklı                                               │
│   • Düşük istikrarlı trafik                                                     │
│   • Saga participant olarak kullanım                                            │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Seçenek 2: ASP.NET Core Web API

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        ASP.NET CORE WEB API YAKLAŞIMI                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ╔═══════════════════════════════════════════════════════════════════════════╗ │
│   ║                         AVANTAJLAR ✓                                      ║ │
│   ╠═══════════════════════════════════════════════════════════════════════════╣ │
│   ║                                                                           ║ │
│   ║  • Clean Architecture / DDD desteği                                       ║ │
│   ║  • Karmaşık domain logic için ideal                                       ║ │
│   ║  • Daha iyi debugging ve testing                                          ║ │
│   ║  • Mevcut Customer/Order/Product servisleriyle tutarlı                    ║ │
│   ║  • Stateful işlemler için uygun                                           ║ │
│   ║  • Daha esnek middleware pipeline                                         ║ │
│   ║                                                                           ║ │
│   ╚═══════════════════════════════════════════════════════════════════════════╝ │
│                                                                                  │
│   ╔═══════════════════════════════════════════════════════════════════════════╗ │
│   ║                         DEZAVANTAJLAR ✗                                   ║ │
│   ╠═══════════════════════════════════════════════════════════════════════════╣ │
│   ║                                                                           ║ │
│   ║  • Manuel ölçekleme gerekli                                               ║ │
│   ║  • Sürekli çalışan container maliyeti                                     ║ │
│   ║  • Service Bus tüketimi için ek yapılandırma                              ║ │
│   ║                                                                           ║ │
│   ╚═══════════════════════════════════════════════════════════════════════════╝ │
│                                                                                  │
│   NE ZAMAN TERCİH EDİLMELİ?                                                     │
│   • Karmaşık ödeme iş mantığı                                                   │
│   • Yüksek ve sürekli trafik                                                    │
│   • PCI DSS uyumluluk gereksinimleri                                            │
│   • Birden fazla ödeme sağlayıcı entegrasyonu                                   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Seçenek 3: Hibrit Yaklaşım (ÖNERİLEN)

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            HİBRİT YAKLAŞIM ⭐                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ┌─────────────────────────────────────────────────────────────────────────┐   │
│   │                                                                         │   │
│   │  ASP.NET Core API                    Azure Functions                    │   │
│   │  (Ana Servis)                        (Event Handler)                    │   │
│   │                                                                         │   │
│   │  ┌─────────────────────┐             ┌─────────────────────┐           │   │
│   │  │                     │             │                     │           │   │
│   │  │  • REST API         │             │  • Saga Commands    │           │   │
│   │  │  • Domain Logic     │◀───────────▶│  • Event Processing │           │   │
│   │  │  • DB Operations    │   Internal  │  • Notifications    │           │   │
│   │  │  • Validations      │    Call     │                     │           │   │
│   │  │                     │             │                     │           │   │
│   │  └──────────┬──────────┘             └──────────┬──────────┘           │   │
│   │             │                                   │                       │   │
│   │             │                                   │                       │   │
│   │             ▼                                   ▼                       │   │
│   │  ┌─────────────────────┐             ┌─────────────────────┐           │   │
│   │  │                     │             │                     │           │   │
│   │  │   Payment DB        │             │   Service Bus       │           │   │
│   │  │   (SQL Server)      │             │                     │           │   │
│   │  │                     │             │                     │           │   │
│   │  └─────────────────────┘             └─────────────────────┘           │   │
│   │                                                                         │   │
│   └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                  │
│   AVANTAJLAR:                                                                   │
│   • Her iki dünyanın en iyisi                                                   │
│   • Clean Architecture + Event-driven                                           │
│   • Test edilebilirlik + Ölçeklenebilirlik                                      │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Önerilen Yaklaşım

Bu proje için **Azure Functions** yaklaşımını öneriyorum çünkü:

1. ✅ Mevcut Notification ve Backup servisleriyle tutarlı
2. ✅ Saga orchestration için ideal (event-driven)
3. ✅ Daha az altyapı yönetimi
4. ✅ Service Bus trigger'ları doğal olarak destekleniyor

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        ÖNERİLEN: AZURE FUNCTIONS                                 │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│                         PaymentServices/                                         │
│                         (Azure Functions v4)                                     │
│                               │                                                  │
│      ┌──────────────────┬─────┴─────┬──────────────────┐                        │
│      │                  │           │                  │                        │
│      ▼                  ▼           ▼                  ▼                        │
│  ┌────────┐       ┌────────┐   ┌────────┐       ┌────────┐                     │
│  │ HTTP   │       │Service │   │ Timer  │       │ Event  │                     │
│  │Trigger │       │  Bus   │   │Trigger │       │ Grid   │                     │
│  │        │       │Trigger │   │        │       │Trigger │                     │
│  └────────┘       └────────┘   └────────┘       └────────┘                     │
│      │                  │           │                  │                        │
│      ├──────────────────┼───────────┼──────────────────┤                        │
│      │                  │           │                  │                        │
│      ▼                  ▼           ▼                  ▼                        │
│  ┌─────────────────────────────────────────────────────────┐                   │
│  │                                                         │                   │
│  │                   PAYMENT SERVICES                      │                   │
│  │                                                         │                   │
│  │  • ProcessPayment      • RefundPayment                  │                   │
│  │  • GetPaymentStatus    • ListPayments                   │                   │
│  │  • WebhookHandler      • ReconciliationJob              │                   │
│  │                                                         │                   │
│  └─────────────────────────────────────────────────────────┘                   │
│                               │                                                  │
│                               ▼                                                  │
│  ┌─────────────────────────────────────────────────────────┐                   │
│  │                                                         │                   │
│  │                  PAYMENT PROVIDERS                      │                   │
│  │                                                         │                   │
│  │  • Stripe      • PayPal      • Iyzico                  │                   │
│  │  • PayTR       • Dummy (Test)                          │                   │
│  │                                                         │                   │
│  └─────────────────────────────────────────────────────────┘                   │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## Proje Yapısı

```
src/services/Payment/
├── PaymentServices/                    # Azure Functions Projesi
│   ├── Functions/
│   │   ├── PaymentApiFunction.cs       # HTTP API endpoints
│   │   ├── SagaPaymentFunction.cs      # Saga command handlers
│   │   ├── WebhookFunction.cs          # Ödeme sağlayıcı webhook'ları
│   │   ├── ReconciliationFunction.cs   # Günlük mutabakat (Timer)
│   │   └── HealthCheckFunction.cs      # Sağlık kontrolü
│   │
│   ├── Services/
│   │   ├── IPaymentService.cs          # Ana ödeme arayüzü
│   │   ├── PaymentService.cs           # Ödeme iş mantığı
│   │   ├── IPaymentProvider.cs         # Sağlayıcı arayüzü
│   │   ├── Providers/
│   │   │   ├── StripePaymentProvider.cs
│   │   │   ├── PayPalPaymentProvider.cs
│   │   │   ├── IyzicoPaymentProvider.cs
│   │   │   └── DummyPaymentProvider.cs # Test için
│   │   └── PaymentProviderFactory.cs
│   │
│   ├── Models/
│   │   ├── Payment.cs                  # Ödeme entity
│   │   ├── PaymentStatus.cs            # Enum
│   │   ├── PaymentMethod.cs            # Enum
│   │   ├── PaymentRequest.cs           # DTO
│   │   ├── PaymentResponse.cs          # DTO
│   │   └── RefundRequest.cs            # DTO
│   │
│   ├── Repositories/
│   │   ├── IPaymentRepository.cs
│   │   └── CosmosPaymentRepository.cs  # CosmosDB implementasyonu
│   │
│   ├── Configuration/
│   │   └── PaymentConfiguration.cs
│   │
│   ├── Program.cs
│   ├── appsettings.json
│   ├── host.json
│   └── local.settings.json
│
└── PaymentServices.Tests/              # Unit testler
    └── ...
```

---

## Implementasyon

### 1. Payment Entity

```csharp
// Models/Payment.cs

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    
    // Provider bilgileri
    public string Provider { get; set; }
    public string ProviderTransactionId { get; set; }
    public string ProviderResponse { get; set; }
    
    // Zaman damgaları
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    
    // İade bilgileri
    public bool IsRefunded { get; set; }
    public decimal? RefundedAmount { get; set; }
    
    // Hata bilgileri
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
}

public enum PaymentStatus
{
    Pending,        // Bekliyor
    Processing,     // İşleniyor
    Completed,      // Tamamlandı
    Failed,         // Başarısız
    Cancelled,      // İptal edildi
    Refunded,       // İade edildi
    PartialRefund   // Kısmi iade
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    Wallet,
    PayPal
}
```

### 2. Payment Provider Interface

```csharp
// Services/IPaymentProvider.cs

public interface IPaymentProvider
{
    string ProviderName { get; }
    
    Task<PaymentProviderResponse> ProcessPaymentAsync(PaymentProviderRequest request);
    Task<PaymentProviderResponse> RefundPaymentAsync(RefundProviderRequest request);
    Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId);
    bool ValidateWebhook(string payload, string signature);
}

public record PaymentProviderRequest(
    string OrderId,
    decimal Amount,
    string Currency,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv,
    string CardHolderName,
    string CustomerEmail,
    string CustomerIp,
    string Description);

public record PaymentProviderResponse(
    bool Success,
    string TransactionId,
    string Status,
    string ErrorCode,
    string ErrorMessage,
    string RawResponse);

public record RefundProviderRequest(
    string TransactionId,
    decimal Amount,
    string Reason);
```

### 3. Dummy Payment Provider (Test için)

```csharp
// Services/Providers/DummyPaymentProvider.cs

public class DummyPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Dummy";

    public Task<PaymentProviderResponse> ProcessPaymentAsync(PaymentProviderRequest request)
    {
        // Test kartı numarası kontrolü
        var cardNumber = request.CardNumber.Replace(" ", "");
        
        // Başarısız test kartı
        if (cardNumber.EndsWith("0000"))
        {
            return Task.FromResult(new PaymentProviderResponse(
                Success: false,
                TransactionId: null,
                Status: "failed",
                ErrorCode: "INSUFFICIENT_FUNDS",
                ErrorMessage: "Yetersiz bakiye",
                RawResponse: "{}"));
        }
        
        // Başarılı ödeme
        return Task.FromResult(new PaymentProviderResponse(
            Success: true,
            TransactionId: $"DUMMY-{Guid.NewGuid():N}",
            Status: "completed",
            ErrorCode: null,
            ErrorMessage: null,
            RawResponse: "{}"));
    }

    public Task<PaymentProviderResponse> RefundPaymentAsync(RefundProviderRequest request)
    {
        return Task.FromResult(new PaymentProviderResponse(
            Success: true,
            TransactionId: $"REFUND-{Guid.NewGuid():N}",
            Status: "refunded",
            ErrorCode: null,
            ErrorMessage: null,
            RawResponse: "{}"));
    }

    public Task<PaymentStatusResponse> GetPaymentStatusAsync(string transactionId)
    {
        return Task.FromResult(new PaymentStatusResponse(
            TransactionId: transactionId,
            Status: "completed",
            Amount: 100,
            Currency: "TRY"));
    }

    public bool ValidateWebhook(string payload, string signature)
    {
        return true; // Dummy provider için her zaman geçerli
    }
}
```

### 4. Payment Service

```csharp
// Services/PaymentService.cs

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository repository,
        IPaymentProviderFactory providerFactory,
        ILogger<PaymentService> logger)
    {
        _repository = repository;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        _logger.LogInformation("Processing payment for order: {OrderId}", request.OrderId);

        // Ödeme kaydı oluştur
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Currency = request.Currency,
            Method = request.Method,
            Provider = request.Provider ?? "Dummy",
            Status = PaymentStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(payment);

        try
        {
            // Ödeme sağlayıcısı al
            var provider = _providerFactory.GetProvider(payment.Provider);

            // Ödeme işle
            var providerRequest = new PaymentProviderRequest(
                OrderId: request.OrderId.ToString(),
                Amount: request.Amount,
                Currency: request.Currency,
                CardNumber: request.CardNumber,
                ExpiryMonth: request.ExpiryMonth,
                ExpiryYear: request.ExpiryYear,
                Cvv: request.Cvv,
                CardHolderName: request.CardHolderName,
                CustomerEmail: request.CustomerEmail,
                CustomerIp: request.CustomerIp,
                Description: $"Order #{request.OrderId}");

            var response = await provider.ProcessPaymentAsync(providerRequest);

            // Sonucu kaydet
            payment.ProviderTransactionId = response.TransactionId;
            payment.ProviderResponse = response.RawResponse;

            if (response.Success)
            {
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                
                _logger.LogInformation("Payment completed: {PaymentId}", payment.Id);
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.ErrorCode = response.ErrorCode;
                payment.ErrorMessage = response.ErrorMessage;
                
                _logger.LogWarning("Payment failed: {PaymentId}, Error: {Error}", 
                    payment.Id, response.ErrorMessage);
            }

            await _repository.UpdateAsync(payment);

            return new PaymentResult(
                Success: response.Success,
                PaymentId: payment.Id,
                TransactionId: response.TransactionId,
                Status: payment.Status,
                ErrorCode: response.ErrorCode,
                ErrorMessage: response.ErrorMessage);
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.ErrorMessage = ex.Message;
            await _repository.UpdateAsync(payment);

            _logger.LogError(ex, "Payment processing error: {PaymentId}", payment.Id);

            return new PaymentResult(
                Success: false,
                PaymentId: payment.Id,
                TransactionId: null,
                Status: PaymentStatus.Failed,
                ErrorCode: "SYSTEM_ERROR",
                ErrorMessage: ex.Message);
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(RefundPaymentRequest request)
    {
        var payment = await _repository.GetByIdAsync(request.PaymentId);

        if (payment == null)
        {
            return new RefundResult(false, "Payment not found");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            return new RefundResult(false, "Only completed payments can be refunded");
        }

        var provider = _providerFactory.GetProvider(payment.Provider);

        var refundRequest = new RefundProviderRequest(
            payment.ProviderTransactionId,
            request.Amount ?? payment.Amount,
            request.Reason);

        var response = await provider.RefundPaymentAsync(refundRequest);

        if (response.Success)
        {
            payment.IsRefunded = true;
            payment.RefundedAmount = request.Amount ?? payment.Amount;
            payment.RefundedAt = DateTime.UtcNow;
            payment.Status = request.Amount < payment.Amount 
                ? PaymentStatus.PartialRefund 
                : PaymentStatus.Refunded;

            await _repository.UpdateAsync(payment);
        }

        return new RefundResult(response.Success, response.ErrorMessage);
    }
}
```

### 5. Saga Payment Handler (Azure Function)

```csharp
// Functions/SagaPaymentFunction.cs

public class SagaPaymentFunction
{
    private readonly IPaymentService _paymentService;
    private readonly ServiceBusSender _replySender;
    private readonly ILogger<SagaPaymentFunction> _logger;

    public SagaPaymentFunction(
        IPaymentService paymentService,
        ServiceBusClient serviceBusClient,
        ILogger<SagaPaymentFunction> logger)
    {
        _paymentService = paymentService;
        _replySender = serviceBusClient.CreateSender("saga-reply-queue");
        _logger = logger;
    }

    /// <summary>
    /// Saga'dan gelen ödeme işleme komutu
    /// </summary>
    [Function("HandleProcessPayment")]
    public async Task HandleProcessPayment(
        [ServiceBusTrigger("process-payment-queue")] ProcessPaymentCommand command)
    {
        _logger.LogInformation("Processing payment for saga: {SagaId}", command.SagaId);

        PaymentProcessedReply reply;

        try
        {
            var result = await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
            {
                OrderId = command.OrderId,
                CustomerId = command.CustomerId,
                Amount = command.Amount,
                Currency = command.Currency,
                Method = command.PaymentMethod,
                Provider = "Dummy", // Test için
                CardNumber = command.CardNumber,
                ExpiryMonth = command.ExpiryMonth,
                ExpiryYear = command.ExpiryYear,
                Cvv = command.Cvv,
                CardHolderName = command.CardHolderName,
                CustomerEmail = command.CustomerEmail,
                CustomerIp = command.CustomerIp
            });

            reply = new PaymentProcessedReply(
                SagaId: command.SagaId,
                Success: result.Success,
                PaymentId: result.PaymentId,
                TransactionId: result.TransactionId,
                Error: result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment processing failed for saga: {SagaId}", command.SagaId);

            reply = new PaymentProcessedReply(
                SagaId: command.SagaId,
                Success: false,
                PaymentId: null,
                TransactionId: null,
                Error: ex.Message);
        }

        await SendReplyAsync(reply);
    }

    /// <summary>
    /// Saga compensation - İade işlemi
    /// </summary>
    [Function("HandleRefundPayment")]
    public async Task HandleRefundPayment(
        [ServiceBusTrigger("refund-payment-queue")] RefundPaymentCommand command)
    {
        _logger.LogInformation("Processing refund for saga: {SagaId}", command.SagaId);

        try
        {
            var result = await _paymentService.RefundPaymentAsync(new RefundPaymentRequest
            {
                PaymentId = command.PaymentId,
                Amount = command.Amount,
                Reason = command.Reason ?? "Saga compensation"
            });

            if (result.Success)
            {
                _logger.LogInformation("Refund completed for saga: {SagaId}", command.SagaId);
            }
            else
            {
                _logger.LogWarning("Refund failed for saga: {SagaId}, Error: {Error}", 
                    command.SagaId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refund error for saga: {SagaId}", command.SagaId);
        }
    }

    private async Task SendReplyAsync<T>(T reply)
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(reply))
        {
            ContentType = "application/json"
        };
        message.ApplicationProperties["MessageType"] = typeof(T).Name;

        await _replySender.SendMessageAsync(message);
    }
}
```

### 6. HTTP API Function

```csharp
// Functions/PaymentApiFunction.cs

public class PaymentApiFunction
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRepository _repository;
    private readonly ILogger<PaymentApiFunction> _logger;

    public PaymentApiFunction(
        IPaymentService paymentService,
        IPaymentRepository repository,
        ILogger<PaymentApiFunction> logger)
    {
        _paymentService = paymentService;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Ödeme başlat
    /// POST /api/payments
    /// </summary>
    [Function("ProcessPayment")]
    public async Task<IActionResult> ProcessPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "payments")] 
        HttpRequest req)
    {
        var request = await JsonSerializer.DeserializeAsync<ProcessPaymentRequest>(req.Body);

        var result = await _paymentService.ProcessPaymentAsync(request);

        if (result.Success)
        {
            return new OkObjectResult(new
            {
                result.Success,
                result.PaymentId,
                result.TransactionId,
                Status = result.Status.ToString()
            });
        }

        return new BadRequestObjectResult(new
        {
            result.Success,
            result.ErrorCode,
            result.ErrorMessage
        });
    }

    /// <summary>
    /// Ödeme durumu sorgula
    /// GET /api/payments/{id}
    /// </summary>
    [Function("GetPayment")]
    public async Task<IActionResult> GetPayment(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payments/{id}")] 
        HttpRequest req,
        Guid id)
    {
        var payment = await _repository.GetByIdAsync(id);

        if (payment == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(new
        {
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            Status = payment.Status.ToString(),
            Method = payment.Method.ToString(),
            payment.Provider,
            payment.CreatedAt,
            payment.ProcessedAt
        });
    }

    /// <summary>
    /// Sipariş ödemelerini listele
    /// GET /api/payments/order/{orderId}
    /// </summary>
    [Function("GetPaymentsByOrder")]
    public async Task<IActionResult> GetPaymentsByOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payments/order/{orderId}")] 
        HttpRequest req,
        Guid orderId)
    {
        var payments = await _repository.GetByOrderIdAsync(orderId);

        return new OkObjectResult(payments.Select(p => new
        {
            p.Id,
            p.Amount,
            Status = p.Status.ToString(),
            p.CreatedAt
        }));
    }

    /// <summary>
    /// İade işlemi
    /// POST /api/payments/{id}/refund
    /// </summary>
    [Function("RefundPayment")]
    public async Task<IActionResult> RefundPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "payments/{id}/refund")] 
        HttpRequest req,
        Guid id)
    {
        var request = await JsonSerializer.DeserializeAsync<RefundPaymentRequest>(req.Body);
        request.PaymentId = id;

        var result = await _paymentService.RefundPaymentAsync(request);

        if (result.Success)
        {
            return new OkObjectResult(new { Success = true, Message = "Refund processed" });
        }

        return new BadRequestObjectResult(new { Success = false, Error = result.ErrorMessage });
    }
}
```

### 7. Program.cs (DI Yapılandırması)

```csharp
// Program.cs

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentServices.Configuration;
using PaymentServices.Repositories;
using PaymentServices.Services;
using PaymentServices.Services.Providers;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configuration
        services.Configure<PaymentConfiguration>(
            configuration.GetSection(PaymentConfiguration.SectionName));

        // CosmosDB
        var cosmosConfig = configuration.GetSection("CosmosDb");
        var cosmosClient = new CosmosClient(
            cosmosConfig["ConnectionString"],
            new CosmosClientOptions
            {
                HttpClientFactory = () => new HttpClient(
                    new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = 
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }),
                ConnectionMode = ConnectionMode.Gateway
            });

        services.AddSingleton(cosmosClient);
        services.AddScoped<IPaymentRepository>(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            var container = client.GetContainer(
                cosmosConfig["DatabaseName"],
                cosmosConfig["ContainerName"]);
            return new CosmosPaymentRepository(container);
        });

        // Azure Service Bus
        var serviceBusConnectionString = configuration["ServiceBusConnection"];
        services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));

        // Payment Providers
        services.AddScoped<IPaymentProvider, DummyPaymentProvider>();
        // services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        // services.AddScoped<IPaymentProvider, IyzicoPaymentProvider>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();

        // Services
        services.AddScoped<IPaymentService, PaymentService>();
    })
    .Build();

host.Run();
```

---

## Saga Entegrasyonu

### Saga State Machine'deki Payment Adımı

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                        SAGA'DA PAYMENT ADIMI                                     │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ┌────────────┐                                                                │
│   │ Reserving  │                                                                │
│   │   Stock    │                                                                │
│   └─────┬──────┘                                                                │
│         │                                                                        │
│         │ StockReserved ✓                                                       │
│         ▼                                                                        │
│   ╔═════════════════════════════════════════════════════════════════════════╗   │
│   ║                                                                         ║   │
│   ║               PROCESSING PAYMENT                                        ║   │
│   ║                                                                         ║   │
│   ║   Saga Orchestrator                    Payment Service                  ║   │
│   ║         │                                    │                          ║   │
│   ║         │─── ProcessPaymentCommand ─────────▶│                          ║   │
│   ║         │                                    │                          ║   │
│   ║         │                                    ▼                          ║   │
│   ║         │                           ┌──────────────┐                    ║   │
│   ║         │                           │   Provider   │                    ║   │
│   ║         │                           │  (Stripe/    │                    ║   │
│   ║         │                           │  Iyzico/     │                    ║   │
│   ║         │                           │  Dummy)      │                    ║   │
│   ║         │                           └──────────────┘                    ║   │
│   ║         │                                    │                          ║   │
│   ║         │◀── PaymentProcessedReply ──────────│                          ║   │
│   ║         │                                    │                          ║   │
│   ╚═════════╪════════════════════════════════════╪══════════════════════════╝   │
│             │                                    │                              │
│    ┌────────┴────────┐                          │                              │
│    │                 │                          │                              │
│    ▼                 ▼                          │                              │
│  Success          Failed                        │                              │
│    │                 │                          │                              │
│    ▼                 ▼                          ▼                              │
│ ┌──────┐      ┌────────────┐            ┌────────────┐                         │
│ │      │      │            │            │            │                         │
│ │ DONE │      │COMPENSATING│◀───────────│RefundPayment                         │
│ │  ✓   │      │            │            │ Command   │                         │
│ │      │      │ C3: Stock  │            │            │                         │
│ └──────┘      │ C2: Order  │            └────────────┘                         │
│               │            │                                                    │
│               └────────────┘                                                    │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Message Contracts

```csharp
// Saga → Payment Service
public record ProcessPaymentCommand(
    Guid SagaId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv,
    string CardHolderName,
    string CustomerEmail,
    string CustomerIp,
    string ReplyTo);

// Payment Service → Saga
public record PaymentProcessedReply(
    Guid SagaId,
    bool Success,
    Guid? PaymentId,
    string TransactionId,
    string Error);

// Compensation: Saga → Payment Service
public record RefundPaymentCommand(
    Guid SagaId,
    Guid PaymentId,
    decimal Amount,
    string Reason);
```

---

## Docker Yapılandırması

### docker-compose.yml Eklentisi

```yaml
  # ═══════════════════════════════════════════════════
  # PAYMENT SERVICE
  # ═══════════════════════════════════════════════════
  
  payment-db:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    container_name: payment-db
    ports:
      - "8083:8081"
      - "11256:10254"
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true
    volumes:
      - payment_db_data:/data/db
    networks:
      - saga-network

volumes:
  payment_db_data:
```

### appsettings.json

```json
{
  "CosmosDb": {
    "ConnectionString": "AccountEndpoint=https://localhost:8083/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "DatabaseName": "paymentdb",
    "ContainerName": "payments"
  },
  "ServiceBusConnection": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY",
  "Payment": {
    "DefaultProvider": "Dummy",
    "Providers": {
      "Stripe": {
        "ApiKey": "sk_test_..."
      },
      "Iyzico": {
        "ApiKey": "...",
        "SecretKey": "...",
        "BaseUrl": "https://sandbox-api.iyzipay.com"
      }
    }
  }
}
```

---

## Test ve Doğrulama

### Test Senaryoları

```http
### 1. Başarılı Ödeme
POST http://localhost:7073/api/payments
Content-Type: application/json

{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "550e8400-e29b-41d4-a716-446655440001",
  "amount": 250.00,
  "currency": "TRY",
  "method": "CreditCard",
  "cardNumber": "4111111111111111",
  "expiryMonth": "12",
  "expiryYear": "2025",
  "cvv": "123",
  "cardHolderName": "Ali Yılmaz",
  "customerEmail": "ali@example.com",
  "customerIp": "192.168.1.1"
}

### Beklenen Sonuç:
{
  "success": true,
  "paymentId": "...",
  "transactionId": "DUMMY-...",
  "status": "Completed"
}


### 2. Başarısız Ödeme (Yetersiz Bakiye)
POST http://localhost:7073/api/payments
Content-Type: application/json

{
  "orderId": "...",
  "customerId": "...",
  "amount": 250.00,
  "cardNumber": "4111111111110000",  # 0000 ile biten = fail
  ...
}

### Beklenen Sonuç:
{
  "success": false,
  "errorCode": "INSUFFICIENT_FUNDS",
  "errorMessage": "Yetersiz bakiye"
}


### 3. İade İşlemi
POST http://localhost:7073/api/payments/{paymentId}/refund
Content-Type: application/json

{
  "amount": 250.00,
  "reason": "Müşteri talebi"
}
```

---

## Özet

| Karar | Seçim | Gerekçe |
|-------|-------|---------|
| **Mimari** | Azure Functions | Saga entegrasyonu, event-driven, tutarlı mimari |
| **Veritabanı** | CosmosDB | Mevcut Order servisindeki pattern |
| **Provider** | Dummy (Test) | Geliştirme ortamı için |
| **Queue** | Azure Service Bus | Saga orchestration için |

### Sonraki Adımlar

1. ✅ PaymentServices projesi oluşturma
2. ✅ Saga Orchestrator'a payment adımı ekleme
3. ✅ Gerçek ödeme sağlayıcı entegrasyonu (Stripe/Iyzico)
4. ✅ Webhook handler implementasyonu
5. ✅ PCI DSS uyumluluk düzenlemeleri
