# AGENTS.md — inventory-service

## Project Context
C#/.NET 10 inventory microservice. Complements order-service (Java) in an
event-driven stock reservation flow via Apache Kafka. Demonstrates Kubernetes
skills: Helm, Kustomize, Strimzi, KEDA.

## Stack
- .NET 10 (LTS), ASP.NET Core Minimal API, EF Core, PostgreSQL
- MassTransit + Kafka, Redis, FluentValidation, Polly
- OpenTelemetry, Serilog, Prometheus
- xUnit, FluentAssertions, Testcontainers
- Kubernetes: Helm, Kustomize, Strimzi, KEDA

## Architecture
Clean Architecture: Domain → Application → Infrastructure → Api/Worker

## File Tree
```
inventory-service/
├── .editorconfig
├── .github/workflows/ci.yml
├── .gitignore
├── Directory.Build.props
├── Dockerfile
├── Makefile
├── README.md
├── SPEC.md
├── AGENTS.md
├── PLAN.md
├── docker-compose.yml
├── inventory-service.sln
├── src/
│   ├── Inventory.Domain/
│   │   ├── Entities/
│   │   │   ├── StockItem.cs
│   │   │   └── Reservation.cs
│   │   ├── Events/
│   │   │   ├── StockReserved.cs
│   │   │   └── StockInsufficient.cs
│   │   └── Enums/
│   │       └── ReservationStatus.cs
│   ├── Inventory.Application/
│   │   ├── Interfaces/
│   │   │   ├── IStockRepository.cs
│   │   │   ├── IReservationRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Services/
│   │   │   └── InventoryService.cs
│   │   └── Validators/
│   │       ├── CreateStockItemValidator.cs
│   │       └── CreateReservationValidator.cs
│   ├── Inventory.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── InventoryDbContext.cs
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── Messaging/
│   │   │   ├── KafkaConfig.cs
│   │   │   └── OutboxDispatcher.cs
│   │   └── Cache/
│   │       └── RedisCacheService.cs
│   └── Inventory.Api/
│       ├── Program.cs
│       ├── Endpoints/
│       │   ├── StockEndpoints.cs
│       │   └── ReservationEndpoints.cs
│       ├── Middleware/
│       │   └── ApiKeyAuthMiddleware.cs
│       └── HealthChecks/
│           └── KafkaHealthCheck.cs
├── worker/
│   └── Inventory.Worker/
│       ├── Program.cs
│       └── Consumers/
│           └── OrderEventConsumer.cs
├── tests/
│   ├── Inventory.UnitTests/
│   │   └── Services/
│   │       └── InventoryServiceTests.cs
│   └── Inventory.IntegrationTests/
│       ├── StockApiTests.cs
│       └── KafkaIntegrationTests.cs
├── deploy/
│   ├── kustomize/
│   │   ├── base/
│   │   │   ├── deployment.yaml
│   │   │   ├── service.yaml
│   │   │   ├── configmap.yaml
│   │   │   ├── secret.yaml
│   │   │   └── kustomization.yaml
│   │   └── overlays/
│   │       ├── dev/
│   │       ├── staging/
│   │       └── prod/
│   ├── helm/inventory-service/
│   │   ├── Chart.yaml
│   │   ├── values.yaml
│   │   └── templates/
│   ├── strimzi/
│   │   ├── kafka.yaml
│   │   ├── kafkatopic-order-events.yaml
│   │   ├── kafkatopic-stock-reserved.yaml
│   │   └── kafkauser.yaml
│   └── keda/
│       └── scaled-object.yaml
├── observability/
│   └── otel-collector-config.yaml
├── scripts/
│   ├── demo.sh
│   └── seed.sh
└── docs/
    ├── api.md
    ├── architecture.md
    ├── development.md
    └── k8s.md
```

## Build & Test Commands
```bash
dotnet build                         # Build all
dotnet test                          # Run all tests
dotnet format --verify-no-changes    # Lint check
docker build -t inventory-service .  # Docker build
docker compose up -d                 # Start infra
helm lint deploy/helm/inventory-service  # Lint Helm chart
kubeconform deploy/kustomize/base    # Validate K8s manifests
```

## Conventions
- File-scoped namespaces
- Primary constructors for DI
- Records for DTOs/events
- FluentValidation for input validation
- Result pattern for service layer (no exceptions in business logic)
