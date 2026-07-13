# Payment Financial

Microserviço de pagamentos do ecossistema zenyFin. Processa upgrades de plano (Pro/Enterprise) com suporte a Cartão de Crédito, PIX e Boleto via gateway falso (Bogus), utilizando CQRS + Vertical Slices.

## Funcionalidades

- Processamento de pagamentos (Cartão, PIX, Boleto) via gateway fake com Bogus
- Arquitetura CQRS + Vertical Slices com MediatR
- Mensageria assíncrona com RabbitMQ (event-driven)
- Validação JWT rigorosa (HS256, issuer, audience, clock skew zero)
- Rate Limiting, Idempotência e CORS configurados
- Validação de Origin/Referer em middleware de segurança
- Limite de payload de 1MB em todas as controllers
- State machine com guardas de transição de estado no domínio
- Logs estruturados com Serilog (sem dados sensíveis)
- Retry com Polly e Dead Letter Queue
- Health checks profundos (PostgreSQL + RabbitMQ)
- List Payments com paginação e filtro por status
- Refund Flow (reembolso de pagamentos)
- Unit of Work com transações automáticas
- Correlation ID para rastreabilidade de requisições
- Outbox Pattern para publicação confiável de eventos
- Domain Events (padrão DDD)
- InMemory Caching para queries frequentes
- Per-user Rate Limiting (Token Bucket partitioned by userId)
- Worker Service com consumer RabbitMQ

## Tech Stack

| Camada | Tecnologia |
|--------|-----------|
| Runtime | .NET 8 |
| Framework | ASP.NET Core 8 |
| Linguagem | C# 12 |
| ORM | EF Core 8 + PostgreSQL |
| CQRS | MediatR 12 |
| Validação | FluentValidation 11 |
| Mensageria | RabbitMQ.Client 6 |
| Gateway Fake | Bogus 35 |
| JWT | System.IdentityModel.Tokens.Jwt 7 |
| Logs | Serilog 4 + Seq |
| Retry | Polly 8 |
| Cache | Microsoft.Extensions.Caching.Memory |
| Health Checks | Microsoft.Extensions.Diagnostics.HealthChecks |
| Testes | xUnit + Moq + FluentAssertions |
| Documentação | Swagger / Swashbuckle 6 |

## Estrutura do Projeto

```
payment-financial/
├── Payment.Api.sln
├── src/
│   ├── Payment.Api/              # API + DI + Middleware
│   │   └── Api/
│   │       ├── Controllers/      # Payments, Health
│   │       └── Middleware/       # ExceptionHandling, OriginValidation, JwtUser, CorrelationId
│   ├── Payment.Domain/           # Entidades, Value Objects, Events
│   │   └── Entities/             # Payment, PaymentLog, Money, OutboxMessage
│   ├── Payment.Application/      # CQRS + MediatR + FluentValidation
│   │   └── Features/Payments/
│   │       ├── Commands/         # ProcessPayment, CancelPayment, RefundPayment
│   │       ├── Queries/          # GetPayment, ListPayments
│   │       └── Events/           # PaymentCompletedDomainEvent
│   ├── Payment.Infrastructure/   # EF Core, RabbitMQ, JWT, Gateway, Health Checks
│   │   ├── Caching/              # InMemoryCacheService
│   │   └── HealthChecks/         # PostgresHealthCheck, RabbitMqHealthCheck
│   └── Payment.Worker/           # Background Service
│       └── Consumers/            # PaymentCompletedConsumer, OutboxProcessor
├── tests/
│   ├── Payment.UnitTests/        # Testes unitários (xUnit)
│   └── Payment.IntegrationTests/ # Testes de integração (xUnit)
└── docs/
    └── integracao-frontend-backend.md
```

## Começando

### Pré-requisitos

- .NET SDK 8.0
- PostgreSQL 16+
- RabbitMQ 3.x (ou Docker)

### Configuração rápida

```bash
# Clone
git clone https://github.com/RomuloSergioRE/payment-financial.git
cd payment-financial

# Configure as variáveis de ambiente
cp .env.example .env
# Edite o .env com suas credenciais

# Execute via Docker para dependências
docker run -d --name postgres-payment \
  -e POSTGRES_DB=payment_financial \
  -e POSTGRES_PASSWORD=minha-senha \
  -p 5432:5432 postgres:16

docker run -d --name rabbitmq-payment \
  -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Aplique as migrations
dotnet ef database update -p src/Payment.Infrastructure -s src/Payment.Api

# Execute o projeto
dotnet run --project src/Payment.Api
```

A API estará disponível em `http://localhost:5001`.  
Swagger: `http://localhost:5001/swagger`

