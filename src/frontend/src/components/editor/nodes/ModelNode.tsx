import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import { BaseNode } from './BaseNode'

export const ModelNode = memo(({ id, data, selected }: NodeProps) => {
  // Select icon based on provider
  const getProviderIcon = () => {
    const provider = data.provider as string
    switch (provider) {
      case 'OpenAi':
      case 'OpenAI': // Backward compatibility
      case 'Azure':
        return <OpenAIIcon className="h-4 w-4 text-blue-500" />
      case 'Anthropic':
        return <AnthropicIcon className="h-4 w-4 text-blue-500" />
      case 'Google':
        return <GoogleIcon className="h-4 w-4 text-blue-500" />
      default:
        return <OpenAIIcon className="h-4 w-4 text-blue-500" />
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
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-500/10">
          {getProviderIcon()}
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">Model</div>
          <div className="text-xs text-muted-foreground truncate">
            {data.label || 'model'}
          </div>
          {data.modelName && (
            <div className="text-xs text-muted-foreground/70 truncate">
              {data.modelName}
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
