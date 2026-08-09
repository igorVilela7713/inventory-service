# Architectural Decision Records — inventory-service

---

### ADR-001: .NET 10 LTS Runtime

**Status:** Accepted

**Context:** The project targets a long-lived microservice. .NET 8 LTS support ends November 2026; .NET 9 is STS (short-term support). .NET 10 LTS extends support to November 2028, providing a stable runtime for production workloads.

**Decision:** Target `net10.0` as the framework version in `Directory.Build.props` (`<TargetFramework>net10.0</TargetFramework>`). CI uses `dotnet-version: '10.x'` in GitHub Actions.

**Consequences:**
- Long support window (until 2028)
- Requires .NET 10 SDK locally and in CI
- NuGet packages must be compatible (Npgsql.EntityFrameworkCore.PostgreSQL 10.*, etc.)
- Local SDK may need upgrading from .NET 7 (risk: SDK version mismatch)

---

### ADR-002: Clean / Onion Architecture

**Status:** Accepted

**Context:** The service will grow across multiple phases (persistence, events, caching, observability). Without a layered architecture, business logic would become coupled to infrastructure concerns, making testing and replacement difficult.

**Decision:** Implement Clean Architecture with 4 layers:
1. **Domain** (`src/Inventory.Domain/`) — entities, enums, domain events. No external dependencies.
2. **Application** (`src/Inventory.Application/`) — interfaces, services, validators, DTOs. Depends only on Domain.
3. **Infrastructure** (`src/Inventory.Infrastructure/`) — EF Core, Kafka, Redis implementations. Depends on Application.
4. **Api/Worker** (`src/Inventory.Api/`, `worker/Inventory.Worker/`) — entry points. Depends on all layers.

Dependency direction: Domain ← Application ← Infrastructure ← Api/Worker.

**Consequences:**
- Business logic is testable without infrastructure (unit tests mock interfaces)
- Infrastructure can be swapped (e.g., PostgreSQL to another DB) without touching Domain
- More projects and interfaces to maintain
- DI registration in Api/Worker wires everything together

---

### ADR-003: ASP.NET Core Minimal API (not Controllers)

**Status:** Accepted

**Context:** The API has a small surface area (stock + reservations = ~7 endpoints). Controllers add boilerplate (attribute routing, controller base classes, model binding attributes) that isn't justified for this scope.

**Decision:** Use ASP.NET Core Minimal API with endpoint groups. Endpoints defined in `StockEndpoints.cs` and `ReservationEndpoints.cs` as static extension methods on `IEndpointRouteBuilder`.

**Consequences:**
- Less boilerplate code per endpoint
- Endpoints are organized in separate files via extension methods
- Swagger/OpenAPI via `AddEndpointsApiExplorer()` + Swashbuckle
- Trade-off: less structure than controllers for very large APIs (acceptable here)

---

### ADR-004: EF Core + Npgsql for PostgreSQL

**Status:** Accepted

**Context:** The service needs a relational database for stock items and reservations with ACID guarantees. PostgreSQL is the target database.

**Decision:** Use Entity Framework Core with Npgsql.EntityFrameworkCore.PostgreSQL (version 10.*) as the ORM. Fluent API configuration in `InventoryDbContext.OnModelCreating()`.

**Consequences:**
- Productive developer experience with LINQ queries
- Database migrations via EF Core tooling (`dotnet ef migrations add`)
- Fluent API config: unique index on `ProductId`, value conversion for `ReservationStatus` enum to string
- Trade-off: ORM overhead vs raw SQL; acceptable for this service's complexity
- `InventoryDbContext` has `DbSet<StockItem>` and `DbSet<Reservation>`

---

### ADR-005: MassTransit + Kafka for Event-Driven Messaging

**Status:** Accepted

**Context:** The service needs to consume order events from `order-service` and publish stock reservation/insufficient events. Kafka provides durable, partitioned event streaming.

**Decision:** Use MassTransit (version 9.*) as the message bus abstraction with Confluent.Kafka as the transport. Topics: `inv.order-events` (consume), `inv.stock-reserved` (publish), `inv.stock-insufficient` (publish).

**Consequences:**
- MassTransit provides consumer abstractions, retry, serialization, and endpoint configuration
- Worker runs as a separate process (`worker/Inventory.Worker/`) with its own `Host.CreateApplicationBuilder()`
- Consumer: `OrderEventConsumer` handles `OrderCreatedEvent`
- Trade-off: MassTransit adds abstraction layer; direct Confluent.Kafka would be lower-level
- Worker currently uses `UsingInMemory` transport (placeholder); needs switch to Kafka for production

---

### ADR-006: Transactional Outbox Pattern

**Status:** Accepted (planned, not yet implemented)

**Context:** Publishing events after a business operation creates a dual-write problem: if the publish succeeds but the DB commit fails (or vice versa), the system becomes inconsistent.

**Decision:** Implement the Transactional Outbox pattern: events are written to an outbox table within the same SQL transaction as the business data. A separate process (polling or CDC) reads the outbox and publishes to Kafka.

