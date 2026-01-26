# Frontend - DonkeyWork Agents

## Tech Stack

- **Framework**: React 19 + Vite + TypeScript
- **Styling**: Tailwind CSS + shadcn/ui + CSS Variables (HSL)
- **State Management**: Zustand (with persist middleware)
- **Workflow Editor**: ReactFlow
- **Icons**: lucide-react
- **Authentication**: Keycloak JWT
- **API Client**: Auto-generated from backend OpenAPI spec

## Design Principles

- **Mobile First**: Design for mobile, enhance for desktop
- **Dark Mode Default**: Theme toggle with localStorage persistence
- **Copy patterns from**: `/Users/andrewmorgan/Personal/source/DonkeyWork-CodeSandbox-Manager/frontend`
- **Vibe**: Fun and vibrant, casual speak, but no emojis

---

## Project Structure

```
src/
├── components/
│   ├── layout/
│   │   ├── AppLayout.tsx         # Main layout with sidebar
│   │   ├── Sidebar.tsx           # Left navigation menu
│   │   ├── Header.tsx            # Top header (mobile menu toggle, user, theme)
│   │   └── ThemeToggle.tsx       # Dark/light mode switcher
│   ├── auth/
│   │   ├── AuthProvider.tsx      # Keycloak context provider
│   │   ├── ProtectedRoute.tsx    # Route guard for authenticated routes
│   │   └── LoginRedirect.tsx     # Login landing page
│   ├── agents/
│   │   ├── AgentList.tsx
│   │   ├── AgentCard.tsx
│   │   └── CreateAgentDialog.tsx
│   ├── editor/
│   │   ├── WorkflowEditor.tsx    # Main editor container
│   │   ├── Canvas.tsx            # ReactFlow wrapper
│   │   ├── NodePalette.tsx       # Draggable node types
│   │   ├── PropertiesPanel.tsx   # Selected node configuration
│   │   └── nodes/
│   │       ├── StartNode.tsx
│   │       ├── ModelNode.tsx
│   │       └── EndNode.tsx
│   ├── execution/
│   │   ├── TestPanel.tsx         # Input form + execute button
│   │   ├── StreamingOutput.tsx   # Real-time SSE event display
│   │   └── ExecutionHistory.tsx
│   ├── credentials/
│   │   ├── CredentialList.tsx
│   │   ├── CredentialCard.tsx
│   │   └── CreateCredentialDialog.tsx
│   └── ui/                       # shadcn/ui components
├── hooks/
│   ├── useAuth.ts                # Keycloak auth hook
│   ├── useApi.ts                 # API client hook with auth
│   └── useExecutionStream.ts     # SSE streaming hook
├── lib/
│   ├── utils.ts                  # cn() helper
│   ├── api.ts                    # Generated API client
│   └── keycloak.ts               # Keycloak configuration
├── store/
│   ├── theme.ts                  # Theme store (Zustand)
│   ├── editor.ts                 # Editor state (nodes, edges, config)
│   └── auth.ts                   # Auth state
├── types/
│   └── api.ts                    # Generated API types
├── pages/
│   ├── LoginPage.tsx             # Login landing / redirect
│   ├── AgentsPage.tsx            # Agent list
│   ├── AgentEditorPage.tsx       # Workflow editor
│   ├── ExecutionsPage.tsx        # Execution history
│   ├── CredentialsPage.tsx       # API keys / secrets
│   └── NotFoundPage.tsx
├── App.tsx
├── main.tsx
└── index.css
```

---

## Navigation

### Left Sidebar Menu

```
+------------------+
|  DonkeyWork      |  <- Logo (with donkey icon)
|  --------------  |
|  Agents          |  <- /agents
|  API Keys        |  <- /api-keys
|  Secrets         |  <- /secrets
+------------------+
```

- Mobile: Hamburger menu in header, sidebar slides in as overlay
- Desktop: Fixed sidebar

### Routes

