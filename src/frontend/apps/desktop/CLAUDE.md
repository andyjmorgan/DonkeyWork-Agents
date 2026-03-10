# Desktop App (Tauri)

Native desktop application wrapping the DonkeyWork frontend using Tauri 2.

## Tech Stack

- **Desktop Framework**: Tauri 2.10 (Rust backend)
- **Frontend**: React 19 + TypeScript + Vite
- **Styling**: Tailwind CSS (dark mode default)
- **State**: Zustand stores from shared `@donkeywork/*` packages
- **Auth**: Keycloak PKCE OAuth via Rust (localhost callback server)
- **Notifications**: SignalR hub with native OS notifications
- **Auto-Update**: GitHub Releases via `@tauri-apps/plugin-updater`

## Project Structure

```
src/
├── main.tsx                          # Entry point (configures platform before render)
├── App.tsx                           # Auth gate + page navigation state
├── components/
│   ├── DesktopLayout.tsx             # Sidebar + content layout with drag region
│   ├── DesktopSidebar.tsx            # Navigation menu + recent conversations
│   └── ErrorBoundary.tsx             # Catches React errors
├── hooks/
│   ├── useDesktopAuth.ts             # Keycloak auth (restore session, login, logout, refresh)
│   ├── useAutoUpdater.ts             # Check + install updates from GitHub Releases
│   ├── useNotificationHub.ts         # SignalR connection with native notifications
│   └── useDesktopWorkspaceNav.ts     # Maps workspace nav to page state
├── pages/
│   ├── LoginPage.tsx                 # Provider login buttons (Google, GitHub, Microsoft)
│   └── PlaceholderPage.tsx           # Stub for unimplemented pages
├── platform/
│   └── desktop-platform.ts           # PlatformConfig for desktop (API URLs, TauriStorageAdapter)
└── types.ts                          # Page type union + PageParams

src-tauri/
├── src/
│   ├── main.rs                       # Entry point
│   ├── lib.rs                        # Tauri setup (plugins, menu bar, event handlers)
│   └── auth.rs                       # OAuth PKCE flow, token storage, background refresh
├── tauri.conf.json                   # App config (window, CSP, updater, bundle)
├── Cargo.toml                        # Rust dependencies
├── build.rs                          # Tauri build script
├── capabilities/                     # Tauri permission capabilities
├── gen/                              # Auto-generated schemas (do not edit)
└── icons/                            # App icons
```

## Development

```bash
# From src/frontend/apps/desktop/

# Start Vite dev server (port 5174)
pnpm dev

# Start Tauri dev mode (opens native window pointing to dev server)
pnpm tauri dev
```

Vite HMR works for React changes. Rust changes trigger a rebuild.

## Building

```bash
# Build frontend + native app
pnpm tauri build
```

Produces: DMG (macOS), MSI (Windows), AppImage (Linux).

## Authentication

### Flow

1. User clicks login provider button in `LoginPage`
2. React invokes `start_auth(provider)` Tauri command
3. Rust generates PKCE verifier + challenge, binds to random localhost port
4. System browser opens Keycloak auth URL with `kc_idp_hint`
5. User authenticates, Keycloak redirects to `http://localhost:PORT/auth/callback`
6. Rust exchanges code for tokens, stores in Tauri store (`auth.json`)
7. React parses JWT, updates Zustand auth store

### Token Refresh

- Rust background loop checks every 60 seconds
- Refreshes at 80% of token lifetime (20% remaining)
- Posts to `https://agents.donkeywork.dev/api/v1/auth/refresh`
- Emits `tokens-refreshed` or `auth-expired` events to React

### Constants (`src-tauri/src/auth.rs`)

| Constant | Value |
|----------|-------|
| `KEYCLOAK_AUTHORITY` | `https://auth.donkeywork.dev/realms/Agents` |
| `KEYCLOAK_CLIENT_ID` | `donkeywork-agents-api` |
| `API_BASE_URL` | `https://agents.donkeywork.dev` |
| `STORE_FILENAME` | `auth.json` |

## Navigation

State-based navigation (no react-router). `App.tsx` manages `currentPage` and `pageParams`.

### Pages

| Page | Description |
|------|-------------|
| `chat` | Navi conversation interface |
| `conversations` | Browse/search history |
| `notes` | Notes list |
| `note-editor` | Create/edit note |
| `research` | Research items |
| `research-editor` | Create/edit research |
| `tasks` | Task list |
| `task-editor` | Create/edit task |
| `projects` | Project management |
| `project-detail` | Project view |
| `milestone-detail` | Milestone view |
| `settings` | Placeholder |

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Cmd+1 | Chat (Navi) |
| Cmd+2 | Notes |
| Cmd+3 | Research |
| Cmd+4 | Tasks |
| Cmd+5 | Projects |
| Cmd+Shift+F | History |
| Cmd+Shift+N | New Conversation |
| Cmd+N | New Item |
| Cmd+W | Close Item |
| Cmd+Shift+T | Toggle Theme |
| Cmd+, | Settings |

## Auto-Updater

- Endpoint: `https://github.com/andyjmorgan/DonkeyWork-Agents/releases/latest/download/latest.json`
- Checks 5 seconds after launch, then every 4 hours
- Downloads, installs, and prompts relaunch
- Signing pubkey configured in `tauri.conf.json` (currently empty - needs key before release)

## Release Process

Triggered by pushing a `desktop-v*` tag (e.g., `desktop-v0.1.0`).

Workflow: `.github/workflows/desktop-release.yml`

1. Builds Rust + frontend
2. Signs artifacts with Tauri signing key
3. Uploads to GitHub Release: `.dmg`, `.app.tar.gz`, `.app.tar.gz.sig`, `latest.json`

## Platform Abstraction

`desktop-platform.ts` implements `PlatformConfig`:

- `platform`: `'desktop'`
- `apiBaseUrl`: `https://agents.donkeywork.dev`
- `wsBaseUrl`: `wss://agents.donkeywork.dev`
- `storage`: `TauriStorageAdapter` wrapping `tauri-plugin-store` (`settings.json`)
- `openExternal`: Opens URLs in system browser via Tauri shell

## Tauri Plugins

| Plugin | Purpose |
|--------|---------|
| `single-instance` | Prevents multiple app instances |
| `store` | Persistent key-value storage (tokens, settings) |
| `shell` | System shell / open URLs |
| `opener` | Open files/URLs |
| `notification` | Native OS notifications |
| `updater` | Auto-update from GitHub Releases |
| `process` | Relaunch after update |

## Conventions

- One component per file, PascalCase names
- Hooks use `useXxx` pattern
- Pages suffixed with `Page`
- Platform-specific code isolated in `platform/`
- Shared packages from `@donkeywork/*` workspace
- Dark mode is the default theme
- Escape key navigates back from editor/detail pages
