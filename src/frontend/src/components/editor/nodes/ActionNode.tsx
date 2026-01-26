import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Globe, Mail, Database, File, Zap } from 'lucide-react'
import { BaseNode } from './BaseNode'

export interface ActionNodeData {
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
    default:
      return Zap
  }
}

export const ActionNode = memo(({ id, data, selected }: NodeProps<ActionNodeData>) => {
  const Icon = getActionIcon(data.icon)

  return (
    <BaseNode id={id} selected={selected} borderColor="border-purple-500">
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-purple-500 !border-2 !border-background"
      />

      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-purple-500/10">
          <Icon className="h-4 w-4 text-purple-500" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">{data.displayName}</div>
          <div className="text-xs text-muted-foreground">Action</div>
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

ActionNode.displayName = 'ActionNode'
