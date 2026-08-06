# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY *.sln Directory.Build.props ./
COPY src/Inventory.Domain/ src/Inventory.Domain/
COPY src/Inventory.Application/ src/Inventory.Application/
COPY src/Inventory.Infrastructure/ src/Inventory.Infrastructure/
COPY src/Inventory.Api/ src/Inventory.Api/

RUN dotnet restore src/Inventory.Api/Inventory.Api.csproj
RUN dotnet publish src/Inventory.Api/Inventory.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD curl -f http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