### Variáveis de Ambiente (.env)

```bash
# Database
DB_HOST=localhost
DB_PORT=5432
DB_USER=postgres
DB_PASS=minha-senha
DB_NAME=payment_financial

# JWT (mesmo secret do api-financial)
JWT_SECRET=chave-secreta-forte-com-64-bytes-hex
JWT_ISSUER=zenyfin-api
JWT_AUDIENCE=zenyfin-payment

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USER=guest
RABBITMQ_PASS=guest

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5001
```

## Endpoints da API

| Método | Rota | Descrição | Autenticação | Rate Limit |
|--------|------|-----------|-------------|------------|
| GET | `/health` | Health check profundo | Não | — |
| GET | `/health/live` | Liveness probe | Não | — |
| POST | `/api/payments` | Processar pagamento | JWT Bearer | 20 tokens/min |
| GET | `/api/payments` | Listar pagamentos | JWT Bearer | 20 tokens/min |
| GET | `/api/payments/{id}` | Consultar pagamento | JWT Bearer | 20 tokens/min |
| DELETE | `/api/payments/{id}` | Cancelar pagamento | JWT Bearer | 20 tokens/min |
| POST | `/api/payments/{id}/refund` | Reembolsar pagamento | JWT Bearer | 20 tokens/min |

### Exemplo de requisição (cartão de crédito)

```bash
curl -X POST http://localhost:5001/api/payments \
  -H "Authorization: Bearer <token>" \
  -H "Idempotency-Key: payment_user_123_1710000000" \
  -H "Content-Type: application/json" \
  -d '{
    "planType": "pro",
    "amount": 29.90,
    "currency": "BRL",
    "paymentMethod": "credit_card",
    "cardNumber": "4111111111111111",
    "cardCvv": "123",
    "cardExpiryMonth": 12,
    "cardExpiryYear": 2028,
    "cardHolderName": "João Silva"
  }'
```

### Exemplo de requisição (PIX)

```bash
curl -X POST http://localhost:5001/api/payments \
  -H "Authorization: Bearer <token>" \
  -H "Idempotency-Key: payment_user_123_1710000001" \
  -H "Content-Type: application/json" \
  -d '{
    "planType": "pro",
    "amount": 29.90,
    "currency": "BRL",
    "paymentMethod": "pix"
  }'
```

### Exemplo de requisição (listar pagamentos)

```bash
# Listar todos os pagamentos
curl http://localhost:5001/api/payments \
  -H "Authorization: Bearer <token>"

# Filtrar por status
curl "http://localhost:5001/api/payments?status=completed" \
  -H "Authorization: Bearer <token>"

# Com paginação
curl "http://localhost:5001/api/payments?page=1&pageSize=10" \
  -H "Authorization: Bearer <token>"
```

### Exemplo de requisição (cancelamento)

```bash
curl -X DELETE http://localhost:5001/api/payments/<payment-id> \
  -H "Authorization: Bearer <token>"
```

### Exemplo de requisição (reembolso)

```bash
curl -X POST http://localhost:5001/api/payments/<payment-id>/refund \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Cliente solicitou cancelamento"
  }'
```

## Cartões de Teste

| Número | Bandeira | CVV | Validade | Resultado |
|--------|----------|-----|----------|-----------|
| `4111 1111 1111 1111` | Visa | `123` | 12/2028 | Aprovado (90%) |
| `5555 5555 5555 4444` | Mastercard | `123` | 12/2028 | Aprovado (90%) |
| `3782 822463 10005` | Amex | `1234` | 12/2028 | Aprovado (90%) |
| `4000 0000 0000 0002` | Qualquer | `123` | 12/2028 | Declinado (Luhn inválido) |

## Pipelines Configurados (Program.cs)

- Serilog (bootstrap + request logging)
- ExceptionHandlingMiddleware (400/404/409/422/500)
- OriginValidationMiddleware (validação Origin/Referer)
- CorrelationIdMiddleware (rastreabilidade via X-Correlation-Id)
- Swagger (apenas em desenvolvimento)
- CORS (origem do frontend)
- Rate Limiting por usuário (Token Bucket: 20 tokens/min)
- JWT Authentication (HS256, issuer, audience, clock skew zero)
- JwtUserMiddleware (extração de UserId do token)
- MediatR pipeline: LoggingBehaviour → ValidationBehaviour → PerformanceBehaviour → TransactionBehaviour → OutboxBehavior → DomainEventDispatcherBehavior → CachingBehavior
- Limite de payload: 1MB

### Pipelines MediatR (Ordem de Execução)

