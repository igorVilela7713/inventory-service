# PLAN — inventory-service

## Phase 1: Scaffold & Foundation
- [x] Solution structure + Directory.Build.props
- [x] README, SPEC, AGENTS, PLAN
- [x] Dockerfile multi-stage + docker-compose.yml
- [x] CI (build/test/lint/docker)
- [ ] Domain layer (StockItem, Reservation, domain events)
- [ ] Application layer (interfaces, use-cases)

## Phase 2: Persistence
- [ ] EF Core + Npgsql
- [ ] PostgreSQL migrations (Flyway-style or EF migrations)
- [ ] Repository implementations
- [ ] Unit tests

## Phase 3: API (Minimal)
- [ ] /api/v1/stock endpoints
- [ ] /api/v1/reservations endpoints
- [ ] FluentValidation
- [ ] JWT/API key auth
- [ ] ProblemDetails error handling
- [ ] docs/api.md

## Phase 4: Events (Kafka + Outbox)
- [ ] MassTransit + Kafka configuration
- [ ] Transactional Outbox pattern
- [ ] Inventory.Worker (consume inv.order-events)
- [ ] Idempotency in consumers
- [ ] E2E tests with Testcontainers

## Phase 5: Resilience + Cache + Observability
- [ ] Redis cache (read-through)
- [ ] Polly (retry + circuit breaker)
- [ ] OpenTelemetry (OTLP export)
- [ ] Serilog structured logging
- [ ] /metrics endpoint (Prometheus)
- [ ] Health checks (/health/live, /health/ready)

## Phase 6: Kubernetes (Manifests + Kustomize)
- [ ] Base manifests (deployment, service, ingress, configmap, secret)
- [ ] Overlays (dev/staging/prod)
- [ ] Readiness/liveness/startup probes
- [ ] HPA (CPU/memory for API)
- [ ] Postgres subchart or external
- [ ] docs/k8s.md

## Phase 7: Kubernetes (Strimzi + KEDA + Helm)
- [ ] Strimzi Kafka CR + topics + KafkaUser
- [ ] Helm chart (values, templates, NOTES.txt)
- [ ] KEDA ScaledObject (worker, Kafka consumer-lag metric)
- [ ] Demo script (stress, kill pod, observe HPA/KEDA)

## Phase 8: Polish
- [ ] Grafana dashboards
- [ ] scripts/demo.sh (stress + kill + rollback)
- [ ] Coverage badge
- [ ] README final review
