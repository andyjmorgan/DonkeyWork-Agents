import { create } from 'zustand'
import type { Node, Edge, Viewport, NodeChange, EdgeChange, Connection } from '@xyflow/react'
import { agentNodeTypes } from '@/components/agent-builder/agentNodeTypes'
import type { AgentContractV1, AgentDefinitionDetails, ReasoningEffort, McpServerReference, SubAgentReference } from '@donkeywork/api-client'

export interface AgentNodeConfig {
  type: string
  [key: string]: unknown
}

function generateGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

interface AgentBuilderState {
  // Agent metadata
  agentId: string | null
  agentName: string
  agentDescription: string
  isReadOnly: boolean
  isSystem: boolean

  // Agent-level settings
  lifecycle: 'Task' | 'Linger'
  lingerSeconds: number
  timeoutSeconds: number
  persistMessages: boolean
  connectToNavi: boolean

  // ReactFlow state
  nodes: Node[]
  edges: Edge[]
  viewport: Viewport

  // Node configurations
  nodeConfigurations: Record<string, AgentNodeConfig>

  // UI state
  selectedNodeId: string | null
  isPaletteOpen: boolean
  isPropertiesOpen: boolean
  showingAgentSettings: boolean

  // Actions
  setAgentMetadata: (name: string, description: string) => void
  setAgentSettings: (settings: {
    lifecycle?: 'Task' | 'Linger'
    lingerSeconds?: number
    timeoutSeconds?: number
    persistMessages?: boolean
    connectToNavi?: boolean
  }) => void
  setNodes: (nodes: Node[]) => void
  setEdges: (edges: Edge[]) => void
  onNodesChange: (changes: NodeChange[]) => void
  onEdgesChange: (changes: EdgeChange[]) => void
  onConnect: (connection: Connection) => void
  addNode: (position: { x: number; y: number }, schemaInfo: Record<string, unknown>) => void
  removeNode: (nodeId: string) => void
  updateNodeConfig: (nodeId: string, config: Partial<AgentNodeConfig>) => void
  updateNodeLabel: (nodeId: string, label: string) => void
  selectNode: (nodeId: string | null) => void
  showAgentSettings: () => void
  togglePalette: () => void
  setViewport: (viewport: Viewport) => void
  arrangeNodes: () => void
  reset: () => void
  loadAgent: (details: AgentDefinitionDetails) => void
  serializeToContract: () => AgentContractV1
  exportToJson: () => string
  save: () => Promise<void>

  // Helpers
  getModelNodeId: () => string | null
  hasNodeOfType: (type: string) => boolean
}

function createModelNode(id: string): Node {
  return {
    id,
    type: 'agentModel',
    position: { x: 400, y: 300 },
    data: {
      nodeType: 'agentModel',
    },
  }
}

function createInitialState() {
  const modelId = generateGuid()

  return {
    agentId: null,
    agentName: 'Untitled Agent',
    agentDescription: '',
    isReadOnly: false,
    isSystem: false,
    lifecycle: 'Task' as const,
    lingerSeconds: 60,
    timeoutSeconds: 300,
    persistMessages: false,
    connectToNavi: false,
    nodes: [createModelNode(modelId)] as Node[],
    edges: [] as Edge[],
    viewport: { x: 0, y: 0, zoom: 1 },
    nodeConfigurations: {
      [modelId]: {
        type: 'agentModel',
        modelId: '',
        maxTokens: 4096,
        reasoningEffort: '',
        stream: true,
        webSearch: false,
        webFetch: false,
      },
    } as Record<string, AgentNodeConfig>,
    selectedNodeId: null,
    isPaletteOpen: true,
    isPropertiesOpen: false,
    showingAgentSettings: false,
  }
}

/** Determine which handle on the Model a given node type connects to */
function getTargetHandle(nodeType: string): string {
  const info = agentNodeTypes[nodeType]
  return info?.targetHandle || 'tools'
}

