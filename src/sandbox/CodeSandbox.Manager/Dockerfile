# Build stage
# Build context: src/sandbox/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Packages.props for central package management
COPY Directory.Packages.props ./

# Copy csproj files and restore dependencies
COPY CodeSandbox.Manager/CodeSandbox.Manager.csproj CodeSandbox.Manager/
COPY CodeSandbox.Contracts/CodeSandbox.Contracts.csproj CodeSandbox.Contracts/
RUN dotnet restore "CodeSandbox.Manager/CodeSandbox.Manager.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/CodeSandbox.Manager
RUN dotnet build "CodeSandbox.Manager.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodeSandbox.Manager.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user with non-conflicting IDs
RUN groupadd --system --gid 10000 appuser \
    && useradd --system --uid 10000 --gid 10000 --home-dir /app appuser

# Copy published app
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8668

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CodeSandbox.Manager.dll"]
