# SPEC — inventory-service

## Functional Requirements

### Stock Management
- **GET /api/v1/stock** — List all stock items (paginated)
- **GET /api/v1/stock/{productId}** — Get stock for a product
- **POST /api/v1/stock** — Create/update stock quantity
- **POST /api/v1/stock/{productId}/adjust** — Adjust stock (+/-)

### Reservations
- **POST /api/v1/reservations** — Reserve stock for an order
- **DELETE /api/v1/reservations/{id}** — Cancel reservation
- **GET /api/v1/reservations?orderId={id}** — Get reservations for order

### Event Consumption
- Consume `inv.order-events` (ORDER_CREATED, ORDER_CANCELLED)
- Publish `inv.stock-reserved`, `inv.stock-insufficient` via Transactional Outbox

## Technical Requirements

### Stack
- **Runtime:** .NET 10 (LTS)
- **Web:** ASP.NET Core Minimal API
- **ORM:** EF Core + Npgsql (PostgreSQL)
- **Messaging:** MassTransit + Confluent.Kafka
- **Cache:** StackExchange.Redis
- **Validation:** FluentValidation
- **Observability:** OpenTelemetry (OTLP) + Serilog + Prometheus
- **Resilience:** Polly (retry + circuit breaker)
- **Tests:** xUnit + FluentAssertions + Testcontainers

### Patterns
- Clean Architecture (Domain → Application → Infrastructure → Api)
- Repository Pattern (one per aggregate)
- Transactional Outbox (atomic event publishing)
- Domain Events (dispatched after commit)
- Idempotent consumers (absorbs retry/DLQ redeliveries)

### Best Practices
- Nullable reference types enabled
- Implicit usings
- Central package management (Directory.Build.props)
- .editorconfig enforced in CI (`dotnet format --verify-no-changes`)
- Multi-stage Dockerfile (build → test → runtime)
- Health checks: `/health/live` + `/health/ready` (for K8s probes)
- Structured logging (Serilog JSON)
