# Kubernetes Guide — inventory-service

## Quick Start (Kustomize)
```bash
# Dev
kubectl apply -k deploy/kustomize/overlays/dev

# Check pods
kubectl get pods -l app=inventory-api
```

## Helm
```bash
helm install inventory deploy/helm/inventory-service
```

## Strimzi Kafka
```bash
kubectl apply -f deploy/strimzi/kafka.yaml
kubectl apply -f deploy/strimzi/kafkatopic-order-events.yaml
kubectl apply -f deploy/strimzi/kafkatopic-stock-reserved.yaml
```

## KEDA (Worker auto-scaling)
```bash
kubectl apply -f deploy/keda/scaled-object.yaml
```

## Probes
- **Liveness**: `/health/live` — restarts pod if unhealthy
- **Readiness**: `/health/ready` — removes from service if not ready
- **Startup**: `/health/ready` — gives app time to initialize
