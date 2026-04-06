# iOS Chat UI - Estimation & Implementation Plan

## Scope

Reproduce the DonkeyWork Agents chat interface (the "Navi" chat) as a native Swift iOS app, connecting to the existing backend via WebSocket (JSON-RPC 2.0) and REST API.

---

## Architecture Overview (Existing Web Implementation)

The current React implementation consists of:

| Layer | Key Files | Responsibility |
|-------|-----------|----------------|
| **Chat Page** | `AgentChatPage.tsx` | Route wrapper, passes `conversationId` |
| **Chat Panel** | `AgentChatPanel.tsx` (535 lines) | Main chat container: message list, input bar, side panels, modals |
| **WebSocket + State** | `useAgentConversation.ts` (910 lines) | WebSocket lifecycle, JSON-RPC 2.0, event dispatch, message state, reconnection |
| **Message Rendering** | `MessageRenderer.ts` (257 lines) | Converts `InternalMessage[]` to `ChatMessage[]` (grain state -> UI) |
| **Box Rendering** | `BoxRenderer.tsx` (271 lines) | Renders content boxes: markdown text, thinking, tool use, citations, usage |
| **Agent Helpers** | `agentBoxHelpers.ts` (184 lines) | Nested agent group tree operations (attach, update, search) |
| **Agent Cards** | `AgentCard.tsx`, `AgentCardGrid.tsx` | Visual cards for spawned sub-agents with status indicators |
| **Agent Detail Modal** | `AgentDetailModal.tsx` (225 lines) | Full-screen modal showing agent's transcript, recursive nesting |
| **Agent Side Panel** | `AgentSidePanel.tsx` (215 lines) | Collapsible tree view of all agents |
| **Citation Chip** | `CitationChip.tsx` (93 lines) | Inline citation pills with detail dialog |
| **MCP Side Panel** | `McpSidePanel.tsx` | MCP server connection status |
| **Execution Panel** | `ExecutionSidePanel.tsx`, `ExecutionDetailModal.tsx` | Execution history browser |
| **Socket Event Panel** | `SocketEventPanel.tsx` | Debug panel showing raw WebSocket events |
| **Pulse Dots** | `PulseDots.tsx` | Loading/activity indicators |
| **Types** | `agent-chat.ts`, `internal-messages.ts` | TypeScript type definitions (~200 lines) |

### Backend Protocol

- **Transport**: WebSocket at `/api/v1/conversations/{id}/ws` with JWT auth
- **Protocol**: JSON-RPC 2.0 (StreamJsonRpc on server)
- **RPC Methods**: `message(text)`, `cancel(key, scope?)`, `listAgents()`, `getState()`, `getAgentMessages(agentKey)`
- **Server Notifications**: `event` method with ~20 event types (message, thinking, tool_use, tool_result, tool_complete, agent_spawn, agent_complete, web_search, citation, usage, turn_start, turn_end, queue_status, cancelled, error, retry, mcp_server_status, sandbox_status, agent_idle, agent_result_data)
- **REST API**: Conversation CRUD, image upload, agent executions

---

## iOS Implementation Breakdown

### 1. Data Models & Types
**Effort: 2-3 days**

Translate TypeScript types to Swift structs/enums:

- `ChatMessage` (id, role, content, boxes)
- `ContentBox` enum with associated values: `text`, `thinking`, `citation`, `toolUse`, `usage`, `agentGroup`
- `ToolUseBox` with nested `AgentGroupBox`
- `InternalMessage` discriminated union (`InternalContentMessage`, `InternalAssistantMessage`, `InternalToolResultMessage`)
- `SocketEvent`, `McpServerStatus`, `SandboxStatus`
- `TrackedAgent`, `GetStateResponse`
- JSON-RPC 2.0 request/response codable types
- `AgentCompleteReason` enum

All types need `Codable` conformance with custom decoding for the `$type` polymorphic discriminator pattern.

### 2. Networking Layer
**Effort: 4-5 days**

#### WebSocket Client (3 days)
- `URLSessionWebSocketTask` or Starscream-based WebSocket manager
- JSON-RPC 2.0 framing: request/response with `id` correlation, server notifications
- Pending RPC request tracking with timeout (10s)
- Event buffering during reconnection
- Auto-reconnect on disconnect
- JWT auth via query parameter

#### REST API Client (2 days)
- Conversation CRUD (`POST /conversations`, `GET /conversations/{id}`, etc.)
- Image upload (`POST /conversations/{id}/upload`)
- Agent execution queries
- OAuth/JWT token refresh integration
- Generic API client with error handling

### 3. State Management (ViewModel Layer)
**Effort: 5-7 days**

This is the most complex piece. Port `useAgentConversation.ts` (910 lines) to a Swift `@Observable` / `ObservableObject` class:

