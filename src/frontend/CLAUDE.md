# Frontend - DonkeyWork Agents

## Tech Stack

- **Framework**: React 19 + Vite + TypeScript
- **Styling**: Tailwind CSS + shadcn/ui + CSS Variables (HSL)
- **State Management**: Zustand (with persist middleware)
- **Workflow Editor**: ReactFlow (@xyflow/react v12)
- **Icons**: lucide-react
- **Authentication**: Keycloak JWT (OAuth PKCE)
- **API Client**: Auto-generated from backend OpenAPI spec (`@donkeywork/api-client`)
- **Monorepo**: pnpm workspaces

## Design Principles

- **Mobile First**: Design for mobile, enhance for desktop
- **Dark Mode Default**: Theme toggle with localStorage persistence
- **Copy patterns from**: `/Users/andrewmorgan/Personal/source/DonkeyWork-CodeSandbox-Manager/frontend`
- **Vibe**: Fun and vibrant, casual speak, but no emojis

---

## Project Structure

This is a pnpm monorepo with two apps and shared packages:

```
src/frontend/
├── pnpm-workspace.yaml
├── package.json                         # Root scripts proxy to @donkeywork/web
├── colorstyle.html                      # Dark mode design system reference
├── lightmmode.colorstyle.html           # Light mode design system reference
├── apps/
│   ├── web/                             # Main web application (@donkeywork/web)
│   │   └── src/
│   │       ├── App.tsx                  # BrowserRouter + auth guard + providers
│   │       ├── main.tsx
│   │       ├── index.css
│   │       ├── components/
│   │       │   ├── layout/              # AppLayout, Sidebar, Header, ThemeToggle
│   │       │   ├── editor/              # Workflow editor (see editor/CLAUDE.md)
│   │       │   ├── execution/           # TestPanel, StreamingOutput
│   │       │   ├── credentials/         # Credential management
│   │       │   ├── agent-builder/       # Agent builder UI
│   │       │   ├── agent-chat/          # Chat interface
│   │       │   ├── a2a/                 # A2A server components
│   │       │   ├── mcp/                 # MCP server components
│   │       │   ├── oauth/               # OAuth client components
│   │       │   ├── providers/           # NotificationListener, etc.
│   │       │   ├── sandbox-settings/    # Code sandbox config
│   │       │   ├── skills/              # Skills management
│   │       │   ├── files/               # File management
│   │       │   ├── branding/            # Branding components
│   │       │   ├── icons/               # Custom icon components
│   │       │   └── ui/                  # shadcn/ui components
│   │       ├── hooks/
│   │       │   ├── useExecutionStream.ts
│   │       │   ├── useAgentTestStream.ts
│   │       │   ├── useTokenRefresh.ts
│   │       │   ├── useOAuthFlow.ts
│   │       │   ├── useNotifications.ts
│   │       │   └── useWorkspaceNav.ts
│   │       ├── lib/
│   │       │   └── utils.ts             # cn() helper
│   │       ├── store/
│   │       │   ├── editor.ts            # Editor state (nodes, edges, config)
│   │       │   └── agentBuilder.ts      # Agent builder state
│   │       ├── pages/                   # All route pages
│   │       ├── platform/               # Web platform config
│   │       ├── schemas/                # JSON schemas
│   │       ├── types/                  # TypeScript types
│   │       └── test/                   # Test utilities
│   └── desktop/                         # Tauri desktop app (see desktop/CLAUDE.md)
├── packages/
│   ├── api-client/                      # Auto-generated API client (@donkeywork/api-client)
│   ├── chat/                            # Chat components (@donkeywork/chat)
│   ├── editor/                          # Editor components (@donkeywork/editor)
│   ├── platform/                        # Platform abstraction (@donkeywork/platform)
│   ├── stores/                          # Shared Zustand stores (@donkeywork/stores)
│   │   └── src/
│   │       ├── auth.ts                  # Auth state
│   │       ├── theme.ts                # Theme state
│   │       └── active-conversations.ts
│   ├── ui/                              # Shared UI components (@donkeywork/ui)
│   └── workspace/                       # Workspace components (@donkeywork/workspace)
```

---

## Pages (Web App)

The web app uses react-router-dom with these routes:

