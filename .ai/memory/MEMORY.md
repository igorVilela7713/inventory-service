# Project Memory — inventory-service

## Project Overview

- **Name:** inventory-service
- **Description:** C#/.NET 10 inventory microservice with Kafka (MassTransit), Kubernetes (Helm + Kustomize + Strimzi + KEDA), and full observability (OpenTelemetry)
- **License:** MIT
- **GitHub:** igorVilela7713/inventory-service
- **Status:** Phase 1 scaffold complete (solution structure, Dockerfile, CI, basic domain + application + infrastructure + API + worker). Phases 2-8 (persistence migrations, full API, events/outbox, resilience/cache/observability, K8s manifests, Strimzi/KEDA/Helm, polish) are planned but not yet implemented.

## Tech Stack

| Component          | Technology                                          | Version   |
|--------------------|-----------------------------------------------------|-----------|
| Language           | C#                                                  | 10+       |
| Runtime            | .NET (LTS)                                          | 10.0      |
| Web Framework      | ASP.NET Core Minimal API                             | 10.x      |
| ORM                | EF Core + Npgsql                                    | 10.x (Npgsql 10.*) |
| Messaging          | MassTransit + Confluent.Kafka                        | 9.*       |
| Cache              | StackExchange.Redis                                  | 3.*       |
| Validation         | FluentValidation                                     | 12.*      |
| Observability      | OpenTelemetry (OTLP) + Serilog + Prometheus          | OTel 1.*, Serilog 8.*, prometheus-net 8.* |
| Resilience         | Polly (planned, not yet wired)                       | —         |
| Testing            | xUnit + FluentAssertions + Moq + Testcontainers      | xunit 2.*, FA 7.*, Moq 4.*, TC 4.* |
| K8s Deployment     | Kustomize + Helm + Strimzi + KEDA                    | Helm 0.1.0 |
| CI                 | GitHub Actions (build/test/lint/docker/k8s validate) | —         |

## Architecture

**Clean Architecture** with 4 layers:

```
Domain → Application → Infrastructure → Api / Worker
```

### Layer Responsibilities

| Layer | Project | Contents |
|-------|---------|----------|
| Domain | `src/Inventory.Domain/` | Entities (`StockItem`, `Reservation`), Enums (`ReservationStatus`), Domain Events (`StockReserved`, `StockInsufficient`), MediatR Contracts |
| Application | `src/Inventory.Application/` | Service layer (`InventoryService`), Interfaces (`IStockRepository`, `IReservationRepository`, `IUnitOfWork`), Validators (`CreateStockItemValidator`, `CreateReservationValidator`), DTOs (records) |
| Infrastructure | `src/Inventory.Infrastructure/` | EF Core (`InventoryDbContext`), Repositories (`StockRepository`, `ReservationRepository`, `UnitOfWork`), Kafka/MassTransit config, Redis, OpenTelemetry wiring |
| Api | `src/Inventory.Api/` | Minimal API endpoints (`StockEndpoints`, `ReservationEndpoints`), `Program.cs` (DI, middleware, health checks, Swagger, Prometheus) |
| Worker | `worker/Inventory.Worker/` | MassTransit consumer (`OrderEventConsumer`), `Program.cs` (host builder + Kafka consumer registration) |

### Data Flow

```
HTTP Request → API Endpoints → InventoryService → Repository → EF Core → PostgreSQL
Kafka Event  → Worker → OrderEventConsumer → InventoryService → same path
```

### Solution Projects (7 total)

```
inventory-service.sln
├── src/Inventory.Domain/          (SDK: Microsoft.NET.Sdk)
├── src/Inventory.Application/     (SDK: Microsoft.NET.Sdk)
├── src/Inventory.Infrastructure/  (SDK: Microsoft.NET.Sdk)
├── src/Inventory.Api/             (SDK: Microsoft.NET.Sdk.Web)
├── worker/Inventory.Worker/       (SDK: Microsoft.NET.Sdk.Worker)
├── tests/Inventory.UnitTests/     (SDK: Microsoft.NET.Sdk)
└── tests/Inventory.IntegrationTests/ (SDK: Microsoft.NET.Sdk)
```

## Environment Requirements