export const useAgentBuilderStore = create<AgentBuilderState>((set, get) => ({
  ...createInitialState(),

  setAgentMetadata: (name, description) => {
    set({ agentName: name, agentDescription: description })
  },

  setAgentSettings: (settings) => {
    set(settings)
  },

  setNodes: (nodes) => set({ nodes }),
  setEdges: (edges) => set({ edges }),

  onNodesChange: (changes) => {
    const { nodes } = get()
    set({
      nodes: nodes
        .map((node) => {
          const change = changes.find((c) => 'id' in c && c.id === node.id)
          if (!change) return node
          if (change.type === 'position' && 'position' in change && change.position) {
            return { ...node, position: change.position }
          }
          if (change.type === 'select' && 'selected' in change) {
            return { ...node, selected: change.selected }
          }
          if (change.type === 'remove') return null
          return node
        })
        .filter(Boolean) as Node[],
    })
  },

  onEdgesChange: (changes) => {
    const { edges } = get()
    set({
      edges: edges
        .map((edge) => {
          const change = changes.find((c) => 'id' in c && c.id === edge.id)
          if (!change) return edge
          if (change.type === 'select' && 'selected' in change) {
            return { ...edge, selected: change.selected }
          }
          if (change.type === 'remove') return null
          return edge
        })
        .filter(Boolean) as Edge[],
    })
  },

  onConnect: (connection) => {
    const { edges } = get()
    set({
      edges: [
        ...edges,
        {
          id: generateGuid(),
          source: connection.source,
          target: connection.target,
          sourceHandle: connection.sourceHandle,
          targetHandle: connection.targetHandle,
          type: 'default' as const,
          animated: true,
        },
      ],
    })
  },

  addNode: (position, schemaInfo) => {
    const { nodes, edges, nodeConfigurations } = get()
    const nodeType = schemaInfo.nodeType as string

    // For nodes with maxInstances, check count
    const typeInfo = agentNodeTypes[nodeType]
    if (typeInfo?.maxInstances) {
      const count = nodes.filter((n) => n.data?.nodeType === nodeType).length
      if (count >= typeInfo.maxInstances) return
    }

    // For MCP servers, check if this specific server is already on the canvas
    if (nodeType === 'agentMcpServer' && schemaInfo.mcpServerId) {
      const exists = nodes.some(
        (n) =>
          n.data?.nodeType === 'agentMcpServer' &&
          n.data?.mcpServerId === schemaInfo.mcpServerId
      )
      if (exists) return
    }

    // For tool groups, check if this specific group is already on the canvas
    if (nodeType === 'agentToolGroup' && schemaInfo.toolGroupId) {
      const exists = nodes.some(
        (n) =>
          n.data?.nodeType === 'agentToolGroup' &&
          n.data?.toolGroupId === schemaInfo.toolGroupId
      )
      if (exists) return
    }

    // For prompts, check if this specific prompt is already on the canvas
    if (nodeType === 'agentPrompt' && schemaInfo.promptId) {
      const exists = nodes.some(
        (n) =>
          n.data?.nodeType === 'agentPrompt' &&
          n.data?.promptId === schemaInfo.promptId
      )
      if (exists) return
    }

    // For sub-agents, check if this specific agent is already on the canvas
    if (nodeType === 'agentSubAgent' && schemaInfo.subAgentId) {
      const exists = nodes.some(
        (n) =>
          n.data?.nodeType === 'agentSubAgent' &&
          n.data?.subAgentId === schemaInfo.subAgentId
      )
      if (exists) return
    }

    const nodeId = generateGuid()

    // Model node uses its own component type; everything else uses agentSatellite
    const reactFlowType = nodeType === 'agentModel' ? 'agentModel' : 'agentSatellite'

    const newNode: Node = {
      id: nodeId,
      type: reactFlowType,
      position,
      data: {
        label: schemaInfo.displayName || nodeType,
        nodeType,
        displayName: schemaInfo.displayName,
        icon: schemaInfo.icon,
        color: schemaInfo.color,
        canDelete: schemaInfo.canDelete,
        // Store identifiers in node data for palette disable checks
        mcpServerId: schemaInfo.mcpServerId,
        toolGroupId: schemaInfo.toolGroupId,
        subAgentId: schemaInfo.subAgentId,
        promptId: schemaInfo.promptId,
      },
    }

    // Default config per type
    let config: AgentNodeConfig = { type: nodeType }
    if (nodeType === 'agentMcpServer') {
      config = {
        type: nodeType,
        mcpServerId: schemaInfo.mcpServerId as string,
        mcpServerName: schemaInfo.mcpServerName as string,
      }
    } else if (nodeType === 'agentToolGroup') {
      config = {
        type: nodeType,
        toolGroupId: schemaInfo.toolGroupId as string,
        toolGroupName: schemaInfo.toolGroupName as string,
        toolIds: (schemaInfo.toolIds as string[]) || [],
      }
    } else if (nodeType === 'agentSubAgent') {
      config = {
        type: nodeType,
        subAgentId: schemaInfo.subAgentId as string,
        subAgentName: schemaInfo.subAgentName as string,
      }
    } else if (nodeType === 'agentPrompt') {
      config = {
        type: nodeType,
        promptId: schemaInfo.promptId as string,
        promptName: schemaInfo.promptName as string,
      }
    } else if (nodeType === 'agentSandbox') {
      config = { type: nodeType, enabled: true }
    }

    // Auto-connect to model node with the correct handle
    const modelNodeId = get().getModelNodeId()
    const targetHandle = getTargetHandle(nodeType)
    const newEdge = modelNodeId
      ? {
          id: generateGuid(),
          source: nodeId,
          target: modelNodeId,
          targetHandle,
          type: 'default' as const,
          animated: true,
        }
      : null

    set({
      nodes: [...nodes, newNode],
      edges: newEdge ? [...edges, newEdge] : edges,
      nodeConfigurations: { ...nodeConfigurations, [nodeId]: config },
    })
  },

  removeNode: (nodeId) => {
    const { nodes, edges, nodeConfigurations } = get()
    const newConfigs = { ...nodeConfigurations }
    delete newConfigs[nodeId]
    set({
      nodes: nodes.filter((n) => n.id !== nodeId),
      edges: edges.filter((e) => e.source !== nodeId && e.target !== nodeId),
      nodeConfigurations: newConfigs,
      selectedNodeId: null,
    })
  },

  updateNodeConfig: (nodeId, config) => {
    const { nodeConfigurations } = get()
    set({
      nodeConfigurations: {
        ...nodeConfigurations,
        [nodeId]: { ...nodeConfigurations[nodeId], ...config },
      },
    })
  },

  updateNodeLabel: (nodeId, label) => {
    const { nodes } = get()
    set({
      nodes: nodes.map((n) =>
        n.id === nodeId ? { ...n, data: { ...n.data, label } } : n
      ),
    })
  },

  selectNode: (nodeId) => {
    set({ selectedNodeId: nodeId, isPropertiesOpen: nodeId !== null, showingAgentSettings: false })
  },

  showAgentSettings: () => {
    set({ selectedNodeId: null, isPropertiesOpen: true, showingAgentSettings: true })
  },

  togglePalette: () => set((s) => ({ isPaletteOpen: !s.isPaletteOpen })),

  setViewport: (viewport) => set({ viewport }),

  arrangeNodes: () => {
    const { nodes } = get()
    if (nodes.length === 0) return

    const modelNode = nodes.find((n) => n.data?.nodeType === 'agentModel')
    if (!modelNode) return

    const promptNodes = nodes.filter((n) => n.data?.nodeType === 'agentPrompt')
    const toolNodes = nodes.filter(
      (n) =>
        n.data?.nodeType === 'agentMcpServer' ||
        n.data?.nodeType === 'agentToolGroup' ||
        n.data?.nodeType === 'agentSandbox'
    )
    const agentNodes = nodes.filter((n) => n.data?.nodeType === 'agentSubAgent')

    const centerX = 400
    const centerY = 300
    const radius = 250

    const fanOut = (items: Node[], startAngle: number, endAngle: number) => {
      return items.map((node, idx) => {
        if (items.length === 1) {
          const midAngle = (startAngle + endAngle) / 2
          return {
            ...node,
            position: {
              x: centerX + radius * Math.cos(midAngle),
              y: centerY + radius * Math.sin(midAngle),
            },
          }
        }
        const angle = startAngle + ((endAngle - startAngle) * idx) / (items.length - 1)
        return {
          ...node,
          position: {
            x: centerX + radius * Math.cos(angle),
            y: centerY + radius * Math.sin(angle),
          },
        }
      })
    }

    const updatedNodes = nodes.map((node) => {
      if (node.id === modelNode.id) {
        return { ...node, position: { x: centerX, y: centerY } }
      }

      // Prompt nodes fan out above the model (-PI*0.6 to -PI*0.4 i.e. top arc)
      const promptArranged = fanOut(promptNodes, -Math.PI * 0.6, -Math.PI * 0.4)
      const promptMatch = promptArranged.find((n) => n.id === node.id)
      if (promptMatch) return promptMatch

      // Tool nodes fan out to the left (PI*0.6 to PI*1.4)
      const toolArranged = fanOut(toolNodes, Math.PI * 0.6, Math.PI * 1.4)
      const toolMatch = toolArranged.find((n) => n.id === node.id)
      if (toolMatch) return toolMatch

      // Agent nodes fan out to the right (-PI*0.4 to PI*0.4)
      const agentArranged = fanOut(agentNodes, -Math.PI * 0.4, Math.PI * 0.4)
      const agentMatch = agentArranged.find((n) => n.id === node.id)
      if (agentMatch) return agentMatch

      return node
    })

    set({ nodes: updatedNodes })
  },

  reset: () => set(createInitialState()),

  loadAgent: (details) => {
    const contract = details.contract || {}
    const agentSettings = {
      lifecycle: (contract.lifecycle as 'Task' | 'Linger') || 'Task',
      lingerSeconds: (contract.lingerSeconds as number) || 60,
      timeoutSeconds: (contract.timeoutSeconds as number) || 300,
      persistMessages: (contract.persistMessages as boolean) ?? false,
      connectToNavi: (details.connectToNavi as boolean) ?? false,
    }

    if (details.reactFlowData && details.nodeConfigurations) {
      set({
        agentId: details.id,
        agentName: details.name,
        agentDescription: details.description || '',
        isReadOnly: details.isSystem,
        isSystem: details.isSystem,
        ...agentSettings,
        nodes: details.reactFlowData.nodes as Node[],
        edges: details.reactFlowData.edges as Edge[],
        viewport: details.reactFlowData.viewport,
        nodeConfigurations: details.nodeConfigurations as Record<string, AgentNodeConfig>,
        selectedNodeId: null,
        isPaletteOpen: true,
        isPropertiesOpen: false,
      })
    } else {
      const initial = createInitialState()
      set({
        ...initial,
        agentId: details.id,
        agentName: details.name,
        agentDescription: details.description || '',
        isReadOnly: details.isSystem,
        isSystem: details.isSystem,
        ...agentSettings,
      })
    }
  },

  serializeToContract: () => {
    const state = get()
    const { nodes, nodeConfigurations } = state

    let contract: AgentContractV1 = {
      lifecycle: state.lifecycle,
      lingerSeconds: state.lingerSeconds,
      timeoutSeconds: state.timeoutSeconds,
      persistMessages: state.persistMessages,
    }

    // Model node config
    const modelNode = nodes.find((n) => n.data?.nodeType === 'agentModel')
    if (modelNode) {
      const cfg = nodeConfigurations[modelNode.id]
      if (cfg) {
        contract = {
          ...contract,
          modelId: cfg.modelId as string,
          maxTokens: cfg.maxTokens as number,
          reasoningEffort: ((cfg.reasoningEffort as string) || 'None') as ReasoningEffort,
          stream: cfg.stream as boolean,
          webSearch: typeof cfg.webSearch === 'object'
            ? cfg.webSearch as { enabled: boolean; maxUses: number }
            : { enabled: !!cfg.webSearch, maxUses: 5 },
          webFetch: typeof cfg.webFetch === 'object'
            ? cfg.webFetch as { enabled: boolean; maxUses: number }
            : { enabled: !!cfg.webFetch, maxUses: 5 },
        }
      }
    }

    // Prompt nodes — collect prompt IDs
    const promptNodes = nodes.filter((n) => n.data?.nodeType === 'agentPrompt')
    const promptIds = promptNodes
      .map((n) => nodeConfigurations[n.id]?.promptId as string)
      .filter(Boolean)
    if (promptIds.length > 0) contract.prompts = promptIds

    // MCP Servers — each node is one server, include id + name + description
    const mcpNodes = nodes.filter((n) => n.data?.nodeType === 'agentMcpServer')
    const mcpRefs: McpServerReference[] = mcpNodes
      .map((n) => {
        const cfg = nodeConfigurations[n.id]
        if (!cfg?.mcpServerId) return null
        const ref: McpServerReference = {
          id: cfg.mcpServerId as string,
          name: (cfg.mcpServerName as string) || '',
        }
        if (cfg.mcpServerDescription) ref.description = cfg.mcpServerDescription as string
        return ref
      })
      .filter((r): r is McpServerReference => r !== null)
    if (mcpRefs.length > 0) contract.mcpServers = mcpRefs

    // Sub-Agents — each node references another agent, include id + name + description
    const subAgentNodes = nodes.filter((n) => n.data?.nodeType === 'agentSubAgent')
    const subAgentRefs: SubAgentReference[] = subAgentNodes
      .map((n) => {
        const cfg = nodeConfigurations[n.id]
        if (!cfg?.subAgentId) return null
        const ref: SubAgentReference = {
          id: cfg.subAgentId as string,
          name: (cfg.subAgentName as string) || '',
        }
        if (cfg.subAgentDescription) ref.description = cfg.subAgentDescription as string
        return ref
      })
      .filter((r): r is SubAgentReference => r !== null)
    if (subAgentRefs.length > 0) contract.subAgents = subAgentRefs

    // Tool Groups — each node may contain multiple tool IDs
    const toolNodes = nodes.filter((n) => n.data?.nodeType === 'agentToolGroup')
    const allToolIds = toolNodes.flatMap(
      (n) => (nodeConfigurations[n.id]?.toolIds as string[]) || []
    )

    // Auto-include swarm tools when sub-agents are connected
    if (subAgentRefs.length > 0) {
      const swarmTools = ['swarm_delegate', 'swarm_management']
      for (const tool of swarmTools) {
        if (!allToolIds.includes(tool)) allToolIds.push(tool)
      }
    }

    if (allToolIds.length > 0) contract.toolGroups = allToolIds

    // Sandbox — present on canvas means enabled
    const sandboxNode = nodes.find((n) => n.data?.nodeType === 'agentSandbox')
    if (sandboxNode) {
      contract.enableSandbox = true
    }

    return contract
  },

  exportToJson: () => {
    const state = get()
    return JSON.stringify(
      {
        agent: {
          id: state.agentId,
          name: state.agentName,
          description: state.agentDescription,
        },
        contract: state.serializeToContract(),
        reactFlowData: {
          nodes: state.nodes,
          edges: state.edges,
          viewport: state.viewport,
        },
        nodeConfigurations: state.nodeConfigurations,
      },
      null,
      2
    )
  },

  save: async () => {
    const state = get()
    if (!state.agentId) throw new Error('No agent ID — create agent first')

    const { agentDefinitions } = await import('@donkeywork/api-client')
    await agentDefinitions.update(state.agentId, {
      name: state.agentName,
      description: state.agentDescription,
      connectToNavi: state.connectToNavi,
      contract: state.serializeToContract(),
      reactFlowData: {
        nodes: state.nodes,
        edges: state.edges,
        viewport: state.viewport,
      },
      nodeConfigurations: state.nodeConfigurations,
    })
  },

  getModelNodeId: () => {
    const { nodes } = get()
    return nodes.find((n) => n.data?.nodeType === 'agentModel')?.id ?? null
  },

  hasNodeOfType: (type) => {
    const { nodes } = get()
    return nodes.some((n) => n.data?.nodeType === type)
  },
}))