**Consequences:**
- Guarantees exactly-once semantics (at-least-once + idempotent consumers)
- Requires an outbox table and a relay process
- Adds latency (event publication is async, not immediate)
- Prevents lost events on partial failures

---

### ADR-007: Repository Pattern + Unit of Work

**Status:** Accepted

**Context:** The Application layer should not depend on EF Core directly. This enables testing with mocks and potential ORM replacement.

**Decision:** Define interfaces in `Inventory.Application/Interfaces/`:
- `IStockRepository` — `GetByProductIdAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync`
- `IReservationRepository` — `GetByIdAsync`, `GetByOrderIdAsync`, `CreateAsync`, `UpdateAsync`
- `IUnitOfWork` — `SaveChangesAsync`

Implementations in `Inventory.Infrastructure/Persistence/Repositories/`: `StockRepository`, `ReservationRepository`, `UnitOfWork`.

**Consequences:**
- Application layer is ORM-agnostic
- Unit tests mock repositories (see `InventoryServiceTests` with Moq)
- UnitOfWork wraps `DbContext.SaveChangesAsync()` — single commit point per operation
- Trade-off: more abstraction layers; acceptable for testability

---

### ADR-008: Result Pattern / Error Handling in Service Layer

**Status:** Accepted (partially implemented)

**Context:** Throwing exceptions for business logic flow control is expensive and doesn't compose well. The Result pattern allows services to return success/failure without exceptions.

**Decision:** Currently uses `InvalidOperationException` for business errors (e.g., "Product not found", "Insufficient stock"). Planned migration to a `Result<T>` type that wraps success values or error codes, avoiding exception-driven control flow.

**Consequences:**
- API layer catches exceptions and maps to ProblemDetails (planned middleware)
- FluentValidation handles input validation → `Results.ValidationProblem()`
- Two-layer error handling: validation (FluentValidation) + business (Result/exceptions)
- Current implementation throws exceptions in `InventoryService` — functional but not ideal

---

### ADR-009: Redis for Read-Through Caching

**Status:** Accepted (planned, not yet implemented)

**Context:** Stock queries (`GET /stock`) may be high-frequency. Caching reduces PostgreSQL load for frequently accessed product data.

**Decision:** Use StackExchange.Redis (version 3.*) for read-through caching. Cache stock items on read, invalidate on write.

**Consequences:**
- Redis is configured in docker-compose (`redis:6379`)
- `StackExchange.Redis` 3.* is already referenced in `Inventory.Infrastructure.csproj`
- Cache invalidation strategy needs design (write-through vs write-behind)
- Adds another infrastructure dependency to manage

---

### ADR-010: Polly for Resilience (Retry + Circuit Breaker)

**Status:** Accepted (planned, not yet implemented)

**Context:** External dependencies (PostgreSQL, Kafka, Redis) can fail transiently. The service should retry transient failures and stop hammering a failing dependency (circuit breaker).

**Decision:** Use Polly for retry policies (exponential backoff) and circuit breaker patterns on outbound calls.

**Consequences:**
- Prevents cascading failures
- Configurable via `AddHttpClient()` resilience handlers or `IAsyncPolicy` registrations
- Trade-off: retries can amplify load during outages; circuit breaker mitigates this

---

### ADR-011: OpenTelemetry (OTLP) for Traces + Metrics

**Status:** Accepted (partially implemented)

**Context:** The service needs distributed tracing and metrics for observability in production. OpenTelemetry is the CNCF standard.

**Decision:** Use OpenTelemetry SDK with OTLP export for traces. Prometheus exporter for metrics via `prometheus-net.AspNetCore`. Serilog for structured logging with `Serilog.Sinks.OpenTelemetry`.

**Consequences:**
- `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http` referenced in `Inventory.Infrastructure.csproj`
- Prometheus metrics exposed at `/metrics` via `app.MapMetrics("/metrics")`
- Serilog configured in `Program.cs` with console sink and OTLP sink
- Requires OTel Collector in production for trace aggregation

---

### ADR-012: Serilog for Structured Logging

**Status:** Accepted

**Context:** Structured logging enables searching, filtering, and analyzing logs in production. Console logs without structure are hard to query.

**Decision:** Use Serilog with `ReadFrom.Configuration(builder.Configuration)`, console sink, and `Enrich.FromLogContext()`. Integrated via `builder.Host.UseSerilog()` and `app.UseSerilogRequestLogging()`.

**Consequences:**
- Request logging middleware automatically logs HTTP requests
- Structured log properties enable Kibana/Grafana log queries
- `Serilog.AspNetCore` 8.* + `Serilog.Sinks.OpenTelemetry` 4.* in `Inventory.Api.csproj`
- Trade-off: slightly more configuration than `ILogger` alone

---

### ADR-013: KEDA ScaledObject for Worker Auto-Scaling

**Status:** Accepted (manifest exists, not yet deployed)