- .NET 10 SDK
- Docker & Docker Compose
- For K8s: kubectl, helm, kubeconform (CI downloads on the fly)

## Build / Test / Run / Deploy Commands

```bash
# Build
dotnet build
dotnet build -c Release

# Test
dotnet test
dotnet test -c Release --collect:"XPlat Code Coverage"

# Run API
dotnet run --project src/Inventory.Api

# Run Worker
dotnet run --project worker/Inventory.Worker

# Lint (format check)
dotnet format --verify-no-changes

# Docker
docker build -t inventory-service .
docker compose up -d
docker compose down -v

# Kubernetes (Kustomize)
kubectl apply -k deploy/kustomize/overlays/dev

# Kubernetes (Helm)
helm install inventory deploy/helm/inventory-service

# K8s Validation (CI)
kubeconform -strict -summary -skip Kustomization deploy/kustomize/base/
helm lint deploy/helm/inventory-service/
```

## API Endpoints

All under prefix `/api/v1/`:

### Stock (`/api/v1/stock`)

| Method | Path | Description | Request Body | Response |
|--------|------|-------------|--------------|----------|
| GET | `/` | List stock items (paginated: `?page=0&size=20`) | — | `200 List<StockItem>` |
| GET | `/{productId}` | Get stock for a product | — | `200 StockItem` / `404` |
| POST | `/` | Create stock item | `{ ProductId, ProductName, Quantity }` | `201 StockItem` / `400 ValidationProblem` |
| POST | `/{productId}/adjust` | Adjust stock by delta | `{ Delta: int }` | `200 StockItem` |

### Reservations (`/api/v1/reservations`)

| Method | Path | Description | Request Body | Response |
|--------|------|-------------|--------------|----------|
| POST | `/` | Reserve stock for an order | `{ OrderId, ProductId, Quantity }` | `201 Reservation` / `400 ValidationProblem` |
| DELETE | `/{id:guid}` | Cancel reservation | — | `204` / `404` |

### Health & Metrics

| Path | Description |
|------|-------------|
| `/health/live` | Liveness probe (no checks) |
| `/health/ready` | Readiness probe (PostgreSQL check) |
| `/metrics` | Prometheus metrics endpoint |

## Key Entities

### StockItem (`src/Inventory.Domain/Entities/StockItem.cs`)

- `Id: Guid`, `ProductId: string`, `ProductName: string`, `Quantity: int`, `ReservedQuantity: int`
- Computed: `AvailableQuantity => Quantity - ReservedQuantity`
- Methods: `Reserve(amount)`, `Release(amount)`, `Adjust(delta)`, `CanReserve(amount)`
- EF Core config: unique index on `ProductId`, max lengths: ProductId=100, ProductName=200

### Reservation (`src/Inventory.Domain/Entities/Reservation.cs`)

- `Id: Guid`, `OrderId: string`, `ProductId: string`, `Quantity: int`, `Status: ReservationStatus`
- Timestamps: `CreatedAt`, `ConfirmedAt?`, `CancelledAt?`
- Status enum: `Pending`, `Confirmed`, `Cancelled`, `Expired`
- EF Core config: index on `OrderId`, status stored as string via value conversion

### Domain Events (`src/Inventory.Domain/Events/`)

- `StockReserved(OrderId, ProductId, Quantity, Timestamp)` — record
- `StockInsufficient(OrderId, ProductId, RequestedQuantity, AvailableQuantity, Timestamp)` — record

## Key Design Decisions

- **Transactional Outbox** for atomic event publishing (planned, not yet implemented)
- **Repository pattern** + **Unit of Work** (interfaces in Application, implementations in Infrastructure)
- **FluentValidation** for request validation at API layer (returns `ValidationProblem`)
- **Result/error pattern** in service layer: `InvalidOperationException` for business errors (planned migration to Result<T>)
- **Domain Events** as records (MediatR Contracts in Domain)
- **Idempotent consumers** absorb retry/DLQ redeliveries (planned)

## Coding Conventions

