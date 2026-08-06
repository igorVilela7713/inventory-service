# Repository Memory System

This page documents the **Repository Memory System** and the **mandatory read/write flow** every AI agent (Hermes, Claude, Copilot, etc.) must follow when working in `inventory-service`.

## Why a memory system?

This is a .NET 10 microservice with Kafka, EF Core, PostgreSQL, Redis, and a Kubernetes deployment (Helm/Kustomize/Strimzi/KEDA). Many of its sharp edges — Docker SDK version constraints during `docker build`, Kafka topic ordering guarantees, Strimzi CR requirements, KEDA trigger wiring — are exactly the kind of tribal knowledge that is expensive to rediscover on every task. `MEMORY.md` is that living memory.

## Where it lives

- **Memory file:** [`MEMORY.md`](../MEMORY.md) at the repo root.
- **This doc:** `docs/memory.md`.
- **Agent contract:** `AGENTS.md` -> "Repository Memory System".

## The mandatory flow

### 1. READ (before any task)
Open `MEMORY.md` and read it in full before writing code, running a build, or opening a PR. Note the known pitfalls, reuse the verified commands, and honor recorded architecture decisions.

### 2. WRITE (after any task)
After completing a task (especially after fixing a bug, working around a pitfall, making an architecture decision, or validating a command), append to `## Agent Memory Log`:

```
- **YYYY-MM-DD** — `scope`: <one-line summary>.
  - **Learned:** <fact>.
  - **Where:** `<file>` + commit `<sha>` (or branch).
  - **Applies to:** <area/command/component>.
```

Newest entries go **first**.

## Commit policy
`MEMORY.md` must be committed — in its own atomic commit (`docs(memory): ...`) or with the task commit. Never leave it uncommitted.

## Repo quick reference

| Item | Detail |
|------|--------|
| Stack | .NET 10; ASP.NET Core Minimal API; EF Core; PostgreSQL; MassTransit+Kafka; Redis |
| Build | `dotnet build` |
| Test | `dotnet test` |
| Deploy | `docker build -t inventory-service .` / `docker compose up -d` / `helm install inventory deploy/helm/inventory-service` |
| Key files | `src/Inventory.Api/Program.cs`, `src/Inventory.Application/Services/InventoryService.cs`, `src/Inventory.Infrastructure/Messaging/OutboxDispatcher.cs` |
| Architecture | Clean Architecture Domain -> Application -> Infrastructure -> Api/Worker; transactional outbox; KEDA scales by consumer lag |

> The canonical, up-to-date version of every item lives in `MEMORY.md`. Treat this page as the explanation; treat `MEMORY.md` as the data.
