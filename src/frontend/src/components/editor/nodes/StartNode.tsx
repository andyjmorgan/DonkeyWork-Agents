import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Play } from 'lucide-react'
import { BaseNode } from './BaseNode'

export const StartNode = memo(({ id, data, selected }: NodeProps) => {
  return (
    <BaseNode id={id} selected={selected} borderColor="border-green-500" canDelete={false}>
      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-green-500/10">
          <Play className="h-4 w-4 text-green-500" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">Start</div>
          <div className="text-xs text-muted-foreground">{data.label || 'start'}</div>
        </div>
      </div>

      {/* Output handle (bottom) */}
      <Handle
        type="source"
        position={Position.Bottom}
        className="!w-3 !h-3 !bg-green-500 !border-2 !border-background"
      />
    </BaseNode>
  )
})

StartNode.displayName = 'StartNode'
