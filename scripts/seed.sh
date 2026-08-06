#!/bin/bash
set -e

API_URL=\${API_URL:-http://localhost:8080}

echo "Seeding inventory data..."

curl -s -X POST "$API_URL/api/v1/stock" -H 'Content-Type: application/json' \
  -d '{"productId":"PROD-001","productName":"Widget Pro","quantity":100}'

curl -s -X POST "$API_URL/api/v1/stock" -H 'Content-Type: application/json' \
  -d '{"productId":"PROD-002","productName":"Gadget Plus","quantity":50}'

curl -s -X POST "$API_URL/api/v1/stock" -H 'Content-Type: application/json' \
  -d '{"productId":"PROD-003","productName":"Component X","quantity":200}'

echo "Seed complete!"
