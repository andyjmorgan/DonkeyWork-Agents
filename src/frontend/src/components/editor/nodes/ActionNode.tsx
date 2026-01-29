import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Globe, Mail, Database, File, Zap, Clock } from 'lucide-react'
import { BaseNode } from './BaseNode'

export interface ActionNodeData {
  label?: string
  actionType: string
  displayName: string
  icon?: string
  parameters?: Record<string, any>
}

const getActionIcon = (iconName?: string) => {
  switch (iconName) {
    case 'globe':
      return Globe
    case 'mail':
      return Mail
    case 'database':
      return Database
    case 'file':
      return File
    case 'clock':
      return Clock
    default:
      return Zap
  }
}

// Get color scheme based on icon type
const getColorScheme = (iconName?: string) => {
  switch (iconName) {
    case 'globe':
      return {
        border: 'border-purple-500',
        bg: 'bg-purple-500/10',
        text: 'text-purple-500',
        handle: '!bg-purple-500'
      }
    case 'clock':
      return {
        border: 'border-cyan-500',
        bg: 'bg-cyan-500/10',
        text: 'text-cyan-500',
        handle: '!bg-cyan-500'
      }
    case 'mail':
      return {
        border: 'border-pink-500',
        bg: 'bg-pink-500/10',
        text: 'text-pink-500',
        handle: '!bg-pink-500'
      }
    case 'database':
      return {
        border: 'border-emerald-500',
        bg: 'bg-emerald-500/10',
        text: 'text-emerald-500',
        handle: '!bg-emerald-500'
      }
    case 'file':
      return {
        border: 'border-orange-500',
        bg: 'bg-orange-500/10',
        text: 'text-orange-500',
        handle: '!bg-orange-500'
      }
    default:
      return {
        border: 'border-violet-500',
        bg: 'bg-violet-500/10',
        text: 'text-violet-500',
        handle: '!bg-violet-500'
      }
  }
}

export const ActionNode = memo(({ id, data, selected }: NodeProps<ActionNodeData>) => {
  const Icon = getActionIcon(data.icon)
  const colors = getColorScheme(data.icon)

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
          <div className="font-medium text-sm">{data.displayName}</div>
          <div className="text-xs text-muted-foreground truncate">{data.label || 'action'}</div>
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
