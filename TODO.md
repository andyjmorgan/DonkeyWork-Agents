# TODO

## Tasks

*No open tasks*

---

## Bugs

### Token refresh not working - users getting logged out on token expiry

**Priority:** High

**Symptom:** User gets logged out when access token expires, despite auto-refresh system in place.

**Current implementation:**
- `useTokenRefresh` hook checks every 60s
- `shouldRefreshToken()` returns true when <2 min remaining
- `refreshTokens()` calls `/api/v1/auth/refresh`
- Also refreshes on tab focus/visibility change

**Possible causes:**
1. Refresh token itself expired (Keycloak session timeout shorter than expected)
2. `/api/v1/auth/refresh` endpoint failing silently
3. Race condition - token expires between check interval and next API call
4. `useTokenRefresh` hook not mounted or unmounting unexpectedly
5. Keycloak refresh token rotation causing issues

**Files to investigate:**
- `src/frontend/src/hooks/useTokenRefresh.ts`
- `src/frontend/src/store/auth.ts`
- `src/frontend/src/lib/api-client.ts` (401 handling)
- `src/identity/DonkeyWork.Agents.Identity.Api/Controllers/AuthController.cs` (refresh endpoint)

**Next steps:**
- Add logging to refresh flow to see what's happening
- Check Keycloak refresh token lifespan settings
- Verify refresh endpoint is being called and succeeding
