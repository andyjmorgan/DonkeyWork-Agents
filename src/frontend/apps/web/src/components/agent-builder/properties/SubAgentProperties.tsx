import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Label } from '@donkeywork/ui'
import { Bot } from 'lucide-react'

interface SubAgentPropertiesProps {
  nodeId: string
}

export function SubAgentProperties({ nodeId }: SubAgentPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])

  if (!config) return null

  const subAgentName = (config.subAgentName as string) || 'Unknown'
  const subAgentId = (config.subAgentId as string) || ''

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-rose-500 to-pink-600 shadow-lg shadow-rose-500/25">
          <Bot className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{subAgentName}</div>
          <div className="text-xs text-muted-foreground font-mono">{subAgentId}</div>
        </div>
      </div>

      <div className="space-y-2">
        <Label>Behavior</Label>
        <p className="text-sm text-muted-foreground">
          This agent will be available for delegation. Swarm tools (spawn, delegate, management)
          are automatically included when sub-agents are connected.
        </p>
      </div>
    </div>
  )
}
