FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and all project files for restore caching
COPY *.sln ./

# Main API
COPY src/DonkeyWork.Agents.Api/*.csproj src/DonkeyWork.Agents.Api/

# Common
COPY src/common/DonkeyWork.Agents.Common.Contracts/*.csproj src/common/DonkeyWork.Agents.Common.Contracts/
COPY src/common/DonkeyWork.Agents.Persistence/*.csproj src/common/DonkeyWork.Agents.Persistence/

# Credentials
COPY src/credentials/DonkeyWork.Agents.Credentials.Api/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Api/
COPY src/credentials/DonkeyWork.Agents.Credentials.Contracts/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Contracts/
COPY src/credentials/DonkeyWork.Agents.Credentials.Core/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Core/

# Identity
COPY src/identity/DonkeyWork.Agents.Identity.Api/*.csproj src/identity/DonkeyWork.Agents.Identity.Api/
COPY src/identity/DonkeyWork.Agents.Identity.Contracts/*.csproj src/identity/DonkeyWork.Agents.Identity.Contracts/
COPY src/identity/DonkeyWork.Agents.Identity.Core/*.csproj src/identity/DonkeyWork.Agents.Identity.Core/

# Storage
COPY src/storage/DonkeyWork.Agents.Storage.Api/*.csproj src/storage/DonkeyWork.Agents.Storage.Api/
COPY src/storage/DonkeyWork.Agents.Storage.Contracts/*.csproj src/storage/DonkeyWork.Agents.Storage.Contracts/
COPY src/storage/DonkeyWork.Agents.Storage.Core/*.csproj src/storage/DonkeyWork.Agents.Storage.Core/

# Providers
COPY src/providers/DonkeyWork.Agents.Providers.Api/*.csproj src/providers/DonkeyWork.Agents.Providers.Api/
COPY src/providers/DonkeyWork.Agents.Providers.Contracts/*.csproj src/providers/DonkeyWork.Agents.Providers.Contracts/
COPY src/providers/DonkeyWork.Agents.Providers.Core/*.csproj src/providers/DonkeyWork.Agents.Providers.Core/

# Agents
COPY src/agents/DonkeyWork.Agents.Agents.Api/*.csproj src/agents/DonkeyWork.Agents.Agents.Api/
COPY src/agents/DonkeyWork.Agents.Agents.Contracts/*.csproj src/agents/DonkeyWork.Agents.Agents.Contracts/
COPY src/agents/DonkeyWork.Agents.Agents.Core/*.csproj src/agents/DonkeyWork.Agents.Agents.Core/

# Actions
COPY src/actions/DonkeyWork.Agents.Actions.Api/*.csproj src/actions/DonkeyWork.Agents.Actions.Api/
COPY src/actions/DonkeyWork.Agents.Actions.Contracts/*.csproj src/actions/DonkeyWork.Agents.Actions.Contracts/
COPY src/actions/DonkeyWork.Agents.Actions.Core/*.csproj src/actions/DonkeyWork.Agents.Actions.Core/

# Projects
COPY src/projects/DonkeyWork.Agents.Projects.Api/*.csproj src/projects/DonkeyWork.Agents.Projects.Api/
COPY src/projects/DonkeyWork.Agents.Projects.Contracts/*.csproj src/projects/DonkeyWork.Agents.Projects.Contracts/
COPY src/projects/DonkeyWork.Agents.Projects.Core/*.csproj src/projects/DonkeyWork.Agents.Projects.Core/

# Restore dependencies (disable parallel to avoid hanging, use HTTP/1.1)
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
RUN dotnet restore src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj --disable-parallel --verbosity minimal

# Copy source code
COPY src/ src/

# Build
RUN dotnet build src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DonkeyWork.Agents.Api.dll"]
