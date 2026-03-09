import { memo, useState, useEffect } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { Brain, FileText, Wrench, Bot } from 'lucide-react'
import { AgentBaseNode } from './AgentBaseNode'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { models, type ModelDefinition } from '@donkeywork/api-client'

export const AgentModelNode = memo(({ id, selected }: NodeProps) => {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[id])
  const [allModels, setAllModels] = useState<ModelDefinition[]>([])

  useEffect(() => {
    models
      .list()
      .then((data) => setAllModels(data.filter((m) => m.mode === 'Chat')))
      .catch(console.error)
  }, [])

  const modelId = (config?.modelId as string) || ''
  const selectedModel = allModels.find((m) => m.id === modelId)
  const modelName = selectedModel?.name || (modelId ? modelId : 'No model selected')
  const providerName = selectedModel?.provider || ''

  return (
    <AgentBaseNode id={id} selected={selected} borderColor="border-blue-500" canDelete={false}>
      {/* Prompts handle (top center, half inside the box) */}
      <Handle
        type="target"
        position={Position.Top}
        id="prompts"
        className="!w-5 !h-5 !bg-emerald-500 !border-2 !border-background !rounded-md flex items-center justify-center"
        title="Prompts"
      >
        <FileText className="h-3 w-3 text-white pointer-events-none" />
      </Handle>

      <div className="flex items-center gap-3 mt-1 min-w-[200px]">
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25">
          <Brain className="h-6 w-6 text-white" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-semibold text-sm">Model</div>
          <div className="text-sm text-foreground/80 truncate">{modelName}</div>
          {providerName && (
            <div className="text-xs text-muted-foreground">{providerName}</div>
          )}
        </div>
      </div>

      {/* Tools handle (left center, half inside the box) */}
      <Handle
        type="target"
        position={Position.Left}
        id="tools"
        className="!w-5 !h-5 !bg-purple-500 !border-2 !border-background !rounded-md flex items-center justify-center"
        title="Tools"
      >
        <Wrench className="h-3 w-3 text-white pointer-events-none" />
      </Handle>

      {/* Agents handle (right center, half inside the box) */}
      <Handle
        type="target"
        position={Position.Right}
        id="agents"
        className="!w-5 !h-5 !bg-rose-500 !border-2 !border-background !rounded-md flex items-center justify-center"
        title="Agents"
      >
        <Bot className="h-3 w-3 text-white pointer-events-none" />
      </Handle>
    </AgentBaseNode>
  )
})

AgentModelNode.displayName = 'AgentModelNode'
