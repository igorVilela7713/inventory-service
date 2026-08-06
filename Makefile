.PHONY: build test lint docker-build infra-up infra-down k8s-apply helm-install

build:
	dotnet build

test:
	dotnet test

lint:
	dotnet format --verify-no-changes

docker-build:
	docker build -t inventory-service .

infra-up:
	docker compose up -d

infra-down:
	docker compose down -v

k8s-apply:
	kubectl apply -k deploy/kustomize/overlays/dev

helm-install:
	helm install inventory deploy/helm/inventory-service

clean:
	dotnet clean
	find . -type d -name bin -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name obj -exec rm -rf {} + 2>/dev/null || true
