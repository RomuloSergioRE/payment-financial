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
| Testes | xUnit + Moq + FluentAssertions |
| Documentação | Swagger / Swashbuckle 6 |

## Estrutura do Projeto

```
payment-financial/
├── Payment.Api.sln
├── src/
│   ├── Payment.Api/              # API + DI + Middleware
│   ├── Payment.Domain/           # Entidades, Enums, Value Objects
│   ├── Payment.Application/      # CQRS + MediatR + FluentValidation
│   ├── Payment.Infrastructure/   # EF Core, RabbitMQ, JWT, Gateway
│   └── Payment.Worker/           # Background Service
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
| GET | `/health` | Health check | Não | — |
| POST | `/api/payments` | Processar pagamento | JWT Bearer | 10 req/min |
| GET | `/api/payments/{id}` | Consultar pagamento | JWT Bearer | — |
| DELETE | `/api/payments/{id}` | Cancelar pagamento | JWT Bearer | 3 req/min |

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

### Exemplo de requisição (cancelamento)

```bash
curl -X DELETE http://localhost:5001/api/payments/<payment-id> \
  -H "Authorization: Bearer <token>"
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
- Swagger (apenas em desenvolvimento)
- CORS (origem do frontend)
- Rate Limiting (10 req/min payments, 3 req/min strict)
- JWT Authentication (HS256, issuer, audience, clock skew zero)
- JwtUserMiddleware (extração de UserId do token)
- MediatR pipeline: LoggingBehaviour → ValidationBehaviour → PerformanceBehaviour
- Limite de payload: 1MB

## Segurança

- JWT validado com algoritmo explícito (apenas HS256)
- Issuer e Audience lidos da configuração
- ClockSkew = 0 (sem tolerância)
- Rate limiting por endpoint (Payment: 10/min, Strict: 3/min)
- CORS restrito ao frontend (origens configuráveis)
- Validação de Origin/Referer em requisições POST/PUT/DELETE
- Limite de payload de 1MB
- Idempotency-Key obrigatório (único no banco de dados)
- State machine com guardas (transições inválidas lançam exceção)
- Logs sem dados sensíveis (nunca loga card number, CVV)
- Erros 500 genéricos em produção (sem detalhes)
- Conexão PostgreSQL com SSL (sslmode=Require)
- Pool de conexão limitado (max 10)

## Testes

O projeto possui **82 testes** cobrindo todas as camadas:

| Categoria | Testes | Cobertura |
|-----------|--------|-----------|
| Domain (Money, Payment, PaymentLog) | 18 | Entidades, value objects, state machine |
| Validators (ProcessPayment, CancelPayment) | 19 | Regras de validação FluentValidation |
| Handlers (Process, Cancel, GetPayment) | 12 | CQRS handlers com mocks |
| Infrastructure (FakePaymentGateway) | 6 | Luhn, expiry, PIX, Boleto |
| JwtValidator | 7 | Token válido, expirado, assinatura, claims |
| ExceptionHandlingMiddleware | 6 | Mapeamento 400/404/409/422/500 |
| Integration | 2 | Fluxo completo com InMemory DB |

```bash
# Executar todos os testes
dotnet test

# Executar apenas testes unitários
dotnet test tests/Payment.UnitTests

# Executar com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## Integração

Consulte [docs/integracao-frontend-backend.md](docs/integracao-frontend-backend.md) para instruções detalhadas de integração com:

- **Frontend Next.js** — service de pagamento, tipos, rotas de checkout
- **api-financial (Node.js)** — consumer RabbitMQ, atualização de plano, audit log

## Licença

MIT