| Route | Page |
|-------|------|
| `/` | Redirect to `/orchestrations` |
| `/login` | Login page |
| `/login/callback` | OAuth callback |
| `/oauth/callback` | OAuth provider callback |
| `/orchestrations` | Orchestration list |
| `/orchestrations/:id` | Orchestration editor |
| `/agent-definitions` | Agent definitions |
| `/agent-builder/:id` | Agent builder |
| `/agent-chat/:id?` | Agent chat interface |
| `/conversations` | Conversation history |
| `/prompts` | Prompt management |
| `/api-keys` | API key management |
| `/credentials` | Credential management |
| `/connected-accounts` | OAuth connected accounts |
| `/oauth-clients` | OAuth client management |
| `/mcp-servers` | MCP server management |
| `/a2a-servers` | A2A server management |
| `/skills` | Skills list |
| `/skills/:id` | Skill detail |
| `/executions` | Execution history |
| `/executions/:id` | Execution detail |
| `/projects` | Project management |
| `/projects/:id` | Project detail |
| `/projects/:projectId/milestones/:milestoneId` | Milestone detail |
| `/tasks` | Task list |
| `/tasks/:id?` | Task editor |
| `/notes` | Notes list |
| `/notes/:id?` | Note editor |
| `/research` | Research list |
| `/research/:id?` | Research editor |
| `/files` | File management |
| `/sandbox-settings` | Sandbox settings |
| `/profile` | User profile |

---

## Authentication

### OAuth PKCE Flow

1. User visits protected route, `AuthGuard` checks for valid JWT
2. If no token -> redirect to `/login`
3. Login page redirects to Keycloak with PKCE
4. Keycloak redirects back to `/login/callback`
5. Callback exchanges code for tokens via backend
6. Tokens stored in Zustand auth store (from `@donkeywork/stores`)
7. `useTokenRefresh` hook handles automatic refresh before expiry
8. On 401 -> attempt refresh -> if fails, redirect to login

---

## Styling

### DonkeyWork Design System

The design system is documented in two HTML reference files:
- `colorstyle.html` - Dark mode design system
- `lightmmode.colorstyle.html` - Light mode design system

Open these files in a browser to see the full visual reference.

### Design Tokens

| Token | Dark Mode | Light Mode |
|-------|-----------|------------|
| **Backgrounds** | | |
| Primary | `#0a0d12` | `#ffffff` |
| Secondary | `#0f1318` | `#f8fafc` |
| Tertiary | `#151a21` | `#f1f5f9` |
| Elevated | `#1a2028` | `#e2e8f0` |
| **Text** | | |
| Primary | `#ffffff` | `#0f172a` |
| Secondary | `#94a3b8` | `#475569` |
| Tertiary | `#64748b` | `#64748b` |
| Muted | `#475569` | `#94a3b8` |
| **Accent** | `#22d3ee` | `#0891b2` |

### Node Colors (same for both modes)

| Node Type | Color | Border Class |
|-----------|-------|--------------|
| Start | `#22c55e` | `border-green-500` |
| End | `#f97316` | `border-orange-500` |
| Action | `#a855f7` | `border-purple-500` |
| Utility | `#22d3ee` | `border-cyan-500` |
| Model | `#3b82f6` | `border-blue-500` |

### Component Patterns

**Buttons:**
- Primary: Gradient `from-cyan-500 to-blue-600` with glow shadow
- Secondary: Border with `shadow-sm`
- Destructive: Red tinted background with red text/border
- Ghost: Transparent, accent on hover
- Success: Gradient `from-emerald-500 to-green-600`
- Warning: Gradient `from-amber-500 to-orange-600`

**Inputs:**
- Border radius: `rounded-xl` (12px)
- Focus: Cyan border with `ring-accent/20`
- Shadow: `shadow-sm`

**Cards:**
- Border radius: `rounded-2xl` (16px)
- Border: `border-border`
- Hover: `hover:border-accent/30` or `hover:shadow-md`

### Typography

- **Font family**: Inter (body), JetBrains Mono (code)
- **Headings**: Semibold/Bold, slate-900 (light) / white (dark)
- **Body**: Regular, slate-700 (light) / slate-300 (dark)

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

- **Mobile**: Card layout with "Label: Value" format, action buttons top-right
- **Desktop**: Full table with all columns visible
- **Pagination**: Default PAGE_SIZE = 20, shows "Showing X-Y of Z"
- **Reference**: See `ApiKeysPage.tsx` for full implementation

---

## Development Commands

```bash
# From src/frontend/ (proxied to @donkeywork/web)
pnpm dev              # Start dev server
pnpm build            # Build for production
pnpm lint             # Lint
pnpm test:run         # Run tests

# From src/frontend/apps/desktop/
pnpm dev              # Start Vite dev server (port 5174)
pnpm tauri dev        # Start Tauri native dev mode
pnpm tauri build      # Build native app
```

---

## Notes

- Use same icon library (lucide-react) throughout
- Keep components small and focused
- Prefer composition over configuration
- Shared stores, UI, and platform code live in `packages/`
- App-specific code lives in `apps/web/` or `apps/desktop/`
