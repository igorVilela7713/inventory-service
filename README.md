# inventory-service

[![CI](https://github.com/igorVilela7713/inventory-service/actions/workflows/ci.yml/badge.svg)](https://github.com/igorVilela7713/inventory-service/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> C#/.NET 10 inventory microservice with Kafka (MassTransit), Kubernetes (Helm + Kustomize + Strimzi + KEDA), and full observability (OpenTelemetry).

## Architecture

```
┌──────────────────────────────────────────────────┐
│  Client / order-service / App / curl             │
│  (HTTP + JWT Bearer)                             │
└───────────────┬──────────────────────────────────┘
                │ GET/POST /api/v1/stock · /reservations
                ▼
┌──────────────────────────────────────────────────┐
│   inventory-service · InventoryApi               │
│   ASP.NET Core (minimal API) · .NET 10           │
│   FluentValidation · API key/JWT                 │
└───────┬───────────────────────────┬──────────────┘
        │ EF Core (Postgres)        │ cache
        ▼                           ▼
┌────────────────────┐       ┌──────────────┐
│ PostgreSQL         │       │ Redis        │
│ (EF Core migrations)│      │ (read cache) │
└────────────────────┘       └──────────────┘
        │ Outbox (transactional)
        ▼
┌────────────────────┐       ┌──────────────┐
│ Kafka (Strimzi)    │◄──────│ Inventory    │
│ inv.order-events   │       │ Worker       │
│ inv.stock-reserved │       │ (MassTransit)│
└────────────────────┘       └──────────────┘
   Scaling: HPA (API) · KEDA (Worker, by consumer-lag)
```

## Quick Start

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose

### Run locally
```bash
# Start infrastructure (PostgreSQL + Kafka + Redis)
docker compose up -d

# Run API
dotnet run --project src/Inventory.Api

# Run Worker
dotnet run --project worker/Inventory.Worker
```

### Run tests
```bash
dotnet test
```

### Build Docker image
```bash
docker build -t inventory-service .
```

### Deploy to Kubernetes
```bash
# Apply with Kustomize
kubectl apply -k deploy/kustomize/overlays/dev

# Or install Helm chart
helm install inventory deploy/helm/inventory-service
```

## Project Structure

```
inventory-service/
├── src/
│   ├── Inventory.Domain/          # Aggregates, domain events
│   ├── Inventory.Application/     # Use-cases, validators, interfaces
│   ├── Inventory.Infrastructure/  # EF Core, Kafka, Redis, Outbox
│   └── Inventory.Api/             # Minimal API, health checks
├── worker/Inventory.Worker/       # MassTransit consumer (BackgroundService)
├── tests/
│   ├── Inventory.UnitTests/
│   └── Inventory.IntegrationTests/ # Testcontainers (Pg + Kafka + Redis)
├── deploy/
│   ├── kustomize/                 # base + overlays (dev/staging/prod)
│   ├── helm/                      # Helm chart
│   ├── strimzi/                   # Kafka CRDs
│   └── keda/                      # ScaledObject (consumer-lag)
├── docs/
├── .github/workflows/ci.yml
├── Dockerfile                     # multi-stage
├── docker-compose.yml
└── Makefile
```

## Docs
- [API Reference](docs/api.md)
- [Architecture](docs/architecture.md)
- [Development Guide](docs/development.md)
- [Kubernetes Guide](docs/k8s.md)

## License
MIT
