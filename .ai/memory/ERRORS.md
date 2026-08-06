# Known Errors, Bugs, and Pitfalls — inventory-service

---

## Git History of Fixes

The repo has 11 commits. After the initial scaffold, 7 fix commits were needed to get CI green:

| Commit | Description |
|--------|-------------|
| `321eaf7` | `feat: initial scaffold — inventory-service (C#/.NET 10 + K8s)` |
| `bb68e32` | `fix(ci): fix whitespace formatting and remove unavailable kubeconform action` |
| `3439904` | `fix(docker): remove adduser from Dockerfile (not available in .NET 10 aspnet image)` |
| `7e7307e` | `fix(build): correct NuGet package references for .NET 10` |
| `af61d7e` | `fix(build): disable TreatWarningsAsErrors and suppress CA1848/CA1873` |
| `616dff3` | `fix(build): correct FluentValidation namespace and remove missing Cache reference` |
| `2ec73a3` | `fix(build): fix endpoint return types and remove WithOpenApi` |
| `f2856df` | `fix(build): resolve all CI build errors` |
| `c812805` | `fix(ci): skip Kustomization schema validation in kubeconform` |
| `08cd7e3` | `Merge pull request #1 from igorVilela7713/fix/ci-build-errors` |
| `6b02f66` | `fix(build): add AspNetCore.HealthChecks.Npgsql for AddNpgSql extension` |
| `9a04ab2` | `fix(worker): remove Serilog from Worker Program.cs` |

---

## Build / CI Errors Encountered and Fixed

### 1. Dockerfile `adduser` Not Available in .NET 10 aspnet Image

**Error:** `adduser` command not found in `mcr.microsoft.com/dotnet/aspnet:10.0` image.
**Fix:** Removed the `adduser` / `USER` directive from `Dockerfile`. The .NET 10 aspnet image runs as root by default (commit `3439904`).

### 2. NuGet Package References for .NET 10

**Error:** Several NuGet packages had wrong version ranges or were incompatible with `net10.0`.
**Fix:** Updated package references to .NET 10-compatible versions (commit `7e7307e`):
- `Npgsql.EntityFrameworkCore.PostgreSQL` → `10.*`
- `Microsoft.Extensions.Logging.Abstractions` → `10.*`
- `Microsoft.AspNetCore.Mvc.Testing` → `10.*`

### 3. TreatWarningsAsErrors Blocking CI

**Error:** CA1848 and CA1873 analyzer warnings treated as errors, failing the build.
**Fix:** Set `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` and added `<NoWarn>CA1848;CA1873</NoWarn>` in `Directory.Build.props` (commit `af61d7e`).

### 4. FluentValidation Namespace + Missing Cache Reference

**Error:** `FluentValidation` namespace incorrect; missing `Cache` package reference.
**Fix:** Corrected namespace imports and removed the stale Cache reference (commit `616dff3`).

### 5. Endpoint Return Types + WithOpenApi Removed

**Error:** Endpoint return type declarations incorrect; `WithOpenApi()` not available in current Swashbuckle version.
**Fix:** Fixed `IResult` return types and removed `WithOpenApi()` calls (commit `2ec73a3`).

### 6. kubeconform Schema Validation Skipped

**Error:** `kubeconform -strict` failed validating Kustomization CRD (not a standard K8s resource).
**Fix:** Added `-skip Kustomization` to kubeconform command in CI (commit `c812805`).

### 7. AspNetCore.HealthChecks.Npgsql Missing

**Error:** `AddNpgSql()` extension method not found — missing package reference.
**Fix:** Added `<PackageReference Include="AspNetCore.HealthChecks.Npgsql" Version="8.*" />` to `Inventory.Api.csproj` (commit `6b02f66`).

### 8. Worker Program.cs Serilog Dependency

**Error:** Worker `Program.cs` referenced Serilog but Worker project only has `Serilog.AspNetCore` (no `UseSerilog()` on `Host.CreateApplicationBuilder`).
**Fix:** Removed Serilog usage from Worker `Program.cs` (commit `9a04ab2`).

---

## Known Issues

### No TODO/FIXME Comments

No `TODO`, `FIXME`, `HACK`, `WARN`, or `BUG` comments were found in any `.cs` source files. The codebase is clean of inline debt markers.

### Worker Uses InMemory Transport (Placeholder)

**File:** `worker/Inventory.Worker/Program.cs` line 25
```csharp
x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
```
The MassTransit transport is configured as `UsingInMemory` instead of Kafka. This is a placeholder — the worker won't consume from real Kafka topics until this is changed to `UsingRabbitMq` or `UsingKafka` with proper configuration.

### No Result Pattern Implemented Yet

**File:** `src/Inventory.Application/Services/InventoryService.cs`
The service layer throws `InvalidOperationException` for business errors (product not found, insufficient stock). The planned `Result<T>` pattern is not yet implemented. This means exceptions are used for flow control, which is functional but not ideal for performance and composability.

### No Outbox Table or Relay

The Transactional Outbox pattern is described in SPEC.md and DECISIONS.md but has no implementation. Events are not currently persisted atomically with business data.

### No Kafka Topic Configuration in Code

MassTransit topic names (`inv.order-events`, `inv.stock-reserved`) are not configured in any C# code. The topics only exist as Strimzi CRDs in `deploy/strimzi/`. The Worker's InMemory transport doesn't reference them.

### Integration Tests Use InMemory Database

