import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Play } from 'lucide-react'
import { BaseNode } from './BaseNode'

interface StartNodeData {
  label?: string
}

export const StartNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as StartNodeData
  return (
    <BaseNode id={id} selected={selected} borderColor="border-green-500" canDelete={false}>
      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-green-500 to-emerald-600 shadow-lg shadow-green-500/25">
          <Play className="h-4 w-4 text-white" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">Start</div>
          <div className="text-xs text-muted-foreground">{nodeData.label || 'start'}</div>
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