**Context:** The Inventory Worker processes Kafka events. Standard HPA scales on CPU/memory, which doesn't reflect actual processing backlog. If Kafka consumer lag grows, the worker needs more replicas regardless of CPU usage.

**Decision:** Use KEDA (Kubernetes Event-driven Autoscaling) ScaledObject targeting the worker Deployment. Scale metric: Kafka consumer lag on `inv.order-events` topic, threshold: 5 messages, min: 1 replica, max: 10 replicas.

**Consequences:**
- Worker scales based on actual work (consumer lag), not resource utilization
- Requires KEDA operator installed in the cluster
- Config in `deploy/keda/scaled-object.yaml`
- API still uses HPA (CPU/memory) — different scaling strategy per component

---

### ADR-014: Strimzi Operator for Kafka Provisioning

**Status:** Accepted (manifests exist, not yet deployed)

**Context:** Running Kafka in Kubernetes requires a way to manage brokers, topics, and users. Manual YAML is error-prone and hard to maintain.

**Decision:** Use Strimzi Kafka operator to manage Kafka clusters and topics via CRDs. Kafka cluster: `inventory-kafka` (3.7.0, 1 replica, PLAINTEXT). Topics: `inv.order-events` (3 partitions), `inv.stock-reserved` (3 partitions).

**Consequences:**
- Declarative Kafka management via `Kafka`, `KafkaTopic` CRDs
- Strimzi operator must be installed in the cluster
- Config in `deploy/strimzi/kafka.yaml`, `kafkatopic-*.yaml`
- Single-replica Kafka for dev; production should have 3+ brokers

---

### ADR-015: Helm + Kustomize for Deployment

**Status:** Accepted (both patterns provided)

**Context:** Different teams/environments have different deployment preferences. Kustomize is simpler for overlay-based config; Helm provides templating and release management.

**Decision:** Provide both patterns:
- **Kustomize:** `deploy/kustomize/base/` (deployment, service, configmap, secret) + `overlays/dev/` (patch-replicas)
- **Helm:** `deploy/helm/inventory-service/` (Chart.yaml v0.1.0, values.yaml with image, service, resources)

**Consequences:**
- Teams can choose their preferred tool
- CI validates both: `kubeconform` for Kustomize, `helm lint` for Helm
- Slightly more maintenance (two deployment mechanisms)
- Base manifests are shared source of truth

---

### ADR-016: Testcontainers for Integration Tests

**Status:** Accepted (referenced, not yet fully implemented)

**Context:** Integration tests need real databases, message brokers, and caches. Mocking these loses fidelity. Testcontainers spin up Docker containers for tests.

**Decision:** Use Testcontainers (version 4.*) for PostgreSQL, Kafka, and Redis in integration tests. Referenced in `Inventory.IntegrationTests.csproj`:
- `Testcontainers.PostgreSql` 4.*
- `Testcontainers.Kafka` 4.*
- `Testcontainers.Redis` 4.*

**Consequences:**
- Tests run against real infrastructure (high fidelity)
- Requires Docker running locally (Docker Desktop / WSL2 on Windows)
- Slower test startup (container spin-up)
- Consider Redpanda as lightweight Kafka alternative for tests

---

### ADR-017: Nullable Reference Types + Implicit Usings + Central Package Management

**Status:** Accepted

**Context:** C# 10+ features improve code safety and reduce boilerplate. Central package management prevents version drift across projects.

**Decision:** Enabled globally in `Directory.Build.props`:
- `<Nullable>enable</Nullable>` — compiler warns on null dereference
- `<ImplicitUsings>enable</ImplicitUsings>` — auto-import common namespaces
- Central package management via `Directory.Build.props` (all projects inherit)
- `<AnalysisLevel>latest-recommended</AnalysisLevel>` — latest analyzers
- `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` — warnings visible but not blocking
- `<NoWarn>CA1848;CA1873</NoWarn>` — suppress specific analyzer warnings

**Consequences:**
- Compiler catches more null-reference bugs at compile time
- Less `using` boilerplate in every file
- Single place to update package versions (trade-off: all projects get the same version)
- Warnings visible in IDE but don't break builds (decision: pragmatic for scaffold phase)

---

### ADR-018: File-Scoped Namespaces + Primary Constructors + Records for DTOs

**Status:** Accepted

**Context:** Modern C# syntax reduces boilerplate and improves readability. These conventions are consistently applied across the codebase.

**Decision:**
- File-scoped namespaces: `namespace Inventory.Domain.Entities;` (no curly braces)
- Records for DTOs and domain events: `public record CreateStockItemRequest(string ProductId, string ProductName, int Quantity);`
- Domain events as records: `public record StockReserved(string OrderId, string ProductId, int Quantity, DateTime Timestamp);`
- Primary constructors used where applicable (e.g., `InventoryService` still uses traditional constructor for clarity)

**Consequences:**
- Less boilerplate (no namespace braces, no record class ceremony)
- DTOs are immutable by default (records)
- Consistent style across all source files
