import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import {
  Play,
  Flag,
  Globe,
  Mail,
  Database,
  File,
  Zap,
  FileText,
  Clock,
  Brain,
  type LucideIcon
} from 'lucide-react'
import { BaseNode } from './BaseNode'

/**
 * Node data passed from the schema/palette.
 * All display and behavior properties come from the backend schema.
 */
export interface SchemaNodeData {
  label: string           // Node instance name (e.g., "http_request_1")
  nodeType: string        // Backend NodeType (e.g., "HttpRequest", "Start")
  displayName: string     // Human-readable name (e.g., "HTTP Request")
  icon?: string           // Icon name from schema
  color?: string          // Color name from schema
  hasInputHandle?: boolean   // From schema - defaults to true
  hasOutputHandle?: boolean  // From schema - defaults to true
  canDelete?: boolean        // From schema - defaults to true
}

/**
 * Icon mapping from schema icon names to Lucide components.
 */
const iconMap: Record<string, LucideIcon> = {
  play: Play,
  flag: Flag,
  globe: Globe,
  mail: Mail,
  database: Database,
  file: File,
  zap: Zap,
  'file-text': FileText,
  clock: Clock,
  brain: Brain,
}

/**
 * Color schemes for nodes based on schema color names.
 * Maps to Tailwind classes for border, background gradient, text, and handle colors.
 */
const colorSchemes: Record<string, { border: string; bg: string; text: string; handle: string }> = {
  green: {
    border: 'border-green-500',
    bg: 'bg-gradient-to-br from-green-500 to-emerald-600 shadow-lg shadow-green-500/25',
    text: 'text-white',
    handle: '!bg-green-500'
  },
  orange: {
    border: 'border-orange-500',
    bg: 'bg-gradient-to-br from-orange-500 to-red-500 shadow-lg shadow-orange-500/25',
    text: 'text-white',
    handle: '!bg-orange-500'
  },
  blue: {
    border: 'border-blue-500',
    bg: 'bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25',
    text: 'text-white',
    handle: '!bg-blue-500'
  },
  purple: {
    border: 'border-purple-500',
    bg: 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25',
    text: 'text-white',
    handle: '!bg-purple-500'
  },
  cyan: {
    border: 'border-cyan-500',
    bg: 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25',
    text: 'text-white',
    handle: '!bg-cyan-500'
  },
  pink: {
    border: 'border-pink-500',
    bg: 'bg-gradient-to-br from-pink-500 to-rose-600 shadow-lg shadow-pink-500/25',
    text: 'text-white',
    handle: '!bg-pink-500'
  },
  emerald: {
    border: 'border-emerald-500',
    bg: 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25',
    text: 'text-white',
    handle: '!bg-emerald-500'
  },
  amber: {
    border: 'border-amber-500',
    bg: 'bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25',
    text: 'text-white',
    handle: '!bg-amber-500'
  },
  violet: {
    border: 'border-violet-500',
    bg: 'bg-gradient-to-br from-violet-500 to-purple-600 shadow-lg shadow-violet-500/25',
    text: 'text-white',
    handle: '!bg-violet-500'
  },
}

const defaultColorScheme = colorSchemes.violet

/**
 * Unified schema-driven node component.
 * All node types use this component - display is driven by schema data,
 * special behaviors (handles, delete) come from nodeTypeBehaviors lookup.
 */
export const SchemaNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as unknown as SchemaNodeData

  // Get icon from schema, default to Zap
  const Icon = iconMap[nodeData.icon || ''] || Zap

  // Get color scheme from schema, default to violet
  const colors = colorSchemes[nodeData.color || ''] || defaultColorScheme

  // Get behaviors from schema data (defaults to true if not specified)
  const canDelete = nodeData.canDelete !== false
  const hasInputHandle = nodeData.hasInputHandle !== false
  const hasOutputHandle = nodeData.hasOutputHandle !== false

  return (
    <BaseNode id={id} selected={selected} borderColor={colors.border} canDelete={canDelete}>
      {/* Input handle (top) - only if node type supports it */}
      {hasInputHandle && (
        <Handle
          type="target"
          position={Position.Top}
          className={`!w-3 !h-3 ${colors.handle} !border-2 !border-background`}
        />
      )}

      <div className="flex items-center gap-2">
        <div className={`flex h-8 w-8 items-center justify-center rounded-lg ${colors.bg}`}>
          <Icon className={`h-4 w-4 ${colors.text}`} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">{nodeData.displayName}</div>
          <div className="text-xs text-muted-foreground truncate">{nodeData.label}</div>
        </div>
      </div>

      {/* Output handle (bottom) - only if node type supports it */}
      {hasOutputHandle && (
        <Handle
          type="source"
          position={Position.Bottom}
          className={`!w-3 !h-3 ${colors.handle} !border-2 !border-background`}
        />
      )}
    </BaseNode>
  )
})

SchemaNode.displayName = 'SchemaNode'
