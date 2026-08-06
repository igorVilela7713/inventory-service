# API Reference — inventory-service

Base URL: `http://localhost:8080`

## Stock

### List Stock
```
GET /api/v1/stock?page=0&size=20
```

### Get Stock by Product
```
GET /api/v1/stock/{productId}
```

### Create Stock
```
POST /api/v1/stock
Content-Type: application/json

{
  "productId": "PROD-001",
  "productName": "Widget Pro",
  "quantity": 100
}
```

### Adjust Stock
```
POST /api/v1/stock/{productId}/adjust
Content-Type: application/json

{ "delta": -10 }
```

## Reservations

### Create Reservation
```
POST /api/v1/reservations
Content-Type: application/json

{
  "orderId": "ORD-001",
  "productId": "PROD-001",
  "quantity": 5
}
```

### Cancel Reservation
```
DELETE /api/v1/reservations/{id}
```

## Health

```
GET /health/live    → 200 (always, for K8s liveness)
GET /health/ready   → 200 (when DB is reachable)
```

## Metrics

```
GET /metrics  → Prometheus format
```
