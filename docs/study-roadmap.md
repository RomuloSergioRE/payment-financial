# Study Roadmap — Top 10 Features to Implement

Guia de estudo com as 10 features mais impactantes para implementar neste projeto. Cada uma ensina um padrão arquitetural importante e fortalece o codebase.

---

## 1. Outbox Pattern

**Dificuldade:** Intermediário | **Esforço:** 2-3 dias | **Prioridade:** CRÍTICA

### O que ensina

Resolve o problema de **dual-write** — hoje o handler salva no banco e depois publica no RabbitMQ em passos separados. Se o `SaveChangesAsync` falhar após o `PublishAsync`, consumidores recebem eventos de pagamentos que nunca existiram. O Outbox garante atomicidade: eventos são salvos na mesma transação dos dados de negócio, depois publicados por um processador em background.

### Arquivo principal

```
src/Payment.Infrastructure/Persistence/OutboxMessageConfiguration.cs  (novo)
src/Payment.Application/Common/Behaviours/OutboxBehavior.cs           (novo)
src/Payment.Worker/Consumers/OutboxProcessor.cs                       (novo)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Domain | `Entities/OutboxMessage.cs` (novo) | Entidade: Id, EventType, Payload, CreatedAt, ProcessedAt |
| Infrastructure | `PaymentConfiguration.cs` | Adicionar tabela `outbox_messages` com índice em `ProcessedAt` |
| Application | `DependencyInjection.cs` | Registrar `OutboxBehavior` como pipeline behavior |
| Application | `Behaviours/OutboxBehavior.cs` (novo) | IPipelineBehavior que salva eventos na tabela outbox |
| Worker | `OutboxProcessor.cs` (novo) | BackgroundService que publica eventos não processados |
| API | `Program.cs` | Registrar Worker no host |

### Padrão de código

```csharp
// OutboxBehavior.cs
public class OutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly PaymentDbContext _context;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next(); // Executa o handler

        // Salva dados + eventos na mesma transação
        var outboxMessages = _context.ChangeTracker
            .Entries<DomainEvent>()
            .Select(e => new OutboxMessage
            {
                EventType = e.Entity.GetType().Name,
                Payload = JsonSerializer.Serialize(e.Entity),
                CreatedAt = DateTime.UtcNow
            });

        _context.OutboxMessages.AddRange(outboxMessages);
        await _context.SaveChangesAsync(ct);

        return response;
    }
}
```

---

## 2. Worker Service — Event Consumer

**Dificuldade:** Intermediário | **Esforço:** 1-2 dias | **Prioridade:** ALTA

### O que ensina

O Worker está **completamente vazio** (`Console.WriteLine("Hello, World!")`). Implementar um consumidor de eventos RabbitMQ torna a arquitetura funcional de ponta a ponta: Payment MS publica `payment.completed`, o Worker consome e processa.

### Arquivo principal

```
src/Payment.Worker/Program.cs                              (reescrever)
src/Payment.Worker/Consumers/PaymentCompletedConsumer.cs   (novo)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Worker | `Program.cs` | Criar Host com DI, registrar serviços do Infrastructure |
| Worker | `PaymentCompletedConsumer.cs` (novo) | BackgroundService que consome fila `payment.completed.queue` |
| Worker | `Payment.Worker.csproj` | Adicionar referência ao Application + packages necessários |
| Infrastructure | `DependencyInjection.cs` | Já registra RabbitMqBus; Worker precisa do mesmo |

### Padrão de código

