# Agent Editor Implementation Plan

## Overview

Build a ReactFlow-based agent workflow editor that allows users to visually create and configure AI agent workflows. This plan focuses on the UI/UX without API integration (mock data only).

---

## Data Model (From Backend)

### AgentVersion Structure
```typescript
interface AgentVersion {
  id: string                                    // UUID
  agentId: string                               // FK to Agent
  versionNumber: number                         // Incrementing version
  isDraft: boolean                              // true for unpublished draft
  inputSchema: JSONSchema                       // JSON Schema for input validation
  outputSchema: JSONSchema | null               // Optional JSON Schema for output
  reactFlowData: ReactFlowData                  // Complete ReactFlow export
  nodeConfigurations: Record<string, NodeConfig> // Dictionary keyed by node GUID
  createdAt: string                             // ISO date
  publishedAt: string | null                    // ISO date or null
}

interface ReactFlowData {
  nodes: ReactFlowNode[]                        // Array of nodes with id, type, position, data
  edges: ReactFlowEdge[]                        // Array of edges with id, source, target
  viewport: { x: number; y: number; zoom: number } // Zoom/pan state
}

interface ReactFlowNode {
  id: string                                    // GUID
  type: 'start' | 'model' | 'end'              // Node type
  position: { x: number; y: number }           // Canvas position
  data: {
    label?: string                              // Display label
    [key: string]: any                          // Frontend-specific data
  }
}

interface ReactFlowEdge {
  id: string                                    // GUID
  source: string                                // Source node ID
  target: string                                // Target node ID
  type?: string                                 // Edge type (default, smoothstep, etc.)
}
```

### Node Configurations

**Start Node Config:**
```typescript
interface StartNodeConfig {
  name: string                                  // Unique name (e.g., "start_1")
  inputSchema: JSONSchema                       // JSON Schema for input validation
}
```

**Model Node Config:**
```typescript
interface ModelNodeConfig {
  name: string                                  // Unique name for template refs
  provider: 'OpenAI' | 'Anthropic' | 'Google'  // LLM provider
  modelId: string                               // Model identifier
  credentialId: string                          // FK to ExternalApiKey
  systemPrompt?: string                         // Scriban template (optional)
  userMessage: string                           // Scriban template (required)
  temperature?: number                          // Optional (provider default)
  maxTokens?: number                            // Optional (provider default)
  topP?: number                                 // Optional (provider default)
}
```

**End Node Config:**
```typescript
interface EndNodeConfig {
  name: string                                  // Unique name (e.g., "end_1")
  outputSchema?: JSONSchema                     // Optional JSON Schema for output
}
```

### Default Agent Template
When creating a new agent:
- Draft version with Start → End nodes connected
- Start node has default InputSchema: `{ "type": "object", "properties": { "input": { "type": "string" } }, "required": ["input"] }`
- End node has OutputSchema: null (optional)
- Two nodes: start_1 and end_1 with edge connecting them

---

## UI Layout

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Header: [< Back] Agent Name                     [Save Draft] [Publish]  │
├──────────────────────────────────────────────────────────────────────────┤
│ [☰]  │                                                        │           │
│ Node │                  ReactFlow Canvas                     │Properties │
│ Pal. │                                                        │   Panel   │
│      │                                                        │           │
│ Start│                      [Start]                          │ Node Name │
│ Model│                         │                             │ [input]   │
│ End  │                      [Model]                          │           │
│      │                         │                             │ Provider  │
│      │                       [End]                           │ [select]  │
│      │                                                        │           │
│      │                                                        │ Model     │
│      │                                                        │ [select]  │
└──────┴────────────────────────────────────────────────────────┴───────────┘
```

### Layout Breakdown

1. **Header Bar** (top):
   - Back button to agents list
   - Agent name (editable inline or via dialog)
   - Agent description (below name, smaller text)
   - Actions: Save Draft, Publish (disabled for now)

2. **Node Palette** (left side, collapsible):
   - Drawer that shows available node types and models
   - Drag to add to canvas
   - **Section 1: Core Nodes**
     - 🟢 Start (only one allowed, grayed out if exists)
     - 🔴 End (only one allowed, grayed out if exists)
   - **Section 2: Models** (grouped by provider)
     - OpenAI
       - GPT-4o
       - GPT-4o Mini
       - GPT-3.5 Turbo
     - Anthropic
       - Claude 3.5 Sonnet
       - Claude 3.5 Haiku
     - Google
       - Gemini 1.5 Pro
       - Gemini 1.5 Flash
   - Dragging a model creates a pre-configured Model node

3. **Canvas** (center, main area):
   - ReactFlow component
   - Top-down layout direction
   - Controls (zoom, fit view)
   - Mini-map (bottom-right corner)
   - Background pattern (dots or grid)
   - Starts with Start → End connected

4. **Properties Panel** (right side, collapsible):
   - Shows config form for selected node
   - Different forms based on node type:
     - **Start Node**: Name + Input Schema (Monaco Editor)
     - **Model Node**: Full config form
     - **End Node**: Name + Output Schema (Monaco Editor, optional)
   - No node selected: Show "Select a node to configure"

---

## Component Structure

```
pages/
└── AgentEditorPage.tsx              # Main editor page

