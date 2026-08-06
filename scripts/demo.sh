#!/bin/bash
set -e

echo "=== inventory-service K8s Demo ==="

echo "1. Applying dev manifests..."
kubectl apply -k deploy/kustomize/overlays/dev

echo "2. Waiting for pods..."
kubectl wait --for=condition=ready pod -l app=inventory-api --timeout=120s

echo "3. Checking HPA..."
kubectl get hpa

echo "4. Checking KEDA ScaledObject..."
kubectl get scaledobject inventory-worker 2>/dev/null || echo "KEDA not installed"

echo "5. Health check..."
kubectl exec deploy/inventory-api -- curl -sf http://localhost:8080/health/ready

echo "Done!"