```csharp
public class PaymentCompletedConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<PaymentCompletedConsumer> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare("payment.events", ExchangeType.Topic, durable: true);
        channel.QueueDeclare("payment.completed.queue", durable: true, exclusive: false);
        channel.QueueBind("payment.completed.queue", "payment.events", "payment.completed");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(json);
            _logger.LogInformation("Received payment completed: {PaymentId}", evt.Data.PaymentId);
            // Processar: atualizar plano, criar audit log, etc.
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume("payment.completed.queue", autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

---

## 3. Payment Refund Flow

**Dificuldade:** Intermediário | **Esforço:** 2-3 dias | **Prioridade:** ALTA

### O que ensina

O domínio já tem `MarkRefunded()` e `RefundedAt`, mas **não existe nenhum command, handler, controller ou gateway** para acionar um reembolso. Implementar o fluxo completo ensina o ciclo de vida de uma operação financeira: command → validação → gateway → estado → evento → audit.

### Arquivo principal

```
src/Payment.Application/Features/Payments/Commands/RefundPayment/  (novo)
src/Payment.Api/Api/Controllers/PaymentsController.cs              (adicionar endpoint)
src/Payment.Application/Common/Interfaces/IPaymentGateway.cs       (adicionar método)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Application | `RefundPaymentCommand.cs` (novo) | Record: PaymentId, UserId, Reason |
| Application | `RefundPaymentCommandHandler.cs` (novo) | Validar estado → gateway → MarkRefunded → evento → log |
| Application | `RefundPaymentCommandValidator.cs` (novo) | PaymentId/UserId NotEmpty |
| Application | `Interfaces/IPaymentGateway.cs` | Adicionar `Task<PaymentResult> RefundAsync(...)` |
| Infrastructure | `FakePaymentGateway.cs` | Implementar `RefundAsync` (sempre sucesso, simula delay) |
| API | `PaymentsController.cs` | `POST /api/payments/{id}/refund` com `[EnableRateLimiting("Strict")]` |
| API | `ExceptionHandlingMiddleware.cs` | Já trata `PaymentException` → 422 |

### Fluxo de estado

```
Completed → Refunded (MarkRefunded)
  ↓
Gateway RefundAsync (fake: sempre sucesso)
  ↓
PaymentLog("payment.refunded")
  ↓
PaymentRefundedEvent publicado
```

---

## 4. Deep Health Checks

**Dificuldade:** Iniciante | **Esforço:** Meio dia | **Prioridade:** ALTA

### O que ensina

O `/health` atual retorna `"healthy"` hardcoded — não verifica nada real. Adicionar health checks de PostgreSQL e RabbitMQ ensina o padrão `/health/ready` (dependências prontas) vs `/health/live` (processo vivo), essencial para Kubernetes e monitoramento.

### Arquivo principal

```
src/Payment.Api/Api/Controllers/HealthController.cs    (modificar)
src/Payment.Infrastructure/HealthChecks/               (novo)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Infrastructure | `HealthChecks/PostgresHealthCheck.cs` (novo) | Conectar ao DB e executar `SELECT 1` |
| Infrastructure | `HealthChecks/RabbitMqHealthCheck.cs` (novo) | Verificar `IConnection.IsOpen` |
| API | `Program.cs` | `services.AddHealthChecks().AddNpgsql().AddRabbitMQ()` |
| API | `HealthController.cs` | Usar `IHealthCheckService` para retornar status real |

### Padrão de resposta

```json
// GET /health/live
{ "status": "healthy", "service": "payment-financial" }

// GET /health/ready
{
  "status": "healthy",
  "checks": {
    "postgresql": { "status": "healthy", "duration": "2ms" },
    "rabbitmq": { "status": "healthy", "duration": "1ms" }
  }
}
```

---

## 5. ListPaymentsQuery com Paginação

**Dificuldade:** Iniciante | **Esforço:** 1 dia | **Prioridade:** ALTA

### O que ensina

Não existe nenhuma forma de listar o histórico de pagamentos do usuário. Esta é a operação CRUD mais óbvia que falta. Ensina paginação (offset/limit), query design no CQRS read side, e responses paginadas.

### Arquivo principal

```
src/Payment.Application/Features/Payments/Queries/ListPayments/   (novo)
src/Payment.Api/Api/Controllers/PaymentsController.cs             (adicionar endpoint)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Application | `ListPaymentsQuery.cs` (novo) | Query: UserId, Page, PageSize, Status? |
| Application | `ListPaymentsQueryHandler.cs` (novo) | EF Core query com filtro, paginação, ToPagedListAsync |
| Application | `ListPaymentsResponse.cs` (novo) | PagedResult<List<PaymentSummary>> |
| API | `PaymentsController.cs` | `GET /api/payments?page=1&pageSize=10&status=completed` |