- File-scoped namespaces (`namespace Inventory.Domain.Entities;`)
- Records for DTOs and domain events (`public record StockAdjustRequest(int Delta);`)
- Nullable reference types enabled globally
- Implicit usings enabled globally
- `TreatWarningsAsErrors=false` (with CA1848 and CA1873 suppressed)
- No global mutable state
- Error messages in lowercase (FluentValidation messages)
- `AnalysisLevel=latest-recommended` in `Directory.Build.props`
- Conventional commits in git history (`feat:`, `fix(scope):`)

## Dependencies

### Internal

- **order-service** via Kafka topic `inv.order-events` (consumes ORDER_CREATED events)

### External

| Service | Connection | Purpose |
|---------|-----------|---------|
| PostgreSQL 16 (Alpine) | `Host=postgres;Database=inventory;Username=postgres;Password=postgres` | Primary data store |
| Kafka 3.7 (Bitnami) | `kafka:9092` | Event streaming (topics: `inv.order-events`, `inv.stock-reserved`) |
| Redis 7 (Alpine) | `redis:6379` | Read-through cache (planned) |

### Docker Compose Services

- `api` — ASP.NET Core API (port 8080)
- `worker` — MassTransit consumer (same image, different entrypoint)
- `postgres` — PostgreSQL 16-alpine
- `kafka` — Bitnami Kafka 3.7 (single node, KRaft mode)
- `redis` — Redis 7-alpine

## K8s Structure

```
deploy/
├── kustomize/
│   ├── base/              (deployment, service, configmap, secret)
│   └── overlays/
│       └── dev/           (patch-replicas, kustomization)
├── helm/
│   └── inventory-service/ (Chart.yaml v0.1.0, values.yaml)
├── strimzi/               (kafka.yaml, kafkatopic-order-events.yaml, kafkatopic-stock-reserved.yaml)
└── keda/                  (scaled-object.yaml: worker, lagThreshold=5, min=1, max=10)
```

### K8s Deployment Details

- API Deployment: 2 replicas, ports 8080, health probes on /health/ready + /health/live
- Worker: KEDA ScaledObject scales by Kafka consumer lag (threshold: 5)
- Strimzi Kafka: 3.7.0, 1 replica, PLAINTEXT listener
- Topics: `inv.order-events` (3 partitions), `inv.stock-reserved` (3 partitions)

## Known Risks

1. **SDK mismatch:** Local SDK may be .NET 7; CI uses 10.x — ensure `dotnet --version` is 10.x locally
2. **Testcontainers depends on Docker:** Windows requires Docker Desktop / WSL2 backend
3. **Kafka/Testcontainers are memory-heavy:** Consider Redpanda as lightweight alternative for tests
4. **Outbox duplicate/race conditions:** Requires separate table + concurrency control (not yet implemented)
5. **Strimzi/KEDA require CRDs:** Cluster must have Strimzi and KEDA operators installed
6. **EF Core migrations depend on dotnet-ef tool:** Must be installed globally or as local tool
7. **Secrets must not be committed:** Connection strings in docker-compose are for local dev only
8. **Worker + API need healthchecks in docker-compose:** Currently only API has HEALTHCHECK in Dockerfile
9. **Stack version alignment:** MassTransit 9.x + Kafka 3.7 + EF Core 10.x must be compatible
10. **Windows/git-bash paths:** CI runs on ubuntu-latest; local dev on Windows — watch for path separators

## Project Status (per PLAN.md)

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Scaffold & Foundation | Partial | Solution structure, Dockerfile, CI done. Domain/Application layers scaffolded but not complete. |
| Phase 2: Persistence | Not started | EF Core migrations, repository impls, unit tests |
| Phase 3: API (Minimal) | Partial | Endpoints exist but no auth, no ProblemDetails middleware, no docs |
| Phase 4: Events (Kafka + Outbox) | Not started | MassTransit config, Transactional Outbox, idempotency |
| Phase 5: Resilience + Cache + Observability | Not started | Redis cache, Polly, OpenTelemetry full setup, Serilog structured |
| Phase 6: Kubernetes (Manifests + Kustomize) | Partial | Base manifests exist, overlays started |
| Phase 7: Kubernetes (Strimzi + KEDA + Helm) | Partial | Strimzi CRs, KEDA ScaledObject, Helm chart exist as templates |
| Phase 8: Polish | Not started | Grafana dashboards, demo scripts, coverage badge |
