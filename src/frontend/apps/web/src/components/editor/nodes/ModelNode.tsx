import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import { BaseNode } from './BaseNode'

interface ModelNodeData {
  label?: string
  provider?: string
  modelName?: string
}

export const ModelNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as ModelNodeData

  // Select icon based on provider
  const getProviderIcon = () => {
    switch (nodeData.provider) {
      case 'OpenAi':
      case 'OpenAI': // Backward compatibility
      case 'Azure':
        return <OpenAIIcon className="h-4 w-4 text-white" />
      case 'Anthropic':
        return <AnthropicIcon className="h-4 w-4 text-white" />
      case 'Google':
        return <GoogleIcon className="h-4 w-4 text-white" />
      default:
        return <OpenAIIcon className="h-4 w-4 text-white" />
    }
  }

  return (
    <BaseNode id={id} selected={selected} borderColor="border-blue-500">
      {/* Input handle (top) */}
      <Handle
        type="target"
        position={Position.Top}
        className="!w-3 !h-3 !bg-blue-500 !border-2 !border-background"
      />

      <div className="flex items-center gap-2">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25">
          {getProviderIcon()}
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">Model</div>
          <div className="text-xs text-muted-foreground truncate">
            {nodeData.label || 'model'}
          </div>
          {nodeData.modelName && (
            <div className="text-xs text-muted-foreground/70 truncate">
              {nodeData.modelName}
            </div>
          )}
        </div>
      </div>

      {/* Output handle (bottom) */}
      <Handle
        type="source"
        position={Position.Bottom}
        className="!w-3 !h-3 !bg-blue-500 !border-2 !border-background"
      />
    </BaseNode>
  )
})

ModelNode.displayName = 'ModelNode'