#### Core State (1 day)
- `messages: [ChatMessage]`
- `isProcessing`, `pendingCount`, `isConnected`, `isReconnecting`
- `conversationId`, `mcpServerStatuses`, `sandboxStatus`
- `socketEvents` (debug)

#### Event Dispatcher (3-4 days)
Port the 600+ line `handleEvent()` function that processes ~20 event types:
- `turn_start` / `turn_end` — create/finalize assistant message placeholders
- `message` / `thinking` — append or create text/thinking boxes with streaming accumulation
- `tool_use` / `tool_result` / `tool_complete` — tool lifecycle rendering
- `agent_spawn` — create nested agent groups in the box tree
- `agent_complete` / `agent_idle` — mark agents done with recursive tree walk
- `web_search` / `web_search_complete` — web search visualization
- `citation` — citation chip data
- `usage` — token usage accumulation
- `error` / `retry` / `cancelled` — error handling
- `queue_status` / `mcp_server_status` / `sandbox_status` — infrastructure state

#### Agent Group Tree Management (2 days)
Port `agentBoxHelpers.ts` — immutable tree operations:
- `updateNestedGroup()` — recursive box tree update by agent key
- `tryUpdateNested()` — indexed fast-path + fallback scan
- `attachChildAgent()` / `attachRootAgent()` — spawn attachment logic
- `markCompleteInBoxes()` / `markNestedComplete()` — recursive completion marking
- `clearIdleState()` — resume from idle
- Agent group index (`Map<String, AgentGroupEntry>`) for O(1) lookups

#### Reconnection Logic (1 day)
- Buffer events during reconnect
- `getState()` + `listAgents()` RPC to restore state
- `internalToChat()` conversion with agent overlay
- Replay buffered events after state restore
- Rebuild agent group index from restored messages

### 4. Chat UI Views
**Effort: 6-8 days**

#### Main Chat View (2 days)
- Navigation bar with connection indicator, sandbox status, MCP pill, agents pill, execution history, new chat button
- `ScrollViewReader` message list with auto-scroll to bottom
- Message input bar with send button
- Stop/cancel controls when processing
- Queue management (clear queue button with pending count)
- Empty state with Navi branding

#### Message Bubbles (1 day)
- User messages: right-aligned, gradient background (cyan-to-blue), copy button
- Assistant messages: left-aligned, content boxes rendering
- Progress messages: status pill with pulse dots
- Agent result source indicator

#### Box Renderers (2-3 days)
- **Text Box**: Markdown rendering (use `swift-markdown-ui` or `AttributedString` with markdown). Must handle GFM tables, code blocks, links
- **Thinking Box**: Collapsible disclosure group with purple accent
- **Tool Use Box**: Expandable card showing tool name, arguments (JSON viewer), result, duration, web search results. Status indicators (running pulse, success check, error X)
- **Citation Box**: Inline chip with favicon, expandable detail sheet with URL and cited text
- **Usage Box**: Expandable token counter (input/output/total vs limits)
- **Agent Group Box**: Rendered via AgentCardGrid (see below)
- `BoxList`: Groups consecutive citations after preceding content boxes

#### Agent Cards (1 day)
- Grid layout (2 columns on phone, 4 on iPad)
- Card with agent icon, type, label, status badges (search count, citation count, child agent count)
- Active: pulse dots + cancel button. Complete: check/ban/warning/moon icons
- Color coding by agent type

#### Copy Button (0.5 day)
- Clipboard integration with success feedback animation

### 5. Side Panels & Modals
**Effort: 4-5 days**

#### Agent Detail Modal (2 days)
- Full-screen sheet showing agent's content boxes
- Recursive: clicking a child agent opens another modal
- Activity indicator when streaming, cancel button
- Lazy-fetch transcript via `getAgentMessages` RPC when boxes are empty
- Agent key display (monospaced, selectable)

#### Agent Side Panel (1 day)
- Slide-in panel (trailing edge) with tree view
- Collapsible tree nodes with depth indentation
- Active count badge with pulse indicator
- Color-coded by agent type, status icons

#### MCP Side Panel (0.5 day)
- List of MCP servers with connection status, duration, tool count, errors

#### Execution Side Panel (0.5 day)
- List of agent executions for current conversation
- Tap to view execution detail modal

#### Socket Event Debug Panel (0.5 day)
- Scrollable log of raw WebSocket events (debug mode)

### 6. Message Conversion Layer
**Effort: 1-2 days**

Port `MessageRenderer.ts` `internalToChat()`:
- Convert `InternalMessage[]` to `ChatMessage[]`
- Build tool result map, overlay agent info on spawn tools
- Handle `InternalContentMessage`, `InternalAssistantMessage`, `InternalToolResultMessage`
- `convertContentBlocks()` for `$type` discriminated blocks
- Agent overlay with TrackedAgent status mapping

### 7. Auth & Platform Integration
**Effort: 2-3 days**

