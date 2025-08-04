
![banner](./banner.png)

# Rinha de Backend 2025 - Payment processor

Minha submissão para a [Rinha de Backend 2025](https://github.com/zanfranceschi/rinha-de-backend-2025), que esse ano tem como tema **Payment processor**.

## Tecnologias Utilizadas
- [FSharp](https://fsharp.org)
- [SQLite](https://www.sqlite.org/index.html)
- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

## Repositório

O código-fonte da API pode ser encontrado no repositório abaixo:
- [Repositório da API](https://github.com/vhogemann/rinha-de-backend-2025-fsharp)

## Arquitetura

```mermaid
flowchart TD
    subgraph API Layer
        APIRoute1[POST /payments]
        APIRoute2[GET /payments-summary]
    end

    subgraph Gateway
        G[Gateway]
        GQ["MailboxProcessor<GatewayPaymentRequest> (paymentAgent)"]
    end

    subgraph Client_Default
        C1[Client (default)]
        C1Q["Internal State: PaymentSummary, Health"]
    end

    subgraph Client_Fallback
        C2[Client (fallback)]
        C2Q["Internal State: PaymentSummary, Health"]
    end

    subgraph Persistence
        P[Persistence]
        DB[(SQLite DB)]
    end

    APIRoute1 -->|PaymentRequest| G
    G -->|Posts to| GQ
    GQ -->|Selects route| C1
    GQ -->|Or fallback| C2
    C1 -->|POST /payments| PaymentGatewayAPI1[(Payment Gateway API)]
    C2 -->|POST /payments| PaymentGatewayAPI2[(Payment Gateway API)]
    C1 --> C1Q
    C2 --> C2Q
    C1Q -.->|Summary| G
    C2Q -.->|Summary| G
    GQ -->|On success, SavePayment| P
    P -->|INSERT| DB
    APIRoute2 -->|SummaryRequest| P
    P -->|SELECT| DB
    P -->|PaymentsSummaryResponse| APIRoute2
    G -->|StartHealthChecks| C1
    G -->|StartHealthChecks| C2
    C1 -->|GET /payments/service-health| PaymentGatewayAPI1
    C2 -->|GET /payments/service-health| PaymentGatewayAPI2
```
