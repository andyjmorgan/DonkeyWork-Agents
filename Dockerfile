FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/DonkeyWork.Agents.Api/*.csproj src/DonkeyWork.Agents.Api/
COPY src/common/DonkeyWork.Agents.Common.Contracts/*.csproj src/common/DonkeyWork.Agents.Common.Contracts/
COPY src/common/DonkeyWork.Agents.Persistence/*.csproj src/common/DonkeyWork.Agents.Persistence/
COPY src/credentials/DonkeyWork.Agents.Credentials.Api/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Api/
COPY src/credentials/DonkeyWork.Agents.Credentials.Contracts/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Contracts/
COPY src/credentials/DonkeyWork.Agents.Credentials.Core/*.csproj src/credentials/DonkeyWork.Agents.Credentials.Core/
COPY src/identity/DonkeyWork.Agents.Identity.Api/*.csproj src/identity/DonkeyWork.Agents.Identity.Api/
COPY src/identity/DonkeyWork.Agents.Identity.Contracts/*.csproj src/identity/DonkeyWork.Agents.Identity.Contracts/
COPY src/identity/DonkeyWork.Agents.Identity.Core/*.csproj src/identity/DonkeyWork.Agents.Identity.Core/
COPY src/storage/DonkeyWork.Agents.Storage.Api/*.csproj src/storage/DonkeyWork.Agents.Storage.Api/
COPY src/storage/DonkeyWork.Agents.Storage.Contracts/*.csproj src/storage/DonkeyWork.Agents.Storage.Contracts/
COPY src/storage/DonkeyWork.Agents.Storage.Core/*.csproj src/storage/DonkeyWork.Agents.Storage.Core/

# Restore dependencies
RUN dotnet restore src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj

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
