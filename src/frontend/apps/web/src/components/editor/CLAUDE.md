# Agent Builder - Editor Components

## Node Architecture

All workflow nodes use a unified `SchemaNode` component that reads metadata from a centralized `nodeTypes.ts` registry. Visual design, behavior, and connections are driven by the registry rather than individual node components.

---

## Handle Positioning

### Flow Direction: Vertical (Top to Bottom)

All nodes use **vertical flow** with handles positioned at the top and bottom.

```
┌─────────────┐
│   ○ (top)   │  ← Input handle (target)
│             │
│   [Node]    │
│             │
│   ○ (bottom)│  ← Output handle (source)
└─────────────┘
```

### Handle Rules

1. **Start Node**: Only output handle (bottom)
2. **End Node**: Only input handle (top)
3. **Model/Action Nodes**: Input (top) + output (bottom)
4. **Never use left/right handles** - this breaks the vertical flow pattern

### Handle Styling

```tsx
<Handle
  type="target"  // or "source"
  position={Position.Top}  // or Position.Bottom
  className="!w-3 !h-3 !bg-[color] !border-2 !border-background"
/>
```

- **Size**: 3x3 (`!w-3 !h-3`)
- **Color**: Match node theme color (`!bg-green-500`, `!bg-blue-500`, etc.)
- **Border**: 2px white border for contrast (`!border-2 !border-background`)

---

## BaseNode Component

All nodes **must** use the `BaseNode` wrapper component for consistency.

### What BaseNode Provides

1. **Settings Button** (gear icon) - Opens properties panel on click
2. **Delete Button** (trash icon) - Deletes node with confirmation dialog
3. **Hover Effects** - Buttons appear on hover (top-right corner)
4. **Selection Ring** - Visual feedback when node is selected
5. **Consistent Layout** - Padding, borders, shadows, rounded corners
6. **Group Hover States** - Smooth transitions

### BaseNode Props

```tsx
interface BaseNodeProps {
  id: string              // Node ID from ReactFlow
  selected?: boolean      // Selection state from ReactFlow
  borderColor: string     // Tailwind border color class (e.g., "border-blue-500")
  children: ReactNode     // Node content (icon, label, handles)
  canDelete?: boolean     // Allow deletion (default: true, set false for Start/End)
}
```

### BaseNode Usage Example

```tsx
import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Zap } from 'lucide-react'
import { BaseNode } from './BaseNode'

export const MyNode = memo(({ id, data, selected }: NodeProps) => {
  return (
    <BaseNode id={id} selected={selected} borderColor="border-purple-500">
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-purple-500 !border-2 !border-background"
      />

      {/* Node content */}
      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
          <Zap className="h-4 w-4 text-purple-500" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">{data.displayName}</div>
          <div className="text-xs text-muted-foreground">My Node</div>
        </div>
      </div>

      {/* Output handle (bottom) */}
      <Handle
        type="source"
        position={Position.Bottom}
        className="!w-3 !h-3 !bg-purple-500 !border-2 !border-background"
      />
    </BaseNode>
  )
})

MyNode.displayName = 'MyNode'
```

---

## Node Color Conventions

Each node type has a dedicated color scheme from the DonkeyWork design system:

| Node Type | Color | Border Class | Gradient Background |
|-----------|-------|--------------|---------------------|
| Start | Green #22c55e | `border-green-500` | `from-green-500 to-emerald-600` |
| End | Orange #f97316 | `border-orange-500` | `from-orange-500 to-red-500` |
| Model | Blue #3b82f6 | `border-blue-500` | `from-blue-500 to-indigo-600` |
| Action | Purple #a855f7 | `border-purple-500` | `from-purple-500 to-fuchsia-600` |
| Utility | Cyan #22d3ee | `border-cyan-500` | `from-cyan-500 to-teal-600` |
| Condition | Yellow #eab308 | `border-amber-500` | `from-yellow-500 to-amber-600` |

### Color Usage (Design System)

```tsx
// Icon container with gradient and glow shadow
<div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25">
  <Icon className="h-4 w-4 text-white" />
</div>

// BaseNode border (50% opacity for softer look)
<BaseNode borderColor="border-purple-500">
  {/* ... */}
</BaseNode>

// Handle color
<Handle className="!bg-purple-500" />
```

---

## Node Content Structure

### Standard Layout

```tsx
<BaseNode id={id} selected={selected} borderColor="border-[color]">
  {/* 1. Input handle at top */}
  <Handle type="target" position={Position.Top} />

  {/* 2. Content area */}
  <div className="flex items-center gap-2">
    {/* Icon */}
    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-[color]/10">
      <Icon className="h-4 w-4 text-[color]" />
    </div>

    {/* Labels */}
    <div className="flex-1">
      <div className="font-medium text-sm">{title}</div>
      <div className="text-xs text-muted-foreground">{subtitle}</div>
    </div>
  </div>

  {/* 3. Output handle at bottom */}
  <Handle type="source" position={Position.Bottom} />
</BaseNode>
```

