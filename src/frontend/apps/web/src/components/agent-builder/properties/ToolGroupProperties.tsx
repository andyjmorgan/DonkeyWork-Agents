import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Zap } from 'lucide-react'

interface ToolGroupPropertiesProps {
  nodeId: string
}

const toolDescriptions: Record<string, string> = {
  swarm_spawn: 'Spawn new agents in a swarm',
  swarm_delegate: 'Delegate tasks to other agents',
  swarm_management: 'Manage swarm lifecycle',
  project_management: 'Tasks, notes, milestones',
}

export function ToolGroupProperties({ nodeId }: ToolGroupPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])

  if (!config) return null

  const groupName = (config.toolGroupName as string) || 'Unknown'
  const toolIds = (config.toolIds as string[]) || []

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25">
          <Zap className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{groupName}</div>
          <div className="text-xs text-muted-foreground">
            {toolIds.length} tool{toolIds.length !== 1 ? 's' : ''} included
          </div>
        </div>
      </div>

      {toolIds.length > 0 && (
        <div className="space-y-1">
          {toolIds.map((id) => (
            <div key={id} className="flex items-center gap-2 rounded-md border border-border/50 px-3 py-2">
              <Zap className="h-3 w-3 text-amber-500" />
              <div>
                <div className="text-sm font-medium">{id.replace(/_/g, ' ')}</div>
                {toolDescriptions[id] && (
                  <div className="text-xs text-muted-foreground">{toolDescriptions[id]}</div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <p className="text-xs text-muted-foreground">
        Remove the node from the canvas to detach this tool group.
      </p>
    </div>
  )
}
