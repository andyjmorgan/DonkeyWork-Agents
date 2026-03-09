import { useAgentBuilderStore } from '@/store/agentBuilder'
import { Server } from 'lucide-react'

interface McpServerPropertiesProps {
  nodeId: string
}

export function McpServerProperties({ nodeId }: McpServerPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])

  if (!config) return null

  const serverName = (config.mcpServerName as string) || 'Unknown Server'
  const serverId = (config.mcpServerId as string) || ''

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25">
          <Server className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{serverName}</div>
          <div className="text-xs text-muted-foreground font-mono">{serverId}</div>
        </div>
      </div>
      <p className="text-xs text-muted-foreground">
        This MCP server is connected to the agent. Remove the node from the canvas to disconnect it.
      </p>
    </div>
  )
}