components/editor/
├── EditorLayout.tsx                 # Main layout wrapper
├── EditorHeader.tsx                 # Top bar with name, actions
├── Canvas.tsx                       # ReactFlow canvas wrapper
├── NodePalette.tsx                  # Left drawer with node types
├── PropertiesPanel.tsx              # Right panel with forms
└── nodes/
    ├── StartNode.tsx                # Custom Start node component
    ├── ModelNode.tsx                # Custom Model node component
    └── EndNode.tsx                  # Custom End node component

store/
└── editor.ts                        # Zustand store for editor state
```

---

## Zustand Store (editor.ts)

```typescript
interface EditorState {
  // Agent metadata
  agentId: string | null
  agentName: string
  agentDescription: string

  // Version data
  versionId: string | null
  isDraft: boolean

  // ReactFlow state
  nodes: Node[]
  edges: Edge[]
  viewport: { x: number; y: number; zoom: number }

  // Node configurations (source of truth)
  nodeConfigurations: Record<string, NodeConfig>

  // Note: schemas now live on Start/End nodes, not at agent level

  // UI state
  selectedNodeId: string | null
  isPaletteOpen: boolean
  isPropertiesOpen: boolean

  // Actions
  setAgentMetadata: (name: string, description: string) => void
  setNodes: (nodes: Node[]) => void
  setEdges: (edges: Edge[]) => void
  addNode: (type: NodeType, position: Position) => void
  removeNode: (nodeId: string) => void
  updateNodeConfig: (nodeId: string, config: Partial<NodeConfig>) => void
  selectNode: (nodeId: string | null) => void
  togglePalette: () => void
  toggleProperties: () => void
  loadAgent: (agent: Agent) => void
  reset: () => void

  // Validation
  validate: () => ValidationResult
}

interface ValidationResult {
  isValid: boolean
  errors: {
    field: string
    message: string
  }[]
}
```

---

## Implementation Phases

### Phase 1: Basic Canvas Setup ✅

**Goal:** Get ReactFlow working with custom nodes

**Tasks:**
1. Create `AgentEditorPage.tsx` with route `/agents/:id`
2. Install ReactFlow CSS (`import 'reactflow/dist/style.css'`)
3. Create basic `Canvas.tsx` with ReactFlow wrapper
4. Create custom node components:
   - `StartNode.tsx`: Green rounded rectangle with icon
   - `ModelNode.tsx`: Blue rectangle with brain icon
   - `EndNode.tsx`: Red rounded rectangle with icon
5. Initialize store with default Start→End
6. Setup node types mapping in ReactFlow

**Node Styling:**
- Start: Green (#10b981), rounded, "Start" label
- Model: Blue (#3b82f6), rectangle, "Model" label + name
- End: Red (#ef4444), rounded, "End" label

**Default Initial State:**
```typescript
const defaultState = {
  nodes: [
    {
      id: generateGuid(),
      type: 'start',
      position: { x: 250, y: 50 },
      data: { label: 'Start' }
    },
    {
      id: generateGuid(),
      type: 'end',
      position: { x: 250, y: 250 },
      data: { label: 'End' }
    }
  ],
  edges: [
    {
      id: generateGuid(),
      source: nodes[0].id,
      target: nodes[1].id
    }
  ],
  nodeConfigurations: {
    [nodes[0].id]: {
      name: 'start_1',
      inputSchema: {
        type: 'object',
        properties: { input: { type: 'string' } },
        required: ['input']
      }
    },
    [nodes[1].id]: {
      name: 'end_1',
      outputSchema: null
    }
  }
}
```

---

### Phase 2: Node Palette ✅

**Goal:** Allow dragging nodes from palette onto canvas

**Tasks:**
1. Create `NodePalette.tsx` as left sidebar
2. Implement drag-and-drop from palette to canvas
3. Handle node addition:
   - Generate GUID for node ID
   - Auto-generate unique name (start_1, model_2, etc.)
   - Create default configuration
   - Add to store
4. Disable Start/End in palette if already on canvas (only one allowed)
5. Add icons for each node type (lucide-react)

**Node Palette Structure:**
```typescript
// Section 1: Core Nodes
const coreNodes = [
  {
    type: 'start',
    label: 'Start',
    icon: Play,
    description: 'Entry point - validates input',
    disabled: () => hasStartNode(),
    color: 'green'
  },
  {
    type: 'end',
    label: 'End',
    icon: Flag,
    description: 'Output and completion',
    disabled: () => hasEndNode(),
    color: 'red'
  }
]

