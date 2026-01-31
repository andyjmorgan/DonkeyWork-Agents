# Integration Tests Documentation

This document describes the integration tests that need to be recreated using Testcontainers.

## Required Infrastructure

The integration tests require the following services:
- **PostgreSQL** (pgvector) - Database
- **RabbitMQ** with Stream plugin - Message broker

## Test Project: DonkeyWork.Agents.Agents.Api.Tests

### AgentsController Tests

Tests CRUD operations for Agents with real database persistence.

| Test Method | Description |
|-------------|-------------|
| `Create_WithValidRequest_ReturnsCreatedAgent` | Creates agent, verifies 201 response with Location header, checks all fields returned |
| `Create_WithInvalidName_ReturnsBadRequest` | Rejects invalid agent names (uppercase, special chars) |
| `Create_WithEmptyName_ReturnsBadRequest` | Rejects empty agent names |
| `Get_WithExistingAgent_ReturnsAgent` | Retrieves created agent by ID |
| `Get_WithNonExistentAgent_ReturnsNotFound` | Returns 404 for non-existent agent |
| `List_WithMultipleAgents_ReturnsAllUserAgents` | Lists all agents for authenticated user |
| `List_WithNoAgents_ReturnsEmptyList` | Returns empty list when no agents exist |
| `Update_WithValidRequest_ReturnsUpdatedAgent` | Updates agent name/description |
| `Update_WithNonExistentAgent_ReturnsNotFound` | Returns 404 for non-existent agent |
| `Update_WithInvalidName_ReturnsBadRequest` | Rejects invalid names on update |
| `Delete_WithExistingAgent_ReturnsNoContent` | Deletes agent, verifies 204 |
| `Delete_WithNonExistentAgent_ReturnsNotFound` | Returns 404 for non-existent agent |
| `Delete_CascadesVersionDeletion` | Verifies versions are deleted when agent is deleted |
| `CreateAndGet_RoundTrip_PreservesAllData` | Verifies data integrity through create/get cycle |

### AgentVersionsController Tests

Tests version management and draft/publish workflow.

| Test Method | Description |
|-------------|-------------|
| `SaveDraft_WithNewAgent_UpdatesExistingDraft` | Saves draft version, verifies version number is 1 |
| `SaveDraft_MultipleTimes_UpdatesSameVersion` | Multiple saves update same draft (no version increment) |
| `SaveDraft_WithNonExistentAgent_ReturnsNotFound` | Returns 404 for non-existent agent |
| `Publish_WithDraftVersion_PublishesAndCreatesNewDraft` | Publishes draft, updates agent's CurrentVersionId |
| `Publish_WithoutDraft_ReturnsNotFound` | Returns 404 when no draft exists |
| `Publish_WithNonExistentAgent_ReturnsNotFound` | Returns 404 for non-existent agent |
| `PublishWorkflow_SavePublishSave_CreatesNewDraftVersion` | Full workflow: publish v1, save v2, publish v2 |
| `GetVersion_WithExistingVersion_ReturnsVersion` | Retrieves specific version by ID |
| `GetVersion_WithNonExistentVersion_ReturnsNotFound` | Returns 404 for non-existent version |
| `ListVersions_WithMultipleVersions_ReturnsAllVersionsDescending` | Lists all versions in descending order |
| `ListVersions_WithNewAgent_ReturnsInitialDraft` | New agent has initial draft v1 |
| `SaveDraft_PreservesReactFlowDataStructure` | Verifies nodes/edges/viewport structure preserved |
| `SaveDraft_PreservesNodeConfigurations` | Verifies node configs preserved |
| `SaveDraft_PreservesInputSchema` | Verifies input schema preserved |
| `VersionTimestamps_AreCorrect` | Verifies CreatedAt and PublishedAt timestamps |

## Test Project: DonkeyWork.Agents.Actions.Core.Tests

### ActionExecutorService Tests (HTTP Integration)

Tests that make real HTTP calls to external services.

| Test Method | Description |
|-------------|-------------|
| `ExecuteAsync_WithHttpRequestProvider_ExecutesSuccessfully` | Makes real HTTP GET to httpbin.org/get, verifies 200 response |
| `ExecuteAsync_WithContext_PassesContextToProvider` | Makes real HTTP GET with context object |

**Note:** These tests should use WireMock or similar mock HTTP server instead of real external services.

## Test Data Builders

### TestDataBuilder (Agents)

Helper methods for creating test data:

- `CreateAgentRequest(name, description)` - Creates CreateAgentRequestV1
- `CreateSaveVersionRequest()` - Creates SaveAgentVersionRequestV1 with Start -> End template
- `CreateSaveVersionRequestWithModel(credentialId)` - Creates request with Model node

ReactFlow data structure for tests:
```json
{
  "nodes": [
    { "id": "...", "type": "start", "position": {...}, "data": {...} },
    { "id": "...", "type": "end", "position": {...}, "data": {...} }
  ],
  "edges": [
    { "id": "...", "source": "start-id", "target": "end-id" }
  ],
  "viewport": { "x": 0, "y": 0, "zoom": 1 }
}
```

## Authentication in Tests

Tests use a `TestAuthHandler` that:
- Always returns successful authentication
- Provides a fixed test user ID (Guid)
- Sets claims: NameIdentifier, Email, Name, Username

## WebApplicationFactory Configuration

The `AgentsApiFactory` class:
1. Resets Serilog logger before each test run
2. Suppresses verbose logging (Warning level only)
3. Configures test authentication scheme
4. Mocks `IIdentityContext` with fixed test user

## Migration to Testcontainers

When recreating these tests with Testcontainers:

1. Add Testcontainers NuGet packages:
   - `Testcontainers.PostgreSql`
   - `Testcontainers.RabbitMq`

2. Create a base fixture class that:
   - Starts PostgreSQL (pgvector image) container
   - Starts RabbitMQ (with stream plugin) container
   - Runs EF Core migrations
   - Provides connection strings to WebApplicationFactory

3. Use `IAsyncLifetime` for container lifecycle management

4. Consider using `Respawn` for database cleanup between tests

5. Replace httpbin.org calls with WireMock for HTTP mocking

## Environment Variables Needed

The application expects these configuration values:
- `Persistence__ConnectionString` - PostgreSQL connection
- `RabbitMqStream__Host`, `Port`, `Username`, `Password` - RabbitMQ connection
- `Keycloak__Authority`, `Audience` - Auth config (can be mocked)
- `Storage__ServiceUrl` - SeaweedFS (not needed for agent tests)
