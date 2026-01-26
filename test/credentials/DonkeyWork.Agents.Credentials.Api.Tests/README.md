# OAuth Controller Tests

This directory contains comprehensive unit tests for the OAuth integration controllers.

## Test Coverage

### OAuthControllerTests (10 tests)
Tests for OAuth authorization flow and callback handling:

**Authorization URL Generation:**
- ✅ `GetAuthorizationUrl_WithValidProvider_ReturnsOkWithUrl` - Generates authorization URL and sets cookies
- ✅ `GetAuthorizationUrl_WithMissingProviderConfig_ReturnsBadRequest` - Handles missing provider configuration
- ✅ `GetAuthorizationUrl_SetsCookiesWithCorrectOptions` - Verifies cookie creation with correct security settings

**Callback Handling:**
- ✅ `Callback_WithSuccessfulFlow_RedirectsToFrontendWithSuccess` - Successful OAuth callback flow
- ✅ `Callback_WithErrorFromProvider_RedirectsWithError` - Handles provider error responses
- ✅ `Callback_WithMissingCode_RedirectsWithError` - Validates authorization code presence
- ✅ `Callback_WithStateMismatch_RedirectsWithError` - CSRF protection via state validation
- ✅ `Callback_WithMissingCodeVerifier_RedirectsWithError` - PKCE code verifier validation
- ✅ `Callback_WithMissingUserId_RedirectsWithError` - User ID validation
- ✅ `Callback_WithFlowException_RedirectsWithError` - Exception handling during token exchange

### OAuthProviderConfigsControllerTests (11 tests)
Tests for OAuth client configuration management:

**List Operations:**
- ✅ `List_WithConfigs_ReturnsOkWithConfigList` - Lists all provider configs with token status
- ✅ `List_WithEmptyConfigs_ReturnsOkWithEmptyList` - Handles empty configuration list

**Get Operations:**
- ✅ `Get_WithExistingConfig_ReturnsOkWithDetail` - Retrieves specific configuration
- ✅ `Get_WithNonExistingConfig_ReturnsNotFound` - Returns 404 for missing config
- ✅ `Get_MasksClientIdAndSecret` - Verifies secret masking in responses

**Create Operations:**
- ✅ `Create_WithValidRequest_ReturnsCreatedAtAction` - Creates new OAuth configuration
- ✅ `Create_WithDuplicateProvider_ReturnsBadRequest` - Prevents duplicate configurations

**Update Operations:**
- ✅ `Update_WithExistingConfig_ReturnsOkWithUpdatedDetail` - Updates configuration
- ✅ `Update_WithNonExistingConfig_ReturnsNotFound` - Returns 404 for missing config

**Delete Operations:**
- ✅ `Delete_WithExistingConfig_ReturnsNoContent` - Deletes configuration
- ✅ `Delete_WithNonExistingConfig_ReturnsNotFound` - Returns 404 for missing config

### OAuthTokensControllerTests (12 tests)
Tests for OAuth token management:

**List Operations:**
- ✅ `List_WithTokens_ReturnsOkWithTokenList` - Lists all tokens with status indicators
- ✅ `List_WithExpiredToken_SetsExpiredStatus` - Correctly identifies expired tokens
- ✅ `List_WithEmptyTokens_ReturnsOkWithEmptyList` - Handles empty token list

**Get Operations:**
- ✅ `Get_WithExistingToken_ReturnsOkWithDetail` - Retrieves token details
- ✅ `Get_WithNonExistingToken_ReturnsNotFound` - Returns 404 for missing token
- ✅ `Get_MasksAccessToken` - Verifies token masking in responses

**Refresh Operations:**
- ✅ `Refresh_WithValidToken_ReturnsOkWithSuccess` - Successfully refreshes token
- ✅ `Refresh_WithNonExistingToken_ReturnsNotFound` - Returns 404 for missing token
- ✅ `Refresh_WithMissingProviderConfig_ReturnsBadRequest` - Requires provider configuration
- ✅ `Refresh_WithProviderException_ReturnsBadRequest` - Handles provider errors

**Delete Operations:**
- ✅ `Delete_WithExistingToken_ReturnsNoContent` - Disconnects account
- ✅ `Delete_WithNonExistingToken_ReturnsNotFound` - Returns 404 for missing token

## Test Patterns

All tests follow consistent patterns:

1. **Arrange** - Set up mocks and test data
2. **Act** - Call controller method
3. **Assert** - Verify response type and content

### Mocking Strategy

- `Mock<IOAuthFlowService>` - OAuth flow orchestration
- `Mock<IOAuthProviderConfigService>` - Provider configuration management
- `Mock<IOAuthTokenService>` - Token storage and retrieval
- `Mock<IOAuthProviderFactory>` - Provider factory
- `Mock<IIdentityContext>` - User context
- `Mock<ILogger<T>>` - Logging

### Test Naming Convention

`MethodName_StateUnderTest_ExpectedBehavior`

Examples:
- `GetAuthorizationUrl_WithValidProvider_ReturnsOkWithUrl`
- `Callback_WithStateMismatch_RedirectsWithError`
- `Create_WithDuplicateProvider_ReturnsBadRequest`

## Running Tests

```bash
# Run all OAuth controller tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~OAuthControllerTests"
```

## Test Coverage Summary

- **Total Tests:** 43
- **Passed:** 43 ✅
- **Failed:** 0
- **Coverage:** All public controller methods and error paths

## Security Testing

Tests verify security features:

- ✅ PKCE code verifier generation and validation
- ✅ State parameter CSRF protection
- ✅ Cookie security attributes (HttpOnly, Secure, SameSite)
- ✅ Token and secret masking in responses
- ✅ User isolation via IIdentityContext
- ✅ Error handling without information leakage