```
/                       -> Redirect to /agents
/login                  -> Login landing page
/login/callback         -> OAuth callback (parses tokens from URL fragment)
/agents                 -> Agent list
/agents/:id             -> Agent editor (workflow canvas)
/api-keys               -> API keys management (with table + CRUD)
/secrets                -> Secrets management (Coming Soon)
/profile                -> User profile
```

---

## Authentication

### Keycloak Integration

**Flow:**
1. User visits any protected route
2. `ProtectedRoute` checks for valid JWT in memory/localStorage
3. If no token or expired -> redirect to `/login`
4. `/login` page shows "Login with DonkeyWork" button
5. Button redirects to Keycloak login page
6. After Keycloak auth -> redirect back to `/login/callback`
7. Callback page exchanges code for tokens via backend
8. Store access token + refresh token
9. Redirect to original destination (or `/agents`)

**Token Management:**
- Access token stored in memory (short-lived)
- Refresh token stored in localStorage (longer-lived)
- Auto-refresh before access token expires (e.g., refresh at 80% of lifetime)
- On 401 response -> attempt token refresh -> if fails, redirect to `/login`

**API Integration:**
```typescript
// All API calls include Authorization header
const response = await fetch('/api/v1/agents', {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'Content-Type': 'application/json',
  },
})

// Interceptor handles 401
if (response.status === 401) {
  const refreshed = await refreshToken()
  if (!refreshed) {
    redirectToLogin()
    return
  }
  // Retry original request with new token
}
```

**Backend Endpoints (needed):**
```
POST /api/v1/auth/token          -> Exchange Keycloak code for tokens
POST /api/v1/auth/refresh        -> Refresh access token
POST /api/v1/auth/logout         -> Revoke tokens
GET  /api/v1/auth/me             -> Get current user info
```

---

## API Client

### Auto-Generation

Generate TypeScript client from backend OpenAPI spec:

```bash
npx @hey-api/openapi-ts -i http://localhost:5050/swagger/v1/swagger.json -o src/lib/api
```

### API Client Wrapper

```typescript
// src/lib/api-client.ts
import { useAuthStore } from '@/store/auth'

const BASE_URL = import.meta.env.VITE_API_URL || ''

async function fetchWithAuth(url: string, options: RequestInit = {}) {
  const { accessToken, refreshAccessToken, logout } = useAuthStore.getState()

  const response = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
  })

  if (response.status === 401) {
    const refreshed = await refreshAccessToken()
    if (!refreshed) {
      logout()
      window.location.href = '/login'
      throw new Error('Session expired')
    }
    // Retry with new token
    return fetchWithAuth(url, options)
  }

  return response
}

export const api = {
  get: (url: string) => fetchWithAuth(url),
  post: (url: string, body: unknown) => fetchWithAuth(url, { method: 'POST', body: JSON.stringify(body) }),
  put: (url: string, body: unknown) => fetchWithAuth(url, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (url: string) => fetchWithAuth(url, { method: 'DELETE' }),
}
```

---

## Styling

### Theme (copy from CodeSandbox-Manager)

Dark mode default, HSL CSS variables, Zustand persistence.

See index.css for full variable definitions.

---

## Mobile First Patterns

### Responsive Breakpoints

```
sm: 640px   -> Small tablets
md: 768px   -> Tablets
lg: 1024px  -> Laptops
xl: 1280px  -> Desktops
```

### Layout Patterns

```tsx
// Sidebar: hidden on mobile, visible on desktop
<aside className="hidden md:flex md:w-64 md:flex-col">

// Mobile menu button: visible on mobile, hidden on desktop
<button className="md:hidden">
  <Menu />
</button>

// Content padding: smaller on mobile
<main className="p-4 md:p-6 lg:p-8">

// Grid: single column on mobile, multi-column on desktop
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
```

---

## Responsive Table Pattern

Use this pattern for all data tables. Shows cards on mobile, full table on desktop.

### Structure