1. **LoggingBehaviour** — Log de início/fim de cada request
2. **ValidationBehaviour** — Validação via FluentValidation
3. **PerformanceBehaviour** — Alerta se request demorar >500ms
4. **TransactionBehaviour** — Unit of Work com transação automática
5. **OutboxBehavior** — Grava eventos no Outbox para publicação confiável
6. **DomainEventDispatcherBehavior** — Despacha Domain Events do aggregate
7. **CachingBehavior** — Cache InMemory para queries (30s TTL)

## Segurança

- JWT validado com algoritmo explícito (apenas HS256)
- Issuer e Audience lidos da configuração
- ClockSkew = 0 (sem tolerância)
- Rate limiting por usuário (Token Bucket: 20 tokens/min, partitioned by userId)
- CORS restrito ao frontend (origens configuráveis)
- Validação de Origin/Referer em requisições POST/PUT/DELETE
- Limite de payload de 1MB
- Idempotency-Key obrigatório (único no banco de dados)
- State machine com guardas (transições inválidas lançam exceção)
- Logs sem dados sensíveis (nunca loga card number, CVV)
- Erros 500 genéricos em produção (sem detalhes)
- Conexão PostgreSQL com SSL (sslmode=Require)
- Pool de conexão limitado (max 10)
- Correlation ID em todas as requisições para rastreabilidade
- Outbox Pattern para garantir publicação confiável de eventos

## Testes

O projeto possui **84 testes** cobrindo todas as camadas:

| Categoria | Testes | Cobertura |
|-----------|--------|-----------|
| Domain (Money, Payment, PaymentLog) | 18 | Entidades, value objects, state machine |
| Validators (ProcessPayment, CancelPayment, RefundPayment) | 21 | Regras de validação FluentValidation |
| Handlers (Process, Cancel, Get, List, Refund) | 14 | CQRS handlers com mocks |
| Infrastructure (FakePaymentGateway) | 6 | Luhn, expiry, PIX, Boleto |
| JwtValidator | 7 | Token válido, expirado, assinatura, claims |
| ExceptionHandlingMiddleware | 6 | Mapeamento 400/404/409/422/500 |
| Integration | 2 | Fluxo completo com InMemory DB |
| Caching | 2 | Cache hit/miss |
| Health Checks | 2 | PostgreSQL + RabbitMQ |

```bash
# Executar todos os testes
dotnet test

# Executar apenas testes unitários
dotnet test tests/Payment.UnitTests

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Arquitetura Aplicada

### Fluxo de uma Requisição

```
HTTP Request
    ↓
CorrelationIdMiddleware (gera X-Correlation-Id)
    ↓
ExceptionHandlingMiddleware (catch global)
    ↓
OriginValidationMiddleware (valida Origin/Referer)
    ↓
JwtUserMiddleware (extrai userId do token)
    ↓
Rate Limiting (Token Bucket por userId)
    ↓
PaymentsController (valida Idempotency-Key)
    ↓
MediatR Pipeline:
    1. LoggingBehaviour
    2. ValidationBehaviour (FluentValidation)
    3. PerformanceBehaviour (>500ms alert)
    4. TransactionBehaviour (Unit of Work)
    5. OutboxBehavior (publicação confiável)
    6. DomainEventDispatcherBehavior
    7. CachingBehavior (InMemory)
    ↓
Handler (Command/Query)
    ↓
Domain Entity + State Machine
    ↓
FakePaymentGateway (simula processamento)
    ↓
RabbitMQ (publica evento, com Polly retry + fallback NullMessageBus)
```

### Worker Service

O Worker consome eventos do RabbitMQ de forma assíncrona:

- **PaymentCompletedConsumer** — Processa eventos `PaymentCompleted` e atualiza status
- **OutboxProcessor** — Background service que processa mensagens do Outbox e publica no RabbitMQ

```bash
# Executar o Worker
dotnet run --project src/Payment.Worker
```

## Integração

Consulte [docs/integracao-frontend-backend.md](docs/integracao-frontend-backend.md) para instruções detalhadas de integração com:

- **Frontend Next.js** — service de pagamento, tipos, rotas de checkout
- **api-financial (Node.js)** — consumer RabbitMQ, atualização de plano, audit log

## Status do Projeto

**Todas as fases completas** — Payment Microservice pronto para produção.

- [x] Fase 1: Scaffolding
- [x] Fase 2: Domain
- [x] Fase 3: Application (CQRS)
- [x] Fase 4: Infrastructure
- [x] Fase 5: API
- [x] Fase 8: Testes
- [x] Funcionalidades do Roadmap: Health Checks, ListPayments, Unit of Work, Correlation ID, Refund Flow, Outbox Pattern, Domain Events, Caching, Per-user Rate Limiting, Worker Service

## Licença

MIT