### Icon Container

- **Size**: 8x8 (`h-8 w-8`)
- **Shape**: Rounded square (`rounded-lg`)
- **Background**: Theme color at 10% opacity (`bg-[color]/10`)
- **Icon size**: 4x4 (`h-4 w-4`)
- **Icon color**: Full theme color (`text-[color]`)

### Text Labels

- **Primary label**: `font-medium text-sm` (node type or display name)
- **Secondary label**: `text-xs text-muted-foreground` (subtitle or node name)
- **Tertiary label**: `text-xs text-muted-foreground/70` (additional info, if needed)

---

## Creating a New Node Type

All nodes are rendered by the unified `SchemaNode` component. Adding a new node type means adding an entry to the `NODE_TYPES` registry in `nodeTypes.ts` - no new React component is needed.

### 1. Add to Node Type Registry (`nodeTypes.ts`)

```tsx
export const NODE_TYPES: Record<string, NodeTypeMetadata> = {
  // ... existing entries
  myNode: {
    type: 'myNode',
    displayName: 'My Node',
    description: 'Does something useful',
    icon: Zap,                              // Lucide icon
    borderColor: 'border-purple-500',
    iconBgColor: 'bg-purple-500/10',
    iconColor: 'text-purple-500',
    handleColor: '!bg-purple-500',
    category: 'utility',                    // 'flow' | 'ai' | 'utility' | 'action'
    canDelete: true,
    schemaSource: { type: 'local', schema: myNodeSchema },  // or backend endpoint
    showEditableName: true,
  },
}
```

### 2. Define Schema (if local)

For nodes with local config (not fetched from backend API):

```tsx
const myNodeSchema: LocalNodeSchema = {
  tabs: [{ name: 'Settings', order: 0 }],
  fields: [
    {
      name: 'myField',
      label: 'My Field',
      description: 'Description here',
      controlType: 'Text',
      propertyType: 'string',
      order: 0,
      tab: 'Settings',
      required: true,
    },
  ],
}
```

For nodes with backend-driven config, use `schemaSource: { type: 'backend-action', endpoint: (actionType) => \`/api/v1/...\` }`.

### 3. Properties Panel

The `SchemaPropertiesPanel` renders config fields automatically from the schema. No per-node properties component is needed unless custom rendering is required.

---

## Node State Management

### Node vs NodeConfig

- **Node** (ReactFlow): Visual representation on canvas
  - Contains: `id`, `type` (always `"schemaNode"`), `position`, `data` (SchemaNodeData)
  - `data.nodeType` holds the logical type (e.g., `"start"`, `"model"`, `"action"`)
  - Managed by ReactFlow via the unified `SchemaNode` component

- **NodeConfig** (Editor Store): Configuration/settings
  - Contains type-specific config fields driven by the schema
  - Stored in `nodeConfigurations` map keyed by node ID
  - Persisted to backend

---

## Common Patterns

### Conditional Delete Button

Start and End nodes should not be deletable:

```tsx
<BaseNode id={id} selected={selected} borderColor="border-green-500" canDelete={false}>
  {/* ... */}
</BaseNode>
```

### Dynamic Icons

```tsx
const getIcon = (iconName?: string) => {
  switch (iconName) {
    case 'globe':
      return Globe
    case 'mail':
      return Mail
    default:
      return Zap
  }
}

const Icon = getIcon(data.icon)
```

### Truncating Long Text

```tsx
<div className="flex-1 min-w-0">
  <div className="font-medium text-sm truncate">{data.displayName}</div>
  <div className="text-xs text-muted-foreground truncate">{data.subtitle}</div>
</div>
```

---

## Testing Nodes

See `nodes/ActionNode.test.tsx` for testing patterns:

1. Wrap in ReactFlowProvider
2. Test rendering with different data props
3. Test icon and color variations
4. Test selection states

---

## Don'ts

❌ **Don't** use left/right handles (breaks vertical flow)
❌ **Don't** skip BaseNode wrapper (loses settings/delete buttons)
❌ **Don't** use different handle sizes (inconsistent UX)
❌ **Don't** mix border colors within a node type
❌ **Don't** forget to memo your component
❌ **Don't** hardcode node IDs (use `id` prop from ReactFlow)

---

## Reference

### Key Files

- `nodes/SchemaNode.tsx` - Unified node component (renders all node types)
- `nodes/BaseNode.tsx` - Wrapper providing settings/delete buttons, selection ring
- `nodeTypes.ts` - Node type registry (colors, icons, schemas, categories)
- `CanvasInner.tsx` - ReactFlow canvas (registers single `schemaNode` type)
- `NodePalette.tsx` - Drag-to-add palette (fetches available types from API)
- `PropertiesPanel.tsx` - Properties routing for selected node
- `properties/SchemaPropertiesPanel.tsx` - Schema-driven config editor
- `properties/FieldRenderer.tsx` - Renders individual config fields
- `editor.ts` (store) - State management
