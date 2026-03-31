import { useState, useEffect, useCallback } from 'react'
import { Brain, FileText, Server, Zap, Box, Bot, Globe, Plus, Workflow, type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { mcpServers, a2aServers, agentDefinitions, orchestrations as orchestrationsApi, prompts as promptsApi, type McpServerSummary, type A2aServerSummary, type AgentDefinitionSummary, type PromptSummary, type Orchestration } from '@donkeywork/api-client'
import { CreatePromptDialog } from './CreatePromptDialog'
import { McpServerTestDialog } from '@/components/mcp/McpServerTestDialog'

const iconMap: Record<string, LucideIcon> = {
  brain: Brain,
  'file-text': FileText,
  server: Server,
  zap: Zap,
  container: Box,
  bot: Bot,
  workflow: Workflow,
  globe: Globe,
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
  teal: {
    border: 'border-teal-500/30 bg-teal-500/5 hover:border-teal-500/50 hover:bg-teal-500/10',
    iconContainer: 'bg-gradient-to-br from-teal-500 to-emerald-600 shadow-lg shadow-teal-500/25',
    iconColor: 'text-white',
  },
  orange: {
    border: 'border-orange-500/30 bg-orange-500/5 hover:border-orange-500/50 hover:bg-orange-500/10',
    iconContainer: 'bg-gradient-to-br from-orange-500 to-red-600 shadow-lg shadow-orange-500/25',
    iconColor: 'text-white',
  },
  indigo: {
    border: 'border-indigo-500/30 bg-indigo-500/5 hover:border-indigo-500/50 hover:bg-indigo-500/10',
    iconContainer: 'bg-gradient-to-br from-indigo-500 to-violet-600 shadow-lg shadow-indigo-500/25',
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
  mcpServerId?: string
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
  const [a2aServerList, setA2aServerList] = useState<A2aServerSummary[]>([])
  const [agentList, setAgentList] = useState<AgentDefinitionSummary[]>([])
  const [promptList, setPromptList] = useState<PromptSummary[]>([])
  const [orchestrationList, setOrchestrationList] = useState<Orchestration[]>([])
  const [createPromptOpen, setCreatePromptOpen] = useState(false)
  const [testingMcpServer, setTestingMcpServer] = useState<{ id: string; name: string } | null>(null)

  const refreshPrompts = useCallback(() => {
    promptsApi.list().then(setPromptList).catch(console.error)
  }, [])

  useEffect(() => {
    mcpServers.list().then(setServerList).catch(console.error)
    a2aServers.list().then(setA2aServerList).catch(console.error)
    agentDefinitions.list().then(setAgentList).catch(console.error)
    orchestrationsApi.list().then(setOrchestrationList).catch(console.error)
    refreshPrompts()
  }, [refreshPrompts])

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

  const hasA2aServer = (a2aServerId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentA2aServer' && n.data?.a2aServerId === a2aServerId
    )

  const hasOrchestration = (orchestrationId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentOrchestration' && n.data?.orchestrationId === orchestrationId
    )

  const hasPrompt = (promptId: string) =>
    nodes.some(
      (n) => n.data?.nodeType === 'agentPrompt' && n.data?.promptId === promptId
    )

  const renderItem = (item: PaletteItem) => {
    const Icon = iconMap[item.icon] || Zap
    const colors = colorClasses[item.color] || colorClasses.purple

    return (
      <div
        key={item.key}
        draggable={!item.isDisabled}
        onDragStart={(e) => handleDragStart(e, item)}
        onClick={() => {
          if (item.mcpServerId) {
            setTestingMcpServer({ id: item.mcpServerId, name: item.displayName })
          }
        }}
        className={cn(
          'flex items-center gap-3 rounded-xl border-2 p-3 transition-all',
          item.mcpServerId ? 'cursor-pointer' : 'cursor-move',
          colors.border,
          item.isDisabled && 'cursor-not-allowed opacity-40'
        )}
        title={item.mcpServerId ? `Click to preview tools from ${item.displayName}` : item.displayName}
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
  ]

  const sortByName = (a: PaletteItem, b: PaletteItem) =>
    a.displayName.localeCompare(b.displayName)

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
    mcpServerId: server.id,
  })).sort(sortByName)

  // Prompt items (from API)
  const promptItems: PaletteItem[] = promptList.map((prompt) => ({
    key: `prompt-${prompt.id}`,
    dragData: {
      nodeType: 'agentPrompt',
      displayName: prompt.name,
      icon: 'file-text',
      color: 'emerald',
      canDelete: true,
      promptId: prompt.id,
      promptName: prompt.name,
    },
    displayName: prompt.name,
    icon: 'file-text',
    color: 'emerald',
    isDisabled: isReadOnly || hasPrompt(prompt.id),
  })).sort(sortByName)

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
    })).sort(sortByName)

  // A2A Server items (from API)
  const a2aItems: PaletteItem[] = a2aServerList.map((server) => ({
    key: `a2a-${server.id}`,
    dragData: {
      nodeType: 'agentA2aServer',
      displayName: server.name,
      icon: 'bot',
      color: 'orange',
      canDelete: true,
      a2aServerId: server.id,
      a2aServerName: server.name,
      a2aServerDescription: server.description,
    },
    displayName: server.name,
    icon: 'bot',
    color: 'orange',
    isDisabled: isReadOnly || hasA2aServer(server.id),
  })).sort(sortByName)

  // Orchestration items (from API)
  const orchestrationItems: PaletteItem[] = orchestrationList.map((orch) => ({
    key: `orch-${orch.id}`,
    dragData: {
      nodeType: 'agentOrchestration',
      displayName: orch.name,
      icon: 'workflow',
      color: 'indigo',
      canDelete: true,
      orchestrationId: orch.id,
      orchestrationName: orch.name,
      orchestrationDescription: orch.description,
    },
    displayName: orch.name,
    icon: 'workflow',
    color: 'indigo',
    isDisabled: isReadOnly || hasOrchestration(orch.id),
  })).sort(sortByName)

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

      {/* Prompts */}
      <div>
        <div className="flex items-center justify-between mb-2 px-1">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
            Prompts
          </h3>
          {!isReadOnly && (
            <button
              onClick={() => setCreatePromptOpen(true)}
              className="flex h-5 w-5 items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
              title="Create new prompt"
            >
              <Plus className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
        <div className="space-y-2">
          {promptItems.length > 0 ? (
            promptItems.map(renderItem)
          ) : (
            <p className="text-xs text-muted-foreground px-1">No prompts in library</p>
          )}
        </div>
      </div>

      <CreatePromptDialog
        open={createPromptOpen}
        onOpenChange={setCreatePromptOpen}
        onCreated={refreshPrompts}
      />

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

      {/* A2A Servers */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          A2A Servers
        </h3>
        <div className="space-y-2">
          {a2aItems.length > 0 ? (
            a2aItems.map(renderItem)
          ) : (
            <p className="text-xs text-muted-foreground px-1">No A2A servers configured</p>
          )}
        </div>
      </div>

      {/* Orchestrations */}
      <div>
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 px-1">
          Orchestrations
        </h3>
        <div className="space-y-2">
          {orchestrationItems.length > 0 ? (
            orchestrationItems.map(renderItem)
          ) : (
            <p className="text-xs text-muted-foreground px-1">No orchestrations created</p>
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

      {testingMcpServer && (
        <McpServerTestDialog
          open={!!testingMcpServer}
          onOpenChange={(open) => { if (!open) setTestingMcpServer(null) }}
          serverId={testingMcpServer.id}
          serverName={testingMcpServer.name}
        />
      )}
    </div>
  )
}