// Section 2: Models (grouped by provider)
const modelsByProvider = {
  OpenAI: [
    { id: 'gpt-4o', name: 'GPT-4o', icon: Sparkles },
    { id: 'gpt-4o-mini', name: 'GPT-4o Mini', icon: Zap },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo', icon: MessageSquare }
  ],
  Anthropic: [
    { id: 'claude-3-5-sonnet-20241022', name: 'Claude 3.5 Sonnet', icon: Brain },
    { id: 'claude-3-5-haiku-20241022', name: 'Claude 3.5 Haiku', icon: Feather }
  ],
  Google: [
    { id: 'gemini-1.5-pro', name: 'Gemini 1.5 Pro', icon: Gem },
    { id: 'gemini-1.5-flash', name: 'Gemini 1.5 Flash', icon: Zap }
  ]
}
```

**Drag Implementation:**
- Use ReactFlow's `onDrop` handler
- Calculate drop position accounting for zoom/pan
- Automatically name nodes incrementally per type (start_1, model_1, model_2, etc.)
- User can rename via properties panel
- When dragging a model, create Model node pre-configured with provider + modelId

---

### Phase 3: Properties Panel ✅

**Goal:** Configure selected node

**Tasks:**
1. Create `PropertiesPanel.tsx` as right sidebar
2. Show different forms based on selected node type:
   - **Start Node Form**:
     - Node Name text input
     - Input Schema Monaco Editor (JSON)
   - **Model Node Form**:
     - Node Name text input
     - Provider display (read-only, pre-filled from drop)
     - Model display (read-only, pre-filled from drop)
     - Credential dropdown (from API Keys)
     - System Prompt textarea (Scriban, optional)
     - User Message textarea (Scriban, required)
     - Parameters (collapsible): temperature, maxTokens, topP
   - **End Node Form**:
     - Node Name text input
     - Output Schema Monaco Editor (JSON, optional)
3. Implement form state management
4. Update nodeConfigurations in store on change
5. Add validation indicators
6. Show "No node selected" empty state

**Monaco Editor Setup:**
```typescript
// For Input/Output Schema editing
import Editor from '@monaco-editor/react'

<Editor
  height="400px"
  defaultLanguage="json"
  value={schema}
  onChange={handleSchemaChange}
  options={{
    minimap: { enabled: false },
    lineNumbers: 'on',
    formatOnPaste: true,
    formatOnType: true
  }}
/>
```

**Template Help:**
Show hints about available variables in Model node prompts:
- `{{ input.field_name }}` - from Start node's InputSchema
- `{{ steps.node_name }}` - from previous Model nodes

**Form Validation:**
- Node name: required, unique, lowercase, a-z 0-9 - _
- Start InputSchema: required, valid JSON Schema
- End OutputSchema: optional, valid JSON Schema if provided
- Model Credential: required
- Model User Message: required

---

### Phase 4: Editor Header & Metadata ✅

**Goal:** Agent-level configuration

**Tasks:**
1. Create `EditorHeader.tsx` with:
   - Back button to `/agents`
   - Agent name (editable via dialog)
   - Agent description (editable via dialog)
   - Auto-arrange button (uses dagre for layout)
   - Save Draft button (mock for now)
   - Publish button (disabled, for later)
2. Add "Edit Agent Details" dialog

**Auto-arrange Implementation:**
```typescript
import dagre from 'dagre'