### Response esperado

```json
{
  "items": [
    { "paymentId": "...", "status": "completed", "amount": 29.90, "currency": "BRL", "method": "credit_card", "createdAt": "2026-07-10T12:00:00Z" }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 25,
  "totalPages": 3
}
```

---

## 6. Unit of Work Pipeline Behavior

**Dificuldade:** Iniciante | **Esforço:** Meio dia | **Prioridade:** MÉDIA

### O que ensina

Hoje cada handler chama `SaveChangesAsync` manualmente — se o handler esquecer, dados ficam inconsistentes. Um pipeline behavior que wrappeia a execução em uma transação ensina o padrão Unit of Work e por que transações automatizadas são mais seguras que chamadas manuais.

### Arquivo principal

```
src/Payment.Application/Common/Behaviours/TransactionBehaviour.cs  (novo)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Application | `Behaviours/TransactionBehaviour.cs` (novo) | IPipelineBehavior que abre transação |
| Application | `DependencyInjection.cs` | Registrar behavior (deve ser o mais externo) |

### Padrão de código

```csharp
public class TransactionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly PaymentDbContext _context;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var response = await next();
            await transaction.CommitAsync(ct);
            return response;
        });
    }
}
```

---

## 7. Correlation ID Middleware

**Dificuldade:** Iniciante | **Esforço:** Meio dia | **Prioridade:** MÉDIA

### O que ensina

Não existe rastreabilidade de requisições. Um middleware que gera/extrai `X-Request-ID` e propaga via `LogContext` ensina tracing distribuído, logging estruturado com correlação, e o conceito de traces em microsserviços.

### Arquivo principal

```
src/Payment.Api/Api/Middleware/CorrelationIdMiddleware.cs  (novo)
src/Payment.Application/Common/Behaviours/LoggingBehaviour.cs  (modificar)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| API | `Middleware/CorrelationIdMiddleware.cs` (novo) | Gerar/ler X-Request-ID, adicionar ao Response |
| API | `Program.cs` | Registrar middleware (antes de ExceptionHandling) |
| Application | `LoggingBehaviour.cs` | Usar `LogContext.PushProperty("CorrelationId", ...)` |

### Padrão de código

```csharp
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Request-ID"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

---

## 8. Domain Events (implementação correta)

**Dificuldade:** Intermediário | **Esforço:** 1-2 dias | **Prioridade:** MÉDIA

### O que ensina

O `IDomainEvent` foi removido, e eventos são publicados manualmente no handler. Implementar o padrão DDD corretamente: interface `IDomainEvent` com `OccurredOn`, agregado com `List<IDomainEvent>`, e dispatcher que coleta e publica após `SaveChanges`.

### Arquivo principal

```
src/Payment.Domain/Events/                         (novo)
src/Payment.Domain/Entities/Payment.cs             (modificar)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Domain | `Events/IDomainEvent.cs` (novo) | Interface: `DateTime OccurredOn` |
| Domain | `Events/PaymentCompletedDomainEvent.cs` (novo) | Evento de domínio |
| Domain | `Events/PaymentRefundedDomainEvent.cs` (novo) | Evento de domínio |
| Domain | `Entities/Payment.cs` | Property `IReadOnlyList<IDomainEvent> DomainEvents` + método `AddDomainEvent()` |
| Application | `Behaviours/DomainEventDispatcherBehavior.cs` (novo) | Coleta eventos do agregado e publica via IMessageBus |
| Application | `ProcessPaymentCommandHandler.cs` | Usar `payment.AddDomainEvent(new PaymentCompletedDomainEvent(...))` em vez de `_bus.PublishAsync` |

### Padrão de código

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

