# Credentials Controller Tests

Unit tests for the credentials module controllers and services.

## Test Files

### OAuthControllerTests (9 tests)
Tests for OAuth authorization flow and callback handling:

**Authorization URL Generation:**
- `GetAuthorizationUrl_WithValidProvider_ReturnsOkWithUrl`
- `GetAuthorizationUrl_WithMissingProviderConfig_ReturnsBadRequest`

**Callback Handling:**
- `Callback_WithSuccessfulFlow_RedirectsToFrontendWithSuccess`
- `Callback_SetsIdentityContextFromState`
- `Callback_WithErrorFromProvider_RedirectsWithError`
- `Callback_WithMissingCode_RedirectsWithError`
- `Callback_WithInvalidState_RedirectsWithError`
- `Callback_WithProviderMismatch_RedirectsWithError`
- `Callback_WithFlowException_RedirectsWithError`

### OAuthProviderConfigsControllerTests (11 tests)
Tests for OAuth client configuration management:

- `List_WithConfigs_ReturnsOkWithConfigList`
- `List_WithEmptyConfigs_ReturnsOkWithEmptyList`
- `Get_WithExistingConfig_ReturnsOkWithDetail`
- `Get_WithNonExistingConfig_ReturnsNotFound`
- `Get_MasksClientIdAndSecret`
- `Create_WithValidRequest_ReturnsCreatedAtAction`
- `Create_WithDuplicateProvider_ReturnsBadRequest`
- `Update_WithExistingConfig_ReturnsOkWithUpdatedDetail`
- `Update_WithNonExistingConfig_ReturnsNotFound`
- `Delete_WithExistingConfig_ReturnsNoContent`
- `Delete_WithNonExistingConfig_ReturnsNotFound`

### OAuthTokensControllerTests (16 tests)
Tests for OAuth token management:

- `List_WithTokens_ReturnsOkWithTokenList`
- `List_WithExpiredToken_SetsExpiredStatus`
- `List_WithEmptyTokens_ReturnsOkWithEmptyList`
- `List_SetsCanRefreshBasedOnRefreshToken`
- `Get_WithExistingToken_ReturnsOkWithDetail`
- `Get_WithNonExistingToken_ReturnsNotFound`
- `Get_MasksAccessToken`
- `GetAccessToken_WithExistingToken_ReturnsOkWithUnmaskedToken`
- `GetAccessToken_WithNonExistingToken_ReturnsNotFound`
- `GetAccessToken_DoesNotIncludeRefreshToken`
- `Refresh_WithValidToken_ReturnsOkWithSuccess`
- `Refresh_WithNonExistingToken_ReturnsNotFound`
- `Refresh_WithMissingProviderConfig_ReturnsBadRequest`
- `Refresh_WithProviderException_ReturnsBadRequest`
- `Delete_WithExistingToken_ReturnsNoContent`
- `Delete_WithNonExistingToken_ReturnsNotFound`

### ApiKeysControllerTests (10 tests)
Tests for user API key management.

### SandboxCredentialMappingsControllerTests (17 tests)
Tests for sandbox credential mapping CRUD operations.

### AvailableCredentialsControllerTests (3 tests)
Tests for listing available credentials.

### CredentialStoreGrpcServiceTests (2 tests)
Tests for the gRPC credential store service.

## Mocking Strategy

Each test class mocks only its controller's direct dependencies:

- **OAuthControllerTests**: `IOAuthFlowService`, `IIdentityContext`, `ILogger<OAuthController>`
- **OAuthProviderConfigsControllerTests**: `IOAuthProviderConfigService`, `ILogger<OAuthProviderConfigsController>`
- **OAuthTokensControllerTests**: `IOAuthTokenService`, `IOAuthProviderConfigService`, `IOAuthProviderFactory`, `IIdentityContext`, `ILogger<OAuthTokensController>`

## Running Tests

```bash
dotnet test test/credentials/DonkeyWork.Agents.Credentials.Api.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~OAuthControllerTests"
```