**File:** `tests/Inventory.IntegrationTests/StockApiTests.cs` line 24
```csharp
opts.UseInMemoryDatabase("TestDb")
```
Integration tests override DbContext with InMemory provider instead of using Testcontainers PostgreSQL. The Testcontainers packages are referenced but not yet wired up.

---

## K8s Validation Issues

### kubeconform Skips Kustomization CRD

kubeconform doesn't recognize the `kustomize.config.k8s.io/v1beta1` Kustomization CRD. CI uses `-skip Kustomization` to bypass this. The Kustomize resources (deployment, service, configmap, secret) are validated normally.

### Helm Chart Lacks Templates

`deploy/helm/inventory-service/` has `Chart.yaml` and `values.yaml` but no `templates/` directory. `helm lint` will pass but `helm install` will fail without templates. This is a known incomplete state.

---

## Runtime Issues

### Dockerfile HEALTHCHECK Uses curl

**File:** `Dockerfile` line 25
```dockerfile
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:8080/health/live || exit 1
```
The .NET 10 aspnet image doesn't include `curl` by default. This HEALTHCHECK may fail in production. Consider using `wget` or a custom health check binary, or relying solely on K8s probes.

### docker-compose Worker Has No Healthcheck

**File:** `docker-compose.yml`
The `worker` service has `depends_on` with `condition: service_healthy` for postgres and kafka, but the worker container itself has no `healthcheck`. This means Docker Compose won't know if the worker is actually healthy.

---

## Known Risks (from project context)

### 1. SDK Version Mismatch

Local machine has .NET 7 SDK. CI uses `dotnet-version: '10.x'`. Builds may pass in CI but fail locally if SDK 10 is not installed. Always verify with `dotnet --version`.

### 2. Testcontainers Depends on Docker

Testcontainers requires a running Docker daemon. On Windows, this means Docker Desktop with WSL2 backend (not Hyper-V). If Docker is not running, integration tests will fail.

### 3. Kafka/Testcontainers Memory Usage

Full Kafka + PostgreSQL + Redis Testcontainers can consume significant memory (2-4 GB). Consider using Redpanda as a lightweight Kafka replacement for tests.

### 4. Outbox Duplicate/Race Conditions

The Transactional Outbox pattern (planned) needs careful implementation:
- Separate outbox table with unique message ID
- Concurrency control (optimistic locking or SELECT FOR UPDATE)
- Relay process with at-least-once delivery
- Consumers must be idempotent

### 5. Strimzi/KEDA Require CRDs

The Strimzi and KEDA manifests (`deploy/strimzi/`, `deploy/keda/`) require the respective operators to be installed in the cluster. These won't work on a vanilla Kubernetes installation.

### 6. EF Core Migrations Tooling

EF Core migrations require `dotnet-ef` tool:
```bash
dotnet tool install --global dotnet-ef
```
This is not included in the project scaffold. Migrations must be created and applied manually.

### 7. Secrets Must Not Be Committed

Connection strings in `docker-compose.yml` are for local development only:
```
Host=postgres;Database=inventory;Username=postgres;Password=postgres
```
Production secrets must use K8s Secrets (see `deploy/kustomize/base/secret.yaml`) or external secret managers.

### 8. Stack Version Alignment

The following versions must be compatible:
- MassTransit 9.* (Kafka transport)
- Confluent.Kafka (transitive via MassTransit.Kafka)
- EF Core 10.* + Npgsql 10.*
- .NET 10 runtime
- Kafka 3.7 (Strimzi)

Version drift in any of these can cause runtime failures.

### 9. Windows/git-bash Path Issues

CI runs on `ubuntu-latest`. Local development is on Windows with git-bash. Path separators (`/` vs `\`) in scripts and `Directory.Build.props` are handled by MSBuild, but Makefile and shell scripts may need attention.

### 10. Idempotency for Retry/Redelivery

Kafka consumers may receive duplicate messages. The `OrderEventConsumer` does not currently implement idempotency. If `ReserveAsync` is called twice for the same order, it may create duplicate reservations or fail with a constraint violation. Idempotency keys or deduplication tables are needed.

---

## Error Handling Pattern (Planned)

The intended error handling flow:

```
FluentValidation invalid → Results.ValidationProblem() → HTTP 400
Service layer throws → ProblemDetails middleware → HTTP 4xx/5xx
Domain errors → Result<T>.Failure(errorCode) → HTTP 4xx
```

Currently:
- FluentValidation works (returns `ValidationProblem`)
- Service layer throws `InvalidOperationException` (no ProblemDetails middleware yet)
- No `Result<T>` pattern implemented
- No global exception handler middleware

---

## CI Pipeline Summary

The CI pipeline (`.github/workflows/ci.yml`) has 4 jobs:

| Job | Trigger | Steps |
|-----|---------|-------|
| `build` | push/PR to main | checkout → setup .NET 10.x → `dotnet build -c Release` → `dotnet test -c Release --collect:"XPlat Code Coverage"` → upload coverage |
| `lint` | push/PR to main | checkout → setup .NET 10.x → `dotnet format --verify-no-changes` |
| `docker` | main only, after build | checkout → `docker build -t inventory-service:$SHA .` |
| `k8s` | push/PR to main | checkout → download kubeconform → validate Kustomize base → download helm → lint Helm chart |

All 4 jobs must pass for PR merge. The `docker` job only runs on `main` branch (not PRs).
