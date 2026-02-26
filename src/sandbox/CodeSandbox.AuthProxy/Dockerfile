# Build stage
# Build context: src/sandbox/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Packages.props for central package management
COPY Directory.Packages.props ./

# Copy project file and restore
COPY CodeSandbox.AuthProxy/CodeSandbox.AuthProxy.csproj CodeSandbox.AuthProxy/
RUN dotnet restore "CodeSandbox.AuthProxy/CodeSandbox.AuthProxy.csproj"

# Copy source and build
COPY CodeSandbox.AuthProxy/ CodeSandbox.AuthProxy/
WORKDIR /src/CodeSandbox.AuthProxy
RUN dotnet build "CodeSandbox.AuthProxy.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodeSandbox.AuthProxy.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Create non-root user
RUN groupadd --system --gid 10000 appuser \
    && useradd --system --uid 10000 --gid 10000 --home-dir /app appuser

# Copy published app
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

EXPOSE 8080 8081

ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8081/healthz || exit 1

ENTRYPOINT ["dotnet", "CodeSandbox.AuthProxy.dll"]
