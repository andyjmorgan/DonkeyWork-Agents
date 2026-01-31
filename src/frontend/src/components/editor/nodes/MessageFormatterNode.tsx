import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { FileText } from 'lucide-react'
import { BaseNode } from './BaseNode'

export interface MessageFormatterNodeData {
  label?: string
}

export const MessageFormatterNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as MessageFormatterNodeData
  return (
    <BaseNode id={id} selected={selected} borderColor="border-amber-500">
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-amber-500 !border-2 !border-background"
      />

      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-amber-500/10">
          <FileText className="h-4 w-4 text-amber-500" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">Formatter</div>
          <div className="text-xs text-muted-foreground truncate">
            {nodeData.label || 'formatter'}
          </div>
        </div>
      </div>

      {/* Output handle (bottom) */}
      <Handle
        type="source"
        position={Position.Bottom}
        className="!w-3 !h-3 !bg-amber-500 !border-2 !border-background"
      />
    </BaseNode>
  )
})

MessageFormatterNode.displayName = 'MessageFormatterNode'