function autoLayout(nodes, edges) {
  const dagreGraph = new dagre.graphlib.Graph()
  dagreGraph.setDefaultEdgeLabel(() => ({}))
  dagreGraph.setGraph({ rankdir: 'TB', ranksep: 100, nodesep: 80 })

  nodes.forEach(node => {
    dagreGraph.setNode(node.id, { width: 200, height: 100 })
  })

  edges.forEach(edge => {
    dagreGraph.setEdge(edge.source, edge.target)
  })

  dagre.layout(dagreGraph)

  return nodes.map(node => {
    const position = dagreGraph.node(node.id)
    return { ...node, position: { x: position.x, y: position.y } }
  })
}
```

---

### Phase 5: Node Operations ✅

**Goal:** Full CRUD for nodes/edges

**Tasks:**
1. **Delete Node**: Add delete button to selected node (or keyboard Delete)
2. **Delete Edge**: Click edge to select, press Delete
3. **Connect Nodes**: ReactFlow default behavior (drag from handle)
4. **Duplicate Node**: Right-click menu with "Duplicate" (creates copy with new GUID and name)
5. **Node Context Menu**: Right-click on node shows:
   - Delete
   - Duplicate
   - Copy ID
6. **Canvas Context Menu**: Right-click on canvas shows:
   - Add Start (if none)
   - Add Model
   - Add End (if none)
   - Fit View
   - Reset Zoom

**Keyboard Shortcuts:**
- `Delete` / `Backspace`: Delete selected node/edge
- `Cmd/Ctrl + D`: Duplicate selected node
- `Cmd/Ctrl + S`: Save draft
- `Cmd/Ctrl + F`: Fit view
- `Cmd/Ctrl + 0`: Reset zoom

---

### Phase 6: Validation & Feedback ✅

**Goal:** Prevent invalid workflows

**Tasks:**
1. Implement validation function in store:
   - Exactly one Start node
   - Exactly one End node
   - All nodes connected (reachable from Start)
   - No cycles (DAG validation)
   - All edges reference existing nodes
   - All node names unique
   - All required config fields filled
2. Show validation errors in UI:
   - Red outline on invalid nodes
   - Error list in properties panel
   - Tooltip on hover showing error
3. Disable Save/Publish if validation fails
4. Real-time validation as user edits

**Validation Error Examples:**
- "Missing Start node"
- "Node 'model_1' is not connected to Start"
- "Cycle detected: model_1 → model_2 → model_1"
- "Duplicate node name: 'model_1'"
- "Model node 'model_2' missing required field: userMessage"

---

### Phase 7: Polish & UX ✅

**Goal:** Professional, smooth experience

**Tasks:**
1. **Loading States**: Show skeleton when loading agent
2. **Empty State**: If no agent ID, show "Create an agent first"
3. **Auto-save Draft**: Debounced auto-save every 3 seconds (to localStorage for now)
4. **Unsaved Changes Warning**: Warn before navigating away if changes exist
5. **Node Preview**: Hover on palette item shows preview
6. **Connection Validation**: Don't allow invalid connections (e.g., End → Start)
7. **Zoom Controls**: Prominent zoom in/out/fit buttons
8. **Mini-map**: Toggle visibility

**Visual Polish:**
- Smooth animations for panel open/close
- Hover effects on nodes
- Highlighted selected node with glow
- Animated edge connections
- Node drag handles (top = input, bottom = output)

---

## Responsive Strategy (MVP)

**Desktop only for MVP** (> 1024px):
- Full 3-column layout
- All panels visible by default
- Optimal experience

**Mobile/Tablet support**: Deferred to post-MVP

---

## Mock Data for Development

**Mock Agent:**
```typescript
const mockAgent = {
  id: '123e4567-e89b-12d3-a456-426614174000',
  name: 'Customer Support Agent',
  description: 'Answers customer questions using knowledge base',
  currentVersionId: '123e4567-e89b-12d3-a456-426614174001'
}

