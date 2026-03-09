import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Label, Input, Textarea } from '@donkeywork/ui'

interface PromptPropertiesProps {
  nodeId: string
}

export function PromptProperties({ nodeId }: PromptPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])
  const updateNodeConfig = useAgentBuilderStore((s) => s.updateNodeConfig)
  const updateNodeLabel = useAgentBuilderStore((s) => s.updateNodeLabel)
  const nodes = useAgentBuilderStore((s) => s.nodes)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)

  if (!config) return null

  const node = nodes.find((n) => n.id === nodeId)
  const label = (node?.data?.label as string) || 'Prompt'
  const systemPrompt = (config.systemPrompt as string) || ''

  return (
    <div className="space-y-4 h-full">
      <div className="space-y-2">
        <Label>Label</Label>
        <Input
          value={label}
          onChange={(e) => updateNodeLabel(nodeId, e.target.value)}
          placeholder="e.g. System Instructions, Safety Rules..."
          disabled={isReadOnly}
        />
        <p className="text-xs text-muted-foreground">
          A name to identify this prompt on the canvas
        </p>
      </div>
      <div className="space-y-2 flex-1">
        <Label>System Prompt</Label>
        <Textarea
          value={systemPrompt}
          onChange={(e) => updateNodeConfig(nodeId, { systemPrompt: e.target.value })}
          placeholder="You are a helpful assistant..."
          className="min-h-[400px] font-mono text-sm resize-y"
          disabled={isReadOnly}
        />
      </div>
    </div>
  )
}
