# MEMORY.md — Living Memory of the Repo

> **Working agreement (mandatory):** read this file before any task and append findings on completion. See `AGENTS.md` -> "Repository Memory System" and `docs/memory.md`.

## Project Summary
C#/.NET 10 inventory microservice that complements order-service (Java) in an event-driven stock reservation flow via Apache Kafka. Demonstrates Kubernetes (Helm, Kustomize, Strimzi, KEDA) with full observability (OpenTelemetry, Serilog, Prometheus).

## Stack
- .NET 10 (LTS), ASP.NET Core Minimal API, EF Core, PostgreSQL
- MassTransit + Kafka, Redis, FluentValidation, Polly
- OpenTelemetry, Serilog, Prometheus
- xUnit, FluentAssertions, Testcontainers
- Kubernetes: Helm, Kustomize, Strimzi, KEDA

## Architecture
Clean Architecture: Domain -> Application -> Infrastructure -> Api/Worker. Transactional outbox (EF Core). KEDA scales the worker by Kafka consumer lag.

## Conventions (quick reference)
- File-scoped namespaces.
- Primary constructors for dependency injection.
- Records for DTOs/events.
- FluentValidation for input validation.
- Result pattern for the service layer (no exceptions in business logic).

## Verified Commands (build / test / deploy)
| Step | Command | Notes |
|------|---------|-------|
| Build | `dotnet build` | all projects |
| API | `dotnet run --project src/Inventory.Api` | runs minimal API |
| Worker | `dotnet run --project worker/Inventory.Worker` | MassTransit consumer |
| Test | `dotnet test` | |
| Format check | `dotnet format --verify-no-changes` | |
| Docker | `docker build -t inventory-service .` | |
| Infra | `docker compose up -d` | PostgreSQL + Kafka + Redis |
| Helm lint | `helm lint deploy/helm/inventory-service` | |
| K8s validate | `kubeconform deploy/kustomize/base` | |

## Known Pitfalls (gotchas)
_(add entries here as they are discovered)_

## Architecture Decisions (ADRs)
_(add ADR entries here as they are made)_

## Lessons Learned
_(add lessons here)_

## Agent Memory Log
- **2026-08-06** — `memory-system`: Introduced the Repository Memory System. Updated `AGENTS.md`, `README.md` and `docs/memory.md` with the mandatory read/write flow; seeded this file with project facts.