- Keycloak OAuth PKCE flow (ASWebAuthenticationSession)
- Secure token storage (Keychain)
- Token refresh before expiry
- 401 handling with re-auth flow
- Platform config (API base URL, WebSocket URL)

### 8. Polish & Animations
**Effort: 2-3 days**

- Smooth scroll-to-bottom on new messages
- Pulse dot bounce animation (CSS `dot-bounce` -> SwiftUI)
- Orbital spin animation for active web search
- Theme support (dark/light mode matching design tokens)
- Gradient accents (cyan-to-blue send button, dividers)
- Haptic feedback on send/copy
- Keyboard avoidance
- Safe area handling

### 9. Testing
**Effort: 3-4 days**

- Unit tests for data models (Codable round-trips)
- Unit tests for agent box helpers (tree operations)
- Unit tests for `internalToChat()` conversion
- Unit tests for event dispatcher
- Integration tests for WebSocket reconnection
- UI snapshot tests for key views

---

## Effort Summary

| Area | Estimate (days) | Complexity |
|------|-----------------|------------|
| Data Models & Types | 2-3 | Medium |
| Networking (WebSocket + REST) | 4-5 | High |
| State Management (ViewModel) | 5-7 | **Very High** |
| Chat UI Views | 6-8 | High |
| Side Panels & Modals | 4-5 | Medium |
| Message Conversion | 1-2 | Medium |
| Auth & Platform | 2-3 | Medium |
| Polish & Animations | 2-3 | Low-Medium |
| Testing | 3-4 | Medium |
| **Total** | **29-40 days** | |

**Estimated range: 6-8 weeks** for a single experienced iOS developer.

---

## Key Complexity Drivers

### 1. Nested Agent Group Tree (Hardest Part)
The box model supports arbitrary nesting: an agent spawns sub-agents, which spawn their own sub-agents, each with their own tool calls, thinking, and text. All updates are immutable tree transformations indexed for performance. This recursive data structure with O(1) indexed lookups + fallback scans is the single most complex piece to port correctly.

### 2. Real-Time Streaming Event Dispatch
The WebSocket event handler processes 20+ event types, each mutating a specific part of the nested box tree. Events arrive out of order during reconnection and must be buffered. The `appendOrCreate` pattern (find existing box to append to, or create new) with index-assisted nested updates is intricate.

### 3. Markdown Rendering
The web version uses `react-markdown` with GFM. iOS needs an equivalent. Options:
- **swift-markdown-ui** (best option, SwiftUI native)
- **AttributedString** with `MarkdownParsingOptions` (limited GFM support)
- **WKWebView** fallback for complex content (performance trade-off)

### 4. JSON Viewer
Tool arguments and results are shown with an interactive JSON tree viewer. No direct SwiftUI equivalent exists — needs a custom collapsible tree view or a 3rd-party library.

---

## Recommended Dependencies

| Purpose | Library |
|---------|---------|
| Markdown rendering | `MarkdownUI` (gonzalezreal/swift-markdown-ui) |
| WebSocket | `URLSessionWebSocketTask` (built-in) or `Starscream` |
| JSON viewer | Custom implementation or `SwiftyJSON` + custom tree view |
| Keychain | `KeychainAccess` |
| Image loading | `Kingfisher` or `SDWebImage` (for favicons) |
| Syntax highlighting | `Splash` or `Highlightr` (for code blocks) |

---

## Risk Factors

1. **Markdown fidelity** — Achieving pixel-parity with `react-markdown` + `remark-gfm` in SwiftUI is non-trivial, especially tables and code blocks with syntax highlighting.
2. **Performance with deep nesting** — SwiftUI can struggle with deeply nested view hierarchies when agents spawn many sub-agents. May need `LazyVStack` optimizations or view identity management.
3. **WebSocket reliability** — iOS background mode limitations mean the WebSocket will disconnect when the app backgrounds. Need reconnection + state restore (already designed in the web version).
4. **Concurrent state mutations** — The web version uses React's batched state updates. Swift's `@Observable` on `@MainActor` is equivalent, but the event buffering during reconnection needs careful threading.

---

## Suggested Phased Approach

### Phase 1 - MVP (3-4 weeks)
- Data models, networking, auth
- Core chat view with text + thinking rendering
- WebSocket connection with basic event handling
- Send/receive messages, markdown rendering

### Phase 2 - Tool & Agent Support (2-3 weeks)
- Tool use boxes (expandable cards with arguments/results)
- Agent spawn/complete lifecycle
- Agent cards grid
- Agent detail modal (non-recursive first pass)

### Phase 3 - Full Feature Parity (1-2 weeks)
- Side panels (agents, MCP, executions)
- Citation chips with detail sheet
- Recursive agent modals
- Reconnection with state restore
- Debug socket event panel
- Usage tracking display
- Polish and animations