const mockVersion = {
  id: '123e4567-e89b-12d3-a456-426614174001',
  agentId: '123e4567-e89b-12d3-a456-426614174000',
  versionNumber: 1,
  isDraft: true,
  inputSchema: {
    type: 'object',
    properties: {
      question: { type: 'string', description: 'Customer question' },
      customerId: { type: 'string', description: 'Customer ID' }
    },
    required: ['question']
  },
  outputSchema: null,
  reactFlowData: { /* initial Start → End */ },
  nodeConfigurations: { /* start_1, end_1 */ },
  createdAt: new Date().toISOString(),
  publishedAt: null
}
```

**Mock Credentials:**
```typescript
const mockCredentials = [
  { id: '1', name: 'OpenAI Production Key', provider: 'OpenAI' },
  { id: '2', name: 'Anthropic Dev Key', provider: 'Anthropic' },
  { id: '3', name: 'Google Cloud Key', provider: 'Google' }
]
```

**Mock Models:**
```typescript
const mockModels = {
  OpenAI: [
    { id: 'gpt-4o', name: 'GPT-4o' },
    { id: 'gpt-4o-mini', name: 'GPT-4o Mini' },
    { id: 'gpt-3.5-turbo', name: 'GPT-3.5 Turbo' }
  ],
  Anthropic: [
    { id: 'claude-3-5-sonnet-20241022', name: 'Claude 3.5 Sonnet' },
    { id: 'claude-3-5-haiku-20241022', name: 'Claude 3.5 Haiku' }
  ],
  Google: [
    { id: 'gemini-1.5-pro', name: 'Gemini 1.5 Pro' },
    { id: 'gemini-1.5-flash', name: 'Gemini 1.5 Flash' }
  ]
}
```

---

## Technical Considerations

### ReactFlow Configuration

```typescript
const reactFlowOptions = {
  // Top-down layout
  defaultEdgeOptions: {
    type: 'smoothstep',
    animated: true,
    style: { stroke: '#94a3b8', strokeWidth: 2 }
  },

  // Connection rules
  connectionMode: ConnectionMode.Strict,
  connectionLineType: ConnectionLineType.SmoothStep,

  // Interaction
  snapToGrid: true,
  snapGrid: [15, 15],
  defaultZoom: 1,
  minZoom: 0.5,
  maxZoom: 2,

  // Node selection
  multiSelectionKeyCode: 'Shift',
  deleteKeyCode: ['Backspace', 'Delete']
}
```

### Node Handles

Each node should have:
- **Input handle** (top): For incoming connections
- **Output handle** (bottom): For outgoing connections

Exception:
- Start node: Only output handle (no input)
- End node: Only input handle (no output)

### Edge Validation

Don't allow:
- Start node receiving connections
- End node having outgoing connections
- Self-loops (node to itself)
- Duplicate edges (same source/target pair)

### Store Persistence

For development without API:
- Save editor state to localStorage on change (debounced)
- Load from localStorage on mount
- Key: `donkeywork-editor-${agentId}`
- Clear on navigation away

---

## Future Enhancements (Post-MVP)

1. **Undo/Redo**: Track state changes, allow revert
2. **Version History**: View past versions, restore
3. **Templates**: Pre-built workflow templates
4. **Copy/Paste**: Copy nodes between agents
5. **Collaboration**: Real-time multi-user editing
6. **Comments**: Annotate nodes with notes
7. **Grouping**: Group related nodes visually
8. **Subflows**: Collapse sections of workflow
9. **Testing**: Run agent with test input directly in editor
10. **Streaming Preview**: Show live LLM responses as you configure

---

## Success Criteria

- ✅ User can create workflows visually
- ✅ Top-down layout is intuitive
- ✅ Node palette is easy to use
- ✅ Properties panel configures nodes correctly
- ✅ Validation prevents invalid workflows
- ✅ Responsive on mobile/tablet/desktop
- ✅ Works with mock data (no API calls)
- ✅ State persists in localStorage
- ✅ Professional, polished UI

---

## Next Steps After UI Complete

1. Wire up agent API endpoints (CRUD)
2. Wire up version API endpoints
3. Implement save/publish functionality
4. Integrate with credential selection
5. Add test execution feature
6. Add streaming output viewer

