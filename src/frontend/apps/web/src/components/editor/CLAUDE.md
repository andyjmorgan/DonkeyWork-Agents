# Agent Builder - Editor Components

## Node Architecture

All workflow nodes follow a consistent pattern for visual design, behavior, and connections.

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

### 1. Create Node Component

```tsx
// src/components/editor/nodes/MyNode.tsx
import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Zap } from 'lucide-react'
import { BaseNode } from './BaseNode'

export interface MyNodeData {
  displayName: string
  // ... other data fields
}

export const MyNode = memo(({ id, data, selected }: NodeProps<MyNodeData>) => {
  return (
    <BaseNode id={id} selected={selected} borderColor="border-purple-500">
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-purple-500 !border-2 !border-background"
      />

      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
          <Zap className="h-4 w-4 text-purple-500" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">{data.displayName}</div>
          <div className="text-xs text-muted-foreground">My Node</div>
        </div>
      </div>

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

### 2. Export from index.ts

```tsx
// src/components/editor/nodes/index.ts
export { StartNode } from './StartNode'
export { ModelNode } from './ModelNode'
export { EndNode } from './EndNode'
export { ActionNode } from './ActionNode'
export { MyNode } from './MyNode'  // Add your node
```

### 3. Register in CanvasInner.tsx

```tsx
// src/components/editor/CanvasInner.tsx
import { StartNode, ModelNode, EndNode, ActionNode, MyNode } from './nodes'

const nodeTypes = useMemo(
  () => ({
    start: StartNode,
    model: ModelNode,
    end: EndNode,
    action: ActionNode,
    myNode: MyNode  // Register your node type
  }),
  []
)
```

### 4. Add to Editor Store

Update `src/store/editor.ts` to handle your node type:

```tsx
// Add config interface
export interface MyNodeConfig {
  name: string
  // ... other config fields
}

// Update NodeConfig union
export type NodeConfig =
  | StartNodeConfig
  | ModelNodeConfig
  | EndNodeConfig
  | ActionNodeConfig
  | MyNodeConfig  // Add your config

// Add case in addNode function
else if (type === 'myNode') {
  defaultConfig = {
    name: nodeName,
    // ... default values
  } as MyNodeConfig
  nodeData = {
    displayName: (config as any).displayName || '',
    // ... node data
  }
}
```

### 5. Add Properties Panel

Create `src/components/editor/properties/MyNodeProperties.tsx` and add to PropertiesPanel switch statement.

### 6. Add to Node Palette

Add your node to the appropriate section in `NodePalette.tsx`.

---

## Node State Management

### Node vs NodeConfig

- **Node** (ReactFlow): Visual representation on canvas
  - Contains: `id`, `type`, `position`, `data` (display info)
  - Managed by ReactFlow

- **NodeConfig** (Editor Store): Configuration/settings
  - Contains: `name`, and type-specific config
  - Stored in `nodeConfigurations` map keyed by node ID
  - Persisted to backend

### Updating Nodes

```tsx
// Update node configuration (persisted settings)
updateNodeConfig(nodeId, { name: 'new name' })

// Update node data (visual/display info)
updateNodeData(nodeId, { displayName: 'New Display Name' })
```

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

See `ActionNode.test.tsx` for testing patterns:

1. Wrap in ReactFlowProvider
2. Test rendering with different props
3. Test icon variations
4. Test selection states
5. Test parameter storage in data

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

### Existing Node Files

- **StartNode**: Simplest example, no delete button, only output handle
- **EndNode**: Only input handle, no delete button
- **ModelNode**: Full example with dynamic icons, two handles
- **ActionNode**: Full example with parameter support, two handles

### Related Files

- `BaseNode.tsx` - Wrapper component
- `CanvasInner.tsx` - Node type registration
- `PropertiesPanel.tsx` - Properties routing
- `NodePalette.tsx` - Node palette sections
- `editor.ts` (store) - State management