public sealed class PaymentCompletedDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public string PlanType { get; init; }
}
```

---

## 9. Caching Layer

**Dificuldade:** Intermediário | **Esforço:** 1-2 dias | **Prioridade:** MÉDIA

### O que ensina

Não existe cache. `GetPlanPricesQueryHandler` retorna dados hardcoded a cada chamada. Um `ICacheService` com implementação in-memory ou Redis ensina cache-aside pattern, invalidação de cache, e otimização de performance.

### Arquivo principal

```
src/Payment.Application/Common/Interfaces/ICacheService.cs         (novo)
src/Payment.Application/Common/Behaviours/CachingBehavior.cs       (novo)
src/Payment.Infrastructure/Caching/InMemoryCacheService.cs         (novo)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| Application | `Interfaces/ICacheService.cs` (novo) | GetAsync, SetAsync, RemoveAsync |
| Application | `Behaviours/CachingBehavior.cs` (novo) | IPipelineBehavior que verifica cache antes de executar |
| Infrastructure | `Caching/InMemoryCacheService.cs` (novo) | Implementação com `IMemoryCache` |
| Infrastructure | `DependencyInjection.cs` | Registrar `ICacheService` |
| API | `Program.cs` | `services.AddMemoryCache()` |

### Exemplo de uso

```csharp
// GetPlanPricesQueryHandler modificado
public async Task<List<GetPlanPricesResponse>> Handle(GetPlanPricesQuery request, CancellationToken ct)
{
    var cached = await _cache.GetAsync<List<GetPlanPricesResponse>>("plan:prices", ct);
    if (cached is not null) return cached;

    var plans = new List<GetPlanPricesResponse> { /* ... */ };
    await _cache.SetAsync("plan:prices", plans, TimeSpan.FromMinutes(30), ct);
    return plans;
}
```

---

## 10. Per-User Rate Limiting

**Dificuldade:** Intermediário | **Esforço:** 1 dia | **Prioridade:** MÉDIA

### O que ensina

O rate limiting atual é por IP (`FixedWindowLimiter`). Trocar para partitionamento por `userId` (autenticado) ensina rate limit strategies, a diferença entre limitação por IP vs por usuário, e como customizar o `PartitionedRateLimiter`.

### Arquivo principal

```
src/Payment.Api/Api/RateLimiting/UserRateLimiter.cs  (novo)
src/Payment.Api/Program.cs                           (modificar)
```

### Onde tocar

| Camada | Arquivo | Mudança |
|--------|---------|---------|
| API | `RateLimiting/UserRateLimiter.cs` (novo) | Custom `IPartitionedRateLimiter<HttpContext>` |
| API | `Program.cs` | Substituir `AddFixedWindowLimiter` por `AddPolicyLimiter` |
| API | `appsettings.json` | Configurar limites por usuário |

### Padrão de código

```csharp
services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddPolicy("UserPayment", httpContext =>
    {
        var userId = httpContext.Items["UserId"]?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});
```

---

## Resumo

| # | Feature | Dificuldade | Esforço | Prioridade |
|---|---------|-------------|---------|------------|
| 1 | Outbox Pattern | Intermediário | 2-3 dias | CRÍTICA |
| 2 | Worker Event Consumer | Intermediário | 1-2 dias | ALTA |
| 3 | Payment Refund Flow | Intermediário | 2-3 dias | ALTA |
| 4 | Deep Health Checks | Iniciante | Meio dia | ALTA |
| 5 | ListPayments com Paginação | Iniciante | 1 dia | ALTA |
| 6 | Unit of Work Pipeline | Iniciante | Meio dia | MÉDIA |
| 7 | Correlation ID Middleware | Iniciante | Meio dia | MÉDIA |
| 8 | Domain Events (correto) | Intermediário | 1-2 dias | MÉDIA |
| 9 | Caching Layer | Intermediário | 1-2 dias | MÉDIA |
| 10 | Per-User Rate Limiting | Intermediário | 1 dia | MÉDIA |

### Ordem recomendada de implementação

1. **Outbox Pattern** → resolve problema real de confiabilidade
2. **Worker Event Consumer** → torna arquitetura funcional
3. **Payment Refund Flow** → completa ciclo de vida do pagamento
4. **Deep Health Checks** → prontidão para produção
5. **ListPayments** → operação CRUD mais óbvia que falta
6. **Unit of Work** → automatiza transações
7. **Correlation ID** → rastreabilidade de requisições
8. **Domain Events** → padrão DDD correto
9. **Caching** → performance
10. **Per-User Rate Limiting** → segurança por usuário
