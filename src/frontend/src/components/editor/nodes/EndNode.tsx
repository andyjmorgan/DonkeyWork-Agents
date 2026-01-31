import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Flag } from 'lucide-react'
import { BaseNode } from './BaseNode'

interface EndNodeData {
  label?: string
}

export const EndNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as EndNodeData
  return (
    <BaseNode id={id} selected={selected} borderColor="border-red-500" canDelete={false}>
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-red-500 !border-2 !border-background"
      />

      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-red-500/10">
          <Flag className="h-4 w-4 text-red-500" />
        </div>
        <div className="flex-1">
          <div className="font-medium text-sm">End</div>
          <div className="text-xs text-muted-foreground">{nodeData.label || 'end'}</div>
        </div>
      </div>
    </BaseNode>
  )
})

EndNode.displayName = 'EndNode'
