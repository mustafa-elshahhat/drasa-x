# =============================================================================
# backend — local development container image
# Multi-stage build: restore/publish with the .NET 9 SDK, run on the ASP.NET 9
# runtime. Configuration (connection string, JWT key, CORS) is provided through
# environment variables at run time; no secret is baked into the image.
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first (better layer caching).
COPY DerasaX.sln ./
COPY src/DerasaX.Api/DerasaX.Api.csproj src/DerasaX.Api/
COPY src/DerasaX.Application/DerasaX.Application.csproj src/DerasaX.Application/
COPY src/DerasaX.Domain/DerasaX.Domain.csproj src/DerasaX.Domain/
COPY src/DerasaX.Infrastructure/DerasaX.Infrastructure.csproj src/DerasaX.Infrastructure/
RUN dotnet restore src/DerasaX.Api/DerasaX.Api.csproj

# Copy the rest and publish.
COPY . .
RUN dotnet publish src/DerasaX.Api/DerasaX.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl is used by the container/compose healthcheck only.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# Container listens on 8080 (mapped to a host port by docker-compose).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Phase 18 — run as the image's built-in non-root user (UID 1654). The app listens
# on an unprivileged port (8080) and bakes in no secret, so root is unnecessary.
USER $APP_UID

ENTRYPOINT ["dotnet", "DerasaX.Api.dll"]
