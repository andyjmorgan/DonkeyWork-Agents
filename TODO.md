# TODO

## Tasks

### Expression Engine Phase 4 — Execution Infrastructure

**Priority:** Medium

The expression engine (Scriban-based) has core infrastructure complete (44 tests passing), but cannot yet be used in workflow execution. See `EXPRESSION_ENGINE_STATUS.md` for full details.

**Missing components:**
1. **Action Executor/Dispatcher** — Service to discover and execute `[ActionProvider]` classes via assembly scanning
2. **Action Node Executor** — `INodeExecutor` implementation for action nodes in the orchestration engine
3. **Dynamic Context Building** — Helper to convert `ExecutionContext` → Scriban context (`steps`, `input`, `execution_id`, `user_id`)
4. **Action Provider Registration** — DI registration of `[ActionProvider]` classes
5. **Expression Variables UI** (optional) — Frontend variable picker, autocomplete, syntax validation

**Blocked by this:** Cannot execute action nodes in workflows or reference previous step outputs in expressions (`{{ Steps.step1.result }}`).

### Inconsistent API Error Response Schema

**Priority:** Low

Error responses vary across modules:
- Some return `{ "error": "message" }`
- Others return `{ "message": "message" }`
- Validation errors have no standardized format

Consider adopting RFC 7807 Problem Details or a unified error envelope for iOS client consistency.

### Guardrails Middleware is a Stub

**Priority:** Low

`GuardrailsMiddleware` in both the orchestrations and actors middleware pipelines is a pass-through with no actual guardrail logic. This is a placeholder for future content safety/filtering.

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
