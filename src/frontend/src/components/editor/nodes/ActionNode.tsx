import { memo, useMemo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Globe, Mail, Database, File, Zap, Clock, type LucideIcon } from 'lucide-react'
import { BaseNode } from './BaseNode'

export interface ActionNodeData {
  label?: string
  actionType: string
  displayName: string
  icon?: string
  parameters?: Record<string, unknown>
}

const ACTION_ICONS: Record<string, LucideIcon> = {
  globe: Globe,
  mail: Mail,
  database: Database,
  file: File,
  clock: Clock,
}

// Get color scheme based on icon type - using design system gradients
const getColorScheme = (iconName?: string) => {
  switch (iconName) {
    case 'globe':
      return {
        border: 'border-purple-500',
        bg: 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25',
        text: 'text-white',
        handle: '!bg-purple-500'
      }
    case 'clock':
      return {
        border: 'border-cyan-500',
        bg: 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25',
        text: 'text-white',
        handle: '!bg-cyan-500'
      }
    case 'mail':
      return {
        border: 'border-pink-500',
        bg: 'bg-gradient-to-br from-pink-500 to-rose-600 shadow-lg shadow-pink-500/25',
        text: 'text-white',
        handle: '!bg-pink-500'
      }
    case 'database':
      return {
        border: 'border-emerald-500',
        bg: 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25',
        text: 'text-white',
        handle: '!bg-emerald-500'
      }
    case 'file':
      return {
        border: 'border-orange-500',
        bg: 'bg-gradient-to-br from-orange-500 to-amber-600 shadow-lg shadow-orange-500/25',
        text: 'text-white',
        handle: '!bg-orange-500'
      }
    default:
      return {
        border: 'border-violet-500',
        bg: 'bg-gradient-to-br from-violet-500 to-purple-600 shadow-lg shadow-violet-500/25',
        text: 'text-white',
        handle: '!bg-violet-500'
      }
  }
}

export const ActionNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as unknown as ActionNodeData
  const Icon = useMemo(() => ACTION_ICONS[nodeData.icon || ''] || Zap, [nodeData.icon])
  const colors = getColorScheme(nodeData.icon)

  return (
    <BaseNode id={id} selected={selected} borderColor={colors.border}>
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className={`!w-3 !h-3 ${colors.handle} !border-2 !border-background`}
      />

      <div className="flex items-center gap-2">
        <div className={`flex h-8 w-8 items-center justify-center rounded-lg ${colors.bg}`}>
          <Icon className={`h-4 w-4 ${colors.text}`} />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">{nodeData.displayName}</div>
          <div className="text-xs text-muted-foreground truncate">{nodeData.label || 'action'}</div>
        </div>
      </div>

      {/* Output handle (bottom) */}
      <Handle
        type="source"
        position={Position.Bottom}
        className={`!w-3 !h-3 ${colors.handle} !border-2 !border-background`}
      />
    </BaseNode>
  )
})

ActionNode.displayName = 'ActionNode'
