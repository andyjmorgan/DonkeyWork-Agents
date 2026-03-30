# Build stage — context: src/sandbox/
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Packages.props for central package management
COPY Directory.Packages.props ./

# Copy NuGet config if present (CI uses local proxy)
COPY nuget.config* ./

# Copy project files and restore dependencies
COPY CodeSandbox.Contracts/CodeSandbox.Contracts.csproj CodeSandbox.Contracts/
COPY CodeSandbox.Executor/CodeSandbox.Executor.csproj CodeSandbox.Executor/
RUN dotnet restore "CodeSandbox.Executor/CodeSandbox.Executor.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/CodeSandbox.Executor
RUN dotnet build "CodeSandbox.Executor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "CodeSandbox.Executor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - pre-built base with Python, Node.js, Git, GH CLI
FROM --platform=linux/amd64 your-registry/donkeywork-agents/codesandbox-executor-base:latest

COPY --from=publish --chown=sandbox:sandbox /app/publish .

USER sandbox

EXPOSE 8666
ENTRYPOINT ["dotnet", "CodeSandbox.Executor.dll"]
