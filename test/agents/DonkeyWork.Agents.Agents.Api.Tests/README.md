# Agents API Integration Tests

Comprehensive integration tests for the Agents CRUD API and version management.

## Test Structure

### Controllers Tested
- **AgentsController** - Agent CRUD operations (14 tests)
- **AgentVersionsController** - Draft/publish workflow (15 tests)

### Test Helpers
- **AgentsApiFactory** - WebApplicationFactory with mocked IIdentityContext and Serilog suppression
- **AgentsApiCollection** - Shared test collection to ensure single factory instance
- **TestDataBuilder** - Helper methods for creating test data

## AgentsController Tests (14 tests)

### ✅ Create Agent
- `Create_WithValidRequest_ReturnsCreatedAgent` - Happy path agent creation
- `Create_WithInvalidName_ReturnsBadRequest` - Validation: invalid name format
- `Create_WithEmptyName_ReturnsBadRequest` - Validation: empty name

### ✅ Get Agent
- `Get_WithExistingAgent_ReturnsAgent` - Happy path retrieval
- `Get_WithNonExistentAgent_ReturnsNotFound` - 404 for missing agent

### ✅ List Agents
- `List_WithMultipleAgents_ReturnsAllUserAgents` - Returns user's agents
- `List_WithNoAgents_ReturnsEmptyList` - Empty list when no agents

### ✅ Update Agent
- `Update_WithValidRequest_ReturnsUpdatedAgent` - Happy path update
- `Update_WithNonExistentAgent_ReturnsNotFound` - 404 for missing agent
- `Update_WithInvalidName_ReturnsBadRequest` - Validation: invalid name

### ✅ Delete Agent
- `Delete_WithExistingAgent_ReturnsNoContent` - Happy path deletion
- `Delete_WithNonExistentAgent_ReturnsNotFound` - 404 for missing agent
- `Delete_CascadesVersionDeletion` - Cascade delete of versions

### ✅ Data Integrity
- `CreateAndGet_RoundTrip_PreservesAllData` - Data preservation test

## AgentVersionsController Tests (15 tests)

### ✅ Save Draft
- `SaveDraft_WithNewAgent_UpdatesExistingDraft` - Updates initial draft
- `SaveDraft_MultipleTimes_UpdatesSameVersion` - Idempotent draft updates
- `SaveDraft_WithNonExistentAgent_ReturnsNotFound` - 404 for missing agent

### ✅ Publish Workflow
- `Publish_WithDraftVersion_PublishesAndCreatesNewDraft` - Happy path publish
- `Publish_WithoutDraft_ReturnsNotFound` - 404 when no draft exists
- `Publish_WithNonExistentAgent_ReturnsNotFound` - 404 for missing agent
- `PublishWorkflow_SavePublishSave_CreatesNewDraftVersion` - Full workflow test

### ✅ Get Version
- `GetVersion_WithExistingVersion_ReturnsVersion` - Happy path retrieval
- `GetVersion_WithNonExistentVersion_ReturnsNotFound` - 404 for missing version

### ✅ List Versions
- `ListVersions_WithMultipleVersions_ReturnsAllVersionsDescending` - Sorted list
- `ListVersions_WithNewAgent_ReturnsInitialDraft` - Initial draft exists

### ✅ Data Preservation
- `SaveDraft_PreservesReactFlowDataStructure` - ReactFlow data integrity
- `SaveDraft_PreservesNodeConfigurations` - Node configs integrity
- `SaveDraft_PreservesInputSchema` - Input schema integrity

### ✅ Timestamps
- `VersionTimestamps_AreCorrect` - Validates created/published timestamps

## Running the Tests

```bash
# Run all tests
dotnet test test/agents/DonkeyWork.Agents.Agents.Api.Tests/

# Run with detailed output
dotnet test test/agents/DonkeyWork.Agents.Agents.Api.Tests/ --logger "console;verbosity=detailed"

# Run specific test
dotnet test test/agents/DonkeyWork.Agents.Agents.Api.Tests/ --filter "FullyQualifiedName~Create_WithValidRequest_ReturnsCreatedAgent"

# Run all AgentsController tests
dotnet test test/agents/DonkeyWork.Agents.Agents.Api.Tests/ --filter "FullyQualifiedName~AgentsControllerTests"

# Run all AgentVersionsController tests
dotnet test test/agents/DonkeyWork.Agents.Agents.Api.Tests/ --filter "FullyQualifiedName~AgentVersionsControllerTests"
```

## Test Coverage

### CRUD Operations
- ✅ Create
- ✅ Read (Get by ID)
- ✅ Read (List)
- ✅ Update
- ✅ Delete

### Version Management
- ✅ Save draft (create & update)
- ✅ Publish draft
- ✅ Get version
- ✅ List versions
- ✅ Version number incrementing
- ✅ Draft/published state management

### Edge Cases
- ✅ Non-existent resources (404)
- ✅ Invalid input validation (400)
- ✅ Cascade deletions
- ✅ Idempotent operations
- ✅ Timestamp correctness
- ✅ Data structure preservation

### Workflow Tests
- ✅ Full draft → publish → new draft workflow
- ✅ Multiple version history
- ✅ Round-trip data integrity

## Test Infrastructure

### WebApplicationFactory Configuration
- **AgentsApiFactory** extends `WebApplicationFactory<Program>` for integration testing
- **Serilog suppression**: Configured to avoid "logger is already frozen" errors by clearing logging providers
- **Test authentication**: Custom `TestAuthHandler` that auto-authenticates requests
- **Mocked IIdentityContext**: Provides consistent test user ID for all tests

### Shared Test Collection
- All test classes use `[Collection(nameof(AgentsApiCollection))]`
- Ensures single factory instance across all tests
- Prevents Serilog logger conflicts when tests run in parallel
- Tests within the collection run sequentially

## Notes

- Tests use **WebApplicationFactory** for true integration testing
- Uses **real PostgreSQL database** (same as production, isolated by test user ID)
- **IIdentityContext** is mocked for consistent test user ID
- All tests are **isolated** within their collection
- Tests follow xUnit best practices and naming conventions
- **Total: 29 tests** (14 AgentsController + 15 AgentVersionsController)
