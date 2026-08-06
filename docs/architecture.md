# Architecture — inventory-service

## Overview
Clean Architecture with 4 layers:
1. **Domain** — entities, enums, domain events
2. **Application** — interfaces, services, validators
3. **Infrastructure** — EF Core, Kafka, Redis implementations
4. **Api/Worker** — HTTP endpoints / background consumer

## Data Flow
```
HTTP Request → API Endpoints → InventoryService → Repository → EF Core → PostgreSQL
Kafka Event → Worker → OrderEventConsumer → InventoryService → same path
```

## Event-Driven Pattern
- Order events consumed from `inv.order-events` topic
- Stock reservation/depletion published via Transactional Outbox
- Idempotent consumers absorb redeliveries

## K8s Deployment
- API: HPA (CPU/memory based)
- Worker: KEDA (Kafka consumer-lag based)
