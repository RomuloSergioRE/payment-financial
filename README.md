# Payment Financial

Microserviço de pagamentos do ecossistema zenyFin. Processa upgrades de plano (Pro/Enterprise) com suporte a Cartão de Crédito, PIX e Boleto via gateway falso (Bogus), utilizando CQRS + Vertical Slices.

## Funcionalidades

- Processamento de pagamentos (Cartão, PIX, Boleto) via gateway fake com Bogus
- Arquitetura CQRS + Vertical Slices com MediatR
- Mensageria assíncrona com RabbitMQ (event-driven)
- Validação JWT rigorosa (HS256, issuer, audience, clock skew zero)
- Rate Limiting, Idempotência e CORS configurados
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
| Logs | Serilog 3 |
| Retry | Polly 8 |
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

| Método | Rota | Descrição | Autenticação |
|--------|------|-----------|-------------|
| GET | `/health` | Health check | Não |
| POST | `/api/payments` | Processar pagamento | JWT Bearer |
| GET | `/api/payments/{id}` | Consultar pagamento | JWT Bearer |

### Exemplo de requisição (pagamento)

```bash
curl -X POST http://localhost:5001/api/payments \
  -H "Authorization: Bearer <token>" \
  -H "Idempotency-Key: payment_user_123_1710000000" \
  -H "Content-Type: application/json" \
  -d '{
    "planType": "pro",
    "amount": 29.00,
    "currency": "BRL",
    "paymentMethod": "credit_card",
    "cardNumber": "4111111111111111",
    "cardCvv": "123",
    "cardExpiryMonth": 12,
    "cardExpiryYear": 2028,
    "cardHolderName": "João Silva"
  }'
```

## Cartões de Teste

| Número | Bandeira | CVV | Validade | Resultado |
|--------|----------|-----|----------|-----------|
| `4111 1111 1111 1111` | Visa | `123` | 12/2028 | Aprovado |
| `5555 5555 5555 4444` | Mastercard | `123` | 12/2028 | Aprovado |
| `3782 822463 10005` | Amex | `1234` | 12/2028 | Aprovado |
| `4000 0000 0000 0002` | Qualquer | `123` | 12/2028 | Declinado |

## Pipelines Configurados (Program.cs)

- Serilog (bootstrap + request logging)
- Swagger (apenas em desenvolvimento)
- EF Core + PostgreSQL
- JWT Authentication (HS256, issuer, audience, clock skew zero)
- CORS (origem do frontend)
- Rate Limiting (10 req/min payments, 3 req/min strict)
- MediatR + ValidationBehaviour + LoggingBehaviour
- FluentValidation (validação automática)

## Segurança

- JWT validado com algoritmo explícito (apenas HS256)
- Issuer e Audience validados
- ClockSkew = 0 (sem tolerância)
- Rate limiting por endpoint
- CORS restrito ao frontend
- Idempotency-Key obrigatório
- Logs sem dados sensíveis (nunca loga card number, CVV)
- Erros 500 genéricos em produção

## Integração

Consulte [docs/integracao-frontend-backend.md](docs/integracao-frontend-backend.md) para instruções detalhadas de integração com:

- **Frontend Next.js** — service de pagamento, tipos, rotas de checkout
- **api-financial (Node.js)** — consumer RabbitMQ, atualização de plano, audit log

## Licença

MIT