```tsx
{items.length > 0 && (
  <>
    {/* Mobile view - card layout */}
    <div className="space-y-3 md:hidden">
      {items.map((item) => (
        <div key={item.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
          <div className="flex items-start justify-between gap-2">
            <div className="space-y-1 min-w-0 flex-1">
              <div className="text-sm">
                <span className="text-muted-foreground">Name: </span>
                <span className="font-medium">{item.name}</span>
              </div>
              {/* More fields with "Label: Value" format */}
            </div>
            <div className="flex items-center gap-1 shrink-0">
              {/* Action buttons */}
            </div>
          </div>
        </div>
      ))}
    </div>

    {/* Desktop view - table layout */}
    <div className="hidden md:block rounded-md border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Name</TableHead>
            {/* More columns */}
            <TableHead className="w-[100px]">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item) => (
            <TableRow key={item.id}>
              <TableCell className="font-medium">{item.name}</TableCell>
              {/* More cells */}
              <TableCell>
                <div className="flex items-center gap-1">
                  {/* Action buttons */}
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>

    {/* Pagination */}
    {totalPages > 1 && (
      <div className="flex items-center justify-between pt-4">
        <p className="text-sm text-muted-foreground">
          Showing {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
        </p>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setPage(p => p - 1)} disabled={!canGoBack}>
            <ChevronLeft className="h-4 w-4" /> Previous
          </Button>
          <Button variant="outline" size="sm" onClick={() => setPage(p => p + 1)} disabled={!canGoForward}>
            Next <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    )}
  </>
)}
```

### Key Points

- **Mobile**: Card layout with "Label: Value" format, action buttons top-right
- **Desktop**: Full table with all columns visible
- **Pagination**: Default PAGE_SIZE = 20, shows "Showing X-Y of Z"
- **Reference**: See `ApiKeysPage.tsx` for full implementation

---

## Development Commands

```bash
npm run dev          # Start dev server
npm run build        # Build for production
npm run lint         # Lint
npm run generate-api # Generate API client (after backend is running)
```

---

## Environment Variables

```env
# .env.development
VITE_API_URL=http://localhost:5050
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=donkeywork
VITE_KEYCLOAK_CLIENT_ID=donkeywork-frontend
```

---

## Milestones

### Phase 1: Scaffold ✅
- [x] Vite + React + TypeScript project setup
- [x] Tailwind + shadcn/ui setup
- [x] Theme (CSS variables, dark mode, ThemeToggle)
- [x] Layout (AppLayout, Sidebar, Header)
- [x] Routing (react-router-dom)
- [x] Auth placeholder (no Keycloak yet, mock user)

### Phase 2: Agent List & CRUD ✅
- [x] AgentList page with mock data
- [x] AgentCard component
- [x] CreateAgentDialog
- [x] Delete confirmation

### Phase 3: Workflow Editor ✅
- [x] ReactFlow canvas setup (upgraded to @xyflow/react v12)
- [x] Custom nodes (Start, Model, End)
- [x] Node palette (drag to canvas)
- [x] Edge connections
- [x] Properties panel (with Monaco Editor for schemas)
- [x] Editor Zustand store
- [ ] Save to localStorage (mock persistence) **← NEXT**

### Phase 4: Node Configuration ✅
- [x] StartNode panel (name, InputSchema editor)
- [x] ModelNode panel (provider, model, credential, prompts)
- [x] EndNode panel (name, OutputSchema editor)
- [ ] Form validation **← TODO**

### Phase 5: Execution UI
- [ ] TestPanel with dynamic input form
- [ ] Mock streaming output
- [ ] ExecutionHistory list
- [ ] ExecutionDetail view

### Phase 6: Authentication
- [ ] Keycloak integration
- [ ] AuthProvider + ProtectedRoute
- [ ] Login page
- [ ] Token refresh logic
- [ ] 401 handling

### Phase 7: API Integration
- [ ] Generate API client from OpenAPI
- [ ] Wire up all endpoints
- [ ] Replace mocks with real data
- [ ] SSE streaming integration

---

## Notes

- Copy ThemeToggle, theme store, CSS variables directly from CodeSandbox-Manager
- Use same icon library (lucide-react)
- Match visual style (borders, cards, spacing)
- Keep components small and focused
- Prefer composition over configuration
