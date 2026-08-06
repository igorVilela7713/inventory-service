# Development Guide — inventory-service

## Prerequisites
- .NET 10 SDK (or Docker)
- Docker & Docker Compose

## Local Setup
```bash
# Start infra
docker compose up -d

# Run API
dotnet run --project src/Inventory.Api

# Run Worker
dotnet run --project worker/Inventory.Worker
```

## Running Tests
```bash
dotnet test
```

## Code Quality
```bash
dotnet format --verify-no-changes  # Check formatting
dotnet format                       # Auto-fix formatting
```
