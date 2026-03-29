import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Globe } from 'lucide-react'

interface A2aServerPropertiesProps {
  nodeId: string
}

export function A2aServerProperties({ nodeId }: A2aServerPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])

  if (!config) return null

  const serverName = (config.a2aServerName as string) || 'Unknown'
  const serverId = (config.a2aServerId as string) || ''
  const serverDescription = (config.a2aServerDescription as string) || ''

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-teal-500 to-emerald-600 shadow-lg shadow-teal-500/25">
          <Globe className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{serverName}</div>
          <div className="text-xs text-muted-foreground font-mono">{serverId}</div>
        </div>
      </div>

      {serverDescription && (
        <div className="rounded-lg border border-border p-3">
          <p className="text-sm text-muted-foreground">{serverDescription}</p>
        </div>
      )}

      <p className="text-xs text-muted-foreground">
        This A2A server is connected to the agent. The remote agent will be available for
        delegation via the A2A protocol. Remove the node from the canvas to disconnect it.
      </p>
    </div>
  )
}
