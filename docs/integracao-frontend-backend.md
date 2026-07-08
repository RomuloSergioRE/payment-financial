# Integração Frontend + Backend (api-financial) com Payment MS

Este documento explica como integrar o **Payment MS** (C#) com o **frontend Next.js** e o **api-financial (Node.js)**.

---

## Índice

1. [Arquitetura da Integração](#1-arquitetura-da-integração)
2. [Frontend — Next.js](#2-frontend--nextjs)
3. [Backend — api-financial (Consumer Node.js)](#3-backend--api-financial-consumer-nodejs)
4. [Fluxo Completo](#4-fluxo-completo)
5. [Variáveis de Ambiente](#5-variáveis-de-ambiente)

---

## 1. Arquitetura da Integração

```
┌──────────────┐     HTTPS (JWT Bearer)     ┌──────────────────────┐
│              │──────────────────────────▶  │                      │
│   Frontend   │                             │   Payment MS (C#)    │
│  (Next.js)   │◀──────────────────────────  │   ASP.NET Core 8     │
│              │     JSON Response           │   Porta 5001         │
└──────────────┘                             └──────────┬───────────┘
                                                         │
                                                         │ Evento (payment.completed)
                                                         ▼
                                            ┌──────────────────────┐
                                            │     RabbitMQ         │
                                            │  Exchange: payment   │
                                            │  .events (topic)     │
                                            └──────────┬───────────┘
                                                         │
                                                         │ Consome evento
                                                         ▼
                                            ┌──────────────────────┐
                                            │  api-financial (Node)│
                                            │  - Atualiza user.plan│
                                            │  - Cria audit_log    │
                                            │  - PostgreSQL (main) │
                                            └──────────────────────┘
```

### Fluxo Resumido

1. Frontend envia requisição de pagamento → Payment MS
2. Payment MS processa (Fake Gateway) e publica evento no RabbitMQ
3. Consumer no api-financial recebe o evento e atualiza o banco principal

---

## 2. Frontend — Next.js

### 2.1 Service de Pagamento

Criar `app-financial/src/services/payment.service.ts`:

```typescript
import api from './api';
import type { PaymentResponse } from '@/types/payment';

export const paymentService = {
  processPayment: async (data: ProcessPaymentRequest): Promise<PaymentResponse> => {
    const { data: response } = await api.post(
      `${process.env.NEXT_PUBLIC_PAYMENT_API_URL}/api/payments`,
      data,
      {
        headers: {
          'Idempotency-Key': data.idempotencyKey,
        },
      }
    );
    return response;
  },

  getPayment: async (id: string): Promise<PaymentResponse> => {
    const { data } = await api.get(
      `${process.env.NEXT_PUBLIC_PAYMENT_API_URL}/api/payments/${id}`
    );
    return data;
  },
};
```

### 2.2 Tipos TypeScript

Criar `app-financial/src/types/payment.ts`:

```typescript
export type PaymentMethod = 'credit_card' | 'pix' | 'boleto';

export interface ProcessPaymentRequest {
  planType: 'pro' | 'enterprise';
  amount: number;
  currency: string;
  paymentMethod: PaymentMethod;
  idempotencyKey: string;
  // Credit card (opcional para PIX/Boleto)
  cardNumber?: string;
  cardCvv?: string;
  cardExpiryMonth?: number;
  cardExpiryYear?: number;
  cardHolderName?: string;
}

export interface PaymentResponse {
  paymentId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'refunded';
  errorMessage?: string;
}
```

### 2.3 Rotas de Checkout

| Rota | Descrição |
|------|-----------|
| `/financial/checkout` | Página de checkout/upgrade |
| `/financial/checkout/success` | Confirmação de pagamento |
| `/financial/checkout/failed` | Erro no pagamento |

### 2.4 Cartões de Teste

| Número | Bandeira | CVV | Expira | Resultado |
|--------|----------|-----|--------|-----------|
| `4111 1111 1111 1111` | Visa | `123` | 12/2028 | Sucesso |
| `5555 5555 5555 4444` | Mastercard | `123` | 12/2028 | Sucesso |
| `3782 822463 10005` | Amex | `1234` | 12/2028 | Sucesso |
| `4000 0000 0000 0002` | Qualquer | `123` | 12/2028 | Declinado |

### 2.5 Dados de Exemplo para PIX

```json
{
  "planType": "pro",
  "amount": 29.00,
  "currency": "BRL",
  "paymentMethod": "pix",
  "idempotencyKey": "payment_550e8400_1710000000"
}
```

### 2.6 Dados de Exemplo para Boleto

```json
{
  "planType": "enterprise",
  "amount": 99.00,
  "currency": "BRL",
  "paymentMethod": "boleto",
  "idempotencyKey": "payment_550e8401_1710000000"
}
```

---

## 3. Backend — api-financial (Consumer Node.js)

### 3.1 Dependência

```bash
cd api-financial
npm install amqplib
```

### 3.2 Consumer Service

Criar `api-financial/src/services/payment-consumer.service.ts`:

```typescript
import amqp from 'amqplib';

interface PaymentCompletedEvent {
  eventId: string;
  type: 'payment.completed';
  timestamp: string;
  data: {
    paymentId: string;
    userId: string;
    planType: 'pro' | 'enterprise';
    amount: number;
    currency: string;
    paymentMethod: string;
    paidAt: string;
  };
}

export async function startPaymentConsumer() {
  const connection = await amqp.connect(process.env.RABBITMQ_URL!);
  const channel = await connection.createChannel();

  const exchange = 'payment.events';
  const queue = 'payment.completed.queue';

  await channel.assertExchange(exchange, 'topic', { durable: true });
  await channel.assertQueue(queue, {
    durable: true,
    deadLetterExchange: 'payment.dlx',
  });
  await channel.bindQueue(queue, exchange, 'payment.completed');

  channel.consume(queue, async (msg) => {
    if (!msg) return;

    try {
      const event: PaymentCompletedEvent = JSON.parse(msg.content.toString());
      await handlePaymentCompleted(event);
      channel.ack(msg);
    } catch (error) {
      console.error('Failed to process payment event', error);
      channel.nack(msg, false, false);
    }
  });
}

async function handlePaymentCompleted(event: PaymentCompletedEvent) {
  const { userId, planType } = event.data;

  // Atualiza o plano do usuário
  await UserRepository.update(userId, { plan: planType });

  // Registra audit log
  await AuditLogRepository.create({
    action: 'PLAN_UPGRADE',
    targetId: userId,
    targetType: 'user',
    details: {
      from: 'free',
      to: planType,
      paymentId: event.data.paymentId,
      amount: event.data.amount,
    },
  });

  // Opcional: invalidar JWTs atuais do usuário
  await RefreshTokenRepository.deleteByUserId(userId);
}
```

### 3.3 Startup

Adicionar ao `api-financial/src/server.ts`:

```typescript
import { startPaymentConsumer } from './services/payment-consumer.service.js';

// ... existing code ...

if (process.env.RABBITMQ_URL) {
  await startPaymentConsumer();
  logger.info('Payment consumer started');
} else {
  logger.warn('RABBITMQ_URL not set, payment consumer disabled');
}
```

---

## 4. Fluxo Completo

```
1. Usuário seleciona plano (Pro/Enterprise) no frontend
2. Frontend redireciona para página de checkout do Payment MS
3. Usuário preenche dados de pagamento (Cartão/PIX/Boleto)
4. Frontend envia POST /api/payments com JWT Bearer + Idempotency-Key
5. Payment MS valida JWT (mesmo secret do api-financial)
6. Payment MS processa pagamento via Fake Gateway (Bogus)
7. Payment MS salva no PostgreSQL (payment_financial)
8. Payment MS publica evento payment.completed no RabbitMQ
9. Consumer Node.js no api-financial recebe o evento
10. api-financial atualiza user.plan no banco principal
11. api-financial registra audit_log
12. Frontend recebe confirmação via polling ou WebSocket
```

---

## 5. Variáveis de Ambiente

### Frontend (`app-financial/.env`)

```bash
NEXT_PUBLIC_API_URL=http://localhost:3000
NEXT_PUBLIC_PAYMENT_API_URL=http://localhost:5001
```

### api-financial (`api-financial/.env`)

```bash
RABBITMQ_URL=amqp://guest:guest@localhost:5672
```

### Payment MS (`payment-financial/.env`)

```bash
JWT_SECRET=<mesmo secret do api-financial>
```

> **Importante:** O `JWT_SECRET` deve ser **exatamente o mesmo** no Payment MS e no api-financial para que a validação do token funcione corretamente.
