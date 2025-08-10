# Educational Security Demo - Hardened Container
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base

# Create non-privileged user for security
RUN adduser -D -s /bin/sh -u 1001 appuser

WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["VulnerableLogin.csproj", "VulnerableLogin/"]
RUN dotnet restore "VulnerableLogin/VulnerableLogin.csproj"

# Copy source code and build
COPY . VulnerableLogin/
WORKDIR "/src/VulnerableLogin"
RUN dotnet build "./VulnerableLogin.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VulnerableLogin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage with security hardening
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create directory for session storage (if needed)
RUN mkdir -p /tmp/sessions && chown appuser:appuser /tmp/sessions

# Switch to non-privileged user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/Auth/Login || exit 1

ENTRYPOINT ["dotnet", "VulnerableLogin.dll"]
