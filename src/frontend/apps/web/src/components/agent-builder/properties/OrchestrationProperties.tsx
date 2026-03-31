import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Workflow } from 'lucide-react'

interface OrchestrationPropertiesProps {
  nodeId: string
}

export function OrchestrationProperties({ nodeId }: OrchestrationPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])

  if (!config) return null

  const name = (config.orchestrationName as string) || 'Unknown'
  const id = (config.orchestrationId as string) || ''
  const description = (config.orchestrationDescription as string) || ''

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-indigo-500 to-violet-600 shadow-lg shadow-indigo-500/25">
          <Workflow className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{name}</div>
          <div className="text-xs text-muted-foreground font-mono">{id}</div>
        </div>
      </div>

      {description && (
        <div className="rounded-lg border border-border p-3">
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
      )}

      <p className="text-xs text-muted-foreground">
        This orchestration is attached as a tool. The agent can invoke it during execution,
        passing input that matches the orchestration's input schema. Remove the node to detach it.
      </p>
    </div>
  )
}
