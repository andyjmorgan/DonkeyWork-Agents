import { useState, useEffect } from 'react'
import { Brain, FileText, Server, Zap, Box, Bot, type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { mcpServers, agentDefinitions, type McpServerSummary, type AgentDefinitionSummary } from '@donkeywork/api-client'

const iconMap: Record<string, LucideIcon> = {
  brain: Brain,
  'file-text': FileText,
  server: Server,
  zap: Zap,
  container: Box,
  bot: Bot,
}

const colorClasses: Record<string, { border: string; iconContainer: string; iconColor: string }> = {
  blue: {
    border: 'border-blue-500/30 bg-blue-500/5 hover:border-blue-500/50 hover:bg-blue-500/10',
    iconContainer: 'bg-gradient-to-br from-blue-500 to-indigo-600 shadow-lg shadow-blue-500/25',
    iconColor: 'text-white',
  },
  emerald: {
    border: 'border-emerald-500/30 bg-emerald-500/5 hover:border-emerald-500/50 hover:bg-emerald-500/10',
    iconContainer: 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25',
    iconColor: 'text-white',
  },
  purple: {
    border: 'border-purple-500/30 bg-purple-500/5 hover:border-purple-500/50 hover:bg-purple-500/10',
    iconContainer: 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25',
    iconColor: 'text-white',
  },
  amber: {
    border: 'border-amber-500/30 bg-amber-500/5 hover:border-amber-500/50 hover:bg-amber-500/10',
    iconContainer: 'bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25',
    iconColor: 'text-white',
  },
  cyan: {
    border: 'border-cyan-500/30 bg-cyan-500/5 hover:border-cyan-500/50 hover:bg-cyan-500/10',
    iconContainer: 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25',
    iconColor: 'text-white',
  },
  rose: {
    border: 'border-rose-500/30 bg-rose-500/5 hover:border-rose-500/50 hover:bg-rose-500/10',
    iconContainer: 'bg-gradient-to-br from-rose-500 to-pink-600 shadow-lg shadow-rose-500/25',
    iconColor: 'text-white',
  },
}

interface PaletteItem {
  key: string
  dragData: Record<string, unknown>
  displayName: string
  icon: string
  color: string
  isDisabled: boolean
}

interface ToolGroupDef {
  id: string
  label: string
  toolIds: string[]
}

const toolGroupDefinitions: ToolGroupDef[] = [
  { id: 'project_management', label: 'Project Management', toolIds: ['project_management'] },
]

export function AgentNodePalette() {
  const hasNodeOfType = useAgentBuilderStore((s) => s.hasNodeOfType)
  const nodes = useAgentBuilderStore((s) => s.nodes)
  const agentId = useAgentBuilderStore((s) => s.agentId)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)
  const [serverList, setServerList] = useState<McpServerSummary[]>([])
  const [agentList, setAgentList] = useState<AgentDefinitionSummary[]>([])

  useEffect(() => {
    mcpServers.list().then(setServerList).catch(console.error)
    agentDefinitions.list().then(setAgentList).catch(console.error)
  }, [])

  const handleDragStart = (event: React.DragEvent, item: PaletteItem) => {
    if (item.isDisabled) return
    event.dataTransfer.effectAllowed = 'move'
    event.dataTransfer.setData('application/json', JSON.stringify(item.dragData))
  }

  const hasMcpServer = (serverId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentMcpServer' && n.data?.mcpServerId === serverId
    )

  const hasToolGroup = (groupId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentToolGroup' && n.data?.toolGroupId === groupId
    )

  const hasSubAgent = (subAgentId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentSubAgent' && n.data?.subAgentId === subAgentId
    )

  const renderItem = (item: PaletteItem) => {
    const Icon = iconMap[item.icon] || Zap
    const colors = colorClasses[item.color] || colorClasses.purple

    return (
      <div
        key={item.key}
        draggable={!item.isDisabled}
        onDragStart={(e) => handleDragStart(e, item)}
        className={cn(
          'flex cursor-move items-center gap-3 rounded-xl border-2 p-3 transition-all',
          colors.border,
          item.isDisabled && 'cursor-not-allowed opacity-40'
        )}
        title={item.displayName}
      >
        <div
          className={cn(
            'flex h-8 w-8 shrink-0 items-center justify-center rounded-lg',
            colors.iconContainer
          )}
        >
          <Icon className={cn('h-4 w-4', colors.iconColor)} />
        </div>
        <div className="min-w-0 flex-1">
          <div className="text-sm font-medium truncate">{item.displayName}</div>
        </div>
      </div>
    )
  }

  // Core items
  const coreItems: PaletteItem[] = [
    {
      key: 'model',
      dragData: {
        nodeType: 'agentModel',
        displayName: 'Model',
        icon: 'brain',
        color: 'blue',
        canDelete: false,
      },
      displayName: 'Model',
      icon: 'brain',
      color: 'blue',
      isDisabled: isReadOnly || hasNodeOfType('agentModel'),
    },
    {
      key: 'prompt',
      dragData: {
        nodeType: 'agentPrompt',
        displayName: 'Prompt',
        icon: 'file-text',
        color: 'emerald',
        canDelete: true,
      },
      displayName: 'Prompt',
      icon: 'file-text',
      color: 'emerald',
      isDisabled: isReadOnly,
    },
  ]

  // MCP Server items (from API)
  const mcpItems: PaletteItem[] = serverList.map((server) => ({
    key: `mcp-${server.id}`,
    dragData: {
      nodeType: 'agentMcpServer',
      displayName: server.name,
      icon: 'server',
      color: 'purple',
      canDelete: true,
      mcpServerId: server.id,
      mcpServerName: server.name,
    },
    displayName: server.name,
    icon: 'server',
    color: 'purple',
    isDisabled: isReadOnly || hasMcpServer(server.id),
  }))

  // Tool Group items
  const toolGroupItems: PaletteItem[] = toolGroupDefinitions.map((tg) => ({
    key: `tg-${tg.id}`,
    dragData: {
      nodeType: 'agentToolGroup',
      displayName: tg.label,
      icon: 'zap',
      color: 'amber',
      canDelete: true,
      toolGroupId: tg.id,
      toolGroupName: tg.label,
      toolIds: tg.toolIds,
    },
    displayName: tg.label,
    icon: 'zap',
    color: 'amber',
    isDisabled: isReadOnly || hasToolGroup(tg.id),
  }))

  // Sub-agent items (from API, excluding self)
  const agentItems: PaletteItem[] = agentList
    .filter((a) => a.id !== agentId)
    .map((agent) => ({
      key: `agent-${agent.id}`,
      dragData: {
        nodeType: 'agentSubAgent',
        displayName: agent.name,
        icon: 'bot',
        color: 'rose',
        canDelete: true,
        subAgentId: agent.id,
        subAgentName: agent.name,
      },
      displayName: agent.name,
      icon: 'bot',
      color: 'rose',
      isDisabled: isReadOnly || hasSubAgent(agent.id),
    }))

  // Sandbox item
  const sandboxItem: PaletteItem = {
    key: 'sandbox',
    dragData: {
      nodeType: 'agentSandbox',
      displayName: 'Sandbox',
      icon: 'container',
      color: 'cyan',
      canDelete: true,
    },
    displayName: 'Sandbox',
    icon: 'container',
    color: 'cyan',
    isDisabled: isReadOnly || hasNodeOfType('agentSandbox'),
  }

  return (
    <div className="h-full overflow-y-auto space-y-6">
      {/* Core */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          Core
        </h3>
        <div className="space-y-2">{coreItems.map(renderItem)}</div>
      </div>

      {/* Agents */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          Agents
        </h3>
        <div className="space-y-2">
          {agentItems.length > 0 ? (
            agentItems.map(renderItem)
          ) : (
            <p className="text-xs text-muted-foreground px-1">No other agents available</p>
          )}
        </div>
      </div>

      {/* MCP Servers */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          MCP Servers
        </h3>
        <div className="space-y-2">
          {mcpItems.length > 0 ? (
            mcpItems.map(renderItem)
          ) : (
            <p className="text-xs text-muted-foreground px-1">No MCP servers configured</p>
          )}
        </div>
      </div>

      {/* Tool Groups */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          Tool Groups
        </h3>
        <div className="space-y-2">{toolGroupItems.map(renderItem)}</div>
      </div>

      {/* Capabilities */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          Capabilities
        </h3>
        <div className="space-y-2">{renderItem(sandboxItem)}</div>
      </div>
    </div>
  )
}
