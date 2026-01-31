import { create } from 'zustand'
import type { Node, Edge, Viewport } from '@xyflow/react'

// Types
export interface JSONSchema {
  type: string
  properties?: Record<string, unknown>
  required?: string[]
  [key: string]: unknown
}

export interface StartNodeConfig {
  name: string
  inputSchema: JSONSchema
}

export interface ModelNodeConfig {
  name: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  modelId: string
  credentialId?: string
  systemPrompt?: string
  userMessage?: string
  temperature?: number
  maxTokens?: number
  topP?: number
}

export interface EndNodeConfig {
  name: string
  outputSchema?: JSONSchema | null
}

export interface ActionNodeConfig {
  name: string
  actionType: string
  displayName: string
  parameters?: Record<string, any>
}

export interface MessageFormatterNodeConfig {
  name: string
  template: string
}

export type NodeConfig = StartNodeConfig | ModelNodeConfig | EndNodeConfig | ActionNodeConfig | MessageFormatterNodeConfig

export interface ValidationError {
  nodeId?: string
  field: string
  message: string
}

export interface ValidationResult {
  isValid: boolean
  errors: ValidationError[]
}

interface EditorState {
  // Agent metadata
  agentId: string | null
  agentName: string
  agentDescription: string

  // Version data
  versionId: string | null
  isDraft: boolean

  // ReactFlow state
  nodes: Node[]
  edges: Edge[]
  viewport: Viewport

  // Node configurations (source of truth)
  nodeConfigurations: Record<string, NodeConfig>

  // UI state
  selectedNodeId: string | null
  isPaletteOpen: boolean
  isPropertiesOpen: boolean

  // Actions
  setAgentMetadata: (name: string, description: string) => void
  setNodes: (nodes: Node[]) => void
  setEdges: (edges: Edge[]) => void
  onNodesChange: (changes: any[]) => void
  onEdgesChange: (changes: any[]) => void
  onConnect: (connection: any) => void
  addNode: (type: string, position: { x: number; y: number }, config?: Partial<NodeConfig>) => void
  removeNode: (nodeId: string) => void
  updateNodeConfig: (nodeId: string, config: Partial<NodeConfig>) => void
  updateNodeData: (nodeId: string, data: any) => void
  selectNode: (nodeId: string | null) => void
  togglePalette: () => void
  toggleProperties: () => void
  setViewport: (viewport: Viewport) => void
  reset: () => void
  loadAgent: (
    agentId: string,
    agentName: string,
    agentDescription: string,
    versionId?: string,
    isDraft?: boolean,
    reactFlowData?: { nodes: Node[], edges: Edge[], viewport: Viewport },
    nodeConfigurations?: Record<string, NodeConfig>
  ) => void

  // Persistence
  extractCredentialMappings: () => Array<{ nodeId: string; credentialId: string }>
  save: () => Promise<void>
  load: (agentId: string, versionId: string) => Promise<void>
  exportToJson: () => string

  // Validation
  validate: () => ValidationResult

  // Helpers
  generateNodeName: (type: string) => string

  // Layout
  tidyUpNodes: () => void

  // Graph helpers
  getReachablePredecessors: (nodeId: string) => Array<{ nodeId: string; nodeName: string; nodeType: string }>
}

// Default input schema
const defaultInputSchema: JSONSchema = {
  type: 'object',
  properties: {
    input: { type: 'string' }
  },
  required: ['input']
}

// Generate GUID
function generateGuid(): string {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    const v = c === 'x' ? r : (r & 0x3) | 0x8
    return v.toString(16)
  })
}

// Create initial state
const createInitialState = () => {
  const startId = generateGuid()
  const endId = generateGuid()

  return {
    // Agent metadata
    agentId: null,
    agentName: 'Untitled Agent',
    agentDescription: '',

    // Version data
    versionId: null,
    isDraft: true,

    // ReactFlow state
    nodes: [
      {
        id: startId,
        type: 'start',
        position: { x: 250, y: 50 },
        data: { label: 'start' }
      },
      {
        id: endId,
        type: 'end',
        position: { x: 250, y: 250 },
        data: { label: 'end' }
      }
    ] as Node[],
    edges: [
      {
        id: generateGuid(),
        source: startId,
        target: endId,
        type: 'smoothstep',
        animated: true
      }
    ] as Edge[],
    viewport: { x: 0, y: 0, zoom: 1 },

    // Node configurations
    nodeConfigurations: {
      [startId]: {
        name: 'start',
        inputSchema: defaultInputSchema
      } as StartNodeConfig,
      [endId]: {
        name: 'end',
        outputSchema: null
      } as EndNodeConfig
    },

    // UI state
    selectedNodeId: null,
    isPaletteOpen: true,
    isPropertiesOpen: false
  }
}

export const useEditorStore = create<EditorState>((set, get) => ({
  ...createInitialState(),

  setAgentMetadata: (name, description) => {
    set({ agentName: name, agentDescription: description })
  },

  setNodes: (nodes) => {
    set({ nodes })
  },

  setEdges: (edges) => {
    set({ edges })
  },

  onNodesChange: (changes) => {
    const { nodes } = get()
    // Apply changes to nodes (ReactFlow will handle this)
    set({ nodes: nodes.map(node => {
      const change = changes.find((c: any) => c.id === node.id)
      if (!change) return node

      if (change.type === 'position' && change.position) {
        return { ...node, position: change.position }
      }
      if (change.type === 'select') {
        return { ...node, selected: change.selected }
      }
      if (change.type === 'remove') {
        return null
      }
      return node
    }).filter(Boolean) as Node[] })
  },

  onEdgesChange: (changes) => {
    const { edges } = get()
    set({ edges: edges.map(edge => {
      const change = changes.find((c: any) => c.id === edge.id)
      if (!change) return edge

      if (change.type === 'select') {
        return { ...edge, selected: change.selected }
      }
      if (change.type === 'remove') {
        return null
      }
      return edge
    }).filter(Boolean) as Edge[] })
  },

  onConnect: (connection) => {
    const { edges } = get()
    const newEdge = {
      id: generateGuid(),
      source: connection.source,
      target: connection.target,
      type: 'smoothstep',
      animated: true
    }
    set({ edges: [...edges, newEdge] })
  },

  addNode: (type, position, config = {}) => {
    const { nodes, nodeConfigurations, generateNodeName } = get()
    const nodeId = generateGuid()
    const nodeName = generateNodeName(type)

    // Create default config based on type
    let defaultConfig: NodeConfig
    let nodeData: any = { label: nodeName }

    if (type === 'start') {
      defaultConfig = {
        name: nodeName,
        inputSchema: defaultInputSchema,
        ...config
      } as StartNodeConfig
    } else if (type === 'end') {
      defaultConfig = {
        name: nodeName,
        outputSchema: null,
        ...config
      } as EndNodeConfig
    } else if (type === 'model') {
      defaultConfig = {
        name: nodeName,
        provider: (config as any).provider || 'OpenAi',
        modelId: (config as any).modelId || '',
        credentialId: undefined,
        systemPrompt: undefined,
        userMessage: undefined,
        temperature: undefined,
        maxTokens: undefined,
        topP: undefined,
        ...config
      } as ModelNodeConfig
      nodeData = {
        label: nodeName,
        provider: (config as any).provider,
        modelName: (config as any).modelName
      }
    } else if (type === 'action') {
      // Use displayName as the base for the node name (e.g., "http_request", "sleep")
      const actionDisplayName = (config as any).displayName || ''
      const actionBaseName = actionDisplayName
        .toLowerCase()
        .replace(/\s+/g, '_')
        .replace(/[^a-z0-9_]/g, '') || 'action'
      const actionNodeName = generateNodeName(actionBaseName)

      defaultConfig = {
        name: actionNodeName,
        actionType: (config as any).actionType || '',
        displayName: actionDisplayName,
        parameters: {},
        ...config
      } as ActionNodeConfig
      nodeData = {
        label: actionNodeName,
        actionType: (config as any).actionType,
        displayName: actionDisplayName,
        icon: (config as any).icon,
        parameters: {}
      }
    } else if (type === 'messageFormatter') {
      defaultConfig = {
        name: nodeName,
        template: '',
        ...config
      } as MessageFormatterNodeConfig
      nodeData = {
        label: nodeName
      }
    } else {
      return
    }

    const newNode: Node = {
      id: nodeId,
      type,
      position,
      data: nodeData
    }

    set({
      nodes: [...nodes, newNode],
      nodeConfigurations: {
        ...nodeConfigurations,
        [nodeId]: defaultConfig
      }
    })
  },

  removeNode: (nodeId) => {
    const { nodes, edges, nodeConfigurations } = get()

    // Remove node
    const newNodes = nodes.filter(n => n.id !== nodeId)

    // Remove connected edges
    const newEdges = edges.filter(e => e.source !== nodeId && e.target !== nodeId)

    // Remove configuration
    const newConfigs = { ...nodeConfigurations }
    delete newConfigs[nodeId]

    set({
      nodes: newNodes,
      edges: newEdges,
      nodeConfigurations: newConfigs,
      selectedNodeId: null
    })
  },

  updateNodeConfig: (nodeId, config) => {
    const { nodeConfigurations, nodes } = get()

    // Update node configuration
    const updatedConfig = {
      ...nodeConfigurations[nodeId],
      ...config
    }

    // If name changed, also update the node's data.label for canvas display
    const updatedNodes = config.name !== undefined
      ? nodes.map(node =>
          node.id === nodeId
            ? { ...node, data: { ...node.data, label: config.name } }
            : node
        )
      : nodes

    set({
      nodeConfigurations: {
        ...nodeConfigurations,
        [nodeId]: updatedConfig
      },
      nodes: updatedNodes
    })
  },

  updateNodeData: (nodeId, data) => {
    const { nodes, nodeConfigurations } = get()

    // Find the node to determine its type
    const node = nodes.find(n => n.id === nodeId)

    const updatedNodes = nodes.map(n =>
      n.id === nodeId
        ? { ...n, data }
        : n
    )

    // For action nodes, also update the nodeConfiguration with parameters
    let updatedConfigurations = nodeConfigurations
    if (node?.type === 'action' && data.parameters) {
      const config = nodeConfigurations[nodeId]
      if (config && 'parameters' in config) {
        updatedConfigurations = {
          ...nodeConfigurations,
          [nodeId]: {
            ...config,
            parameters: data.parameters
          }
        }
      }
    }

    set({
      nodes: updatedNodes,
      nodeConfigurations: updatedConfigurations
    })
  },

  selectNode: (nodeId) => {
    set({
      selectedNodeId: nodeId,
      isPropertiesOpen: nodeId !== null
    })
  },

  togglePalette: () => {
    set((state) => ({ isPaletteOpen: !state.isPaletteOpen }))
  },

  toggleProperties: () => {
    set((state) => ({ isPropertiesOpen: !state.isPropertiesOpen }))
  },

  setViewport: (viewport) => {
    set({ viewport })
  },

  reset: () => {
    set(createInitialState())
  },

  loadAgent: (agentId, agentName, agentDescription, versionId, isDraft, reactFlowData, nodeConfigurations) => {
    if (reactFlowData && nodeConfigurations) {
      // Sync data from nodeConfigurations to nodes
      const syncedNodes = reactFlowData.nodes.map(node => {
        const config = nodeConfigurations[node.id]

        // For action nodes, preserve all data fields
        if (node.type === 'action' && config && 'actionType' in config) {
          return {
            ...node,
            data: {
              ...node.data,
              actionType: config.actionType,
              displayName: config.displayName,
              parameters: config.parameters
            }
          }
        }

        // For other nodes, just sync the label
        return {
          ...node,
          data: {
            ...node.data,
            label: config?.name || node.data.label
          }
        }
      })

      // Load existing version data
      set({
        agentId,
        agentName,
        agentDescription,
        versionId: versionId || null,
        isDraft: isDraft ?? true,
        nodes: syncedNodes,
        edges: reactFlowData.edges,
        viewport: reactFlowData.viewport,
        nodeConfigurations,
        selectedNodeId: null,
        isPaletteOpen: true,
        isPropertiesOpen: false
      })
    } else {
      // Reset to default state with agent metadata
      const initial = createInitialState()
      set({
        ...initial,
        agentId,
        agentName,
        agentDescription
      })
    }
  },

  validate: () => {
    const { nodes, edges, nodeConfigurations } = get()
    const errors: ValidationError[] = []

    // Check for exactly one Start node
    const startNodes = nodes.filter(n => n.type === 'start')
    if (startNodes.length === 0) {
      errors.push({ field: 'nodes', message: 'Missing Start node' })
    } else if (startNodes.length > 1) {
      errors.push({ field: 'nodes', message: 'Multiple Start nodes found. Only one allowed.' })
    }

    // Check for exactly one End node
    const endNodes = nodes.filter(n => n.type === 'end')
    if (endNodes.length === 0) {
      errors.push({ field: 'nodes', message: 'Missing End node' })
    } else if (endNodes.length > 1) {
      errors.push({ field: 'nodes', message: 'Multiple End nodes found. Only one allowed.' })
    }

    // Check all nodes have configurations
    nodes.forEach(node => {
      if (!nodeConfigurations[node.id]) {
        errors.push({ nodeId: node.id, field: 'config', message: `Missing configuration for node ${node.id}` })
      } else {
        const config = nodeConfigurations[node.id]

        // Check node name
        if (!config.name || config.name.trim() === '') {
          errors.push({ nodeId: node.id, field: 'name', message: 'Node name is required' })
        }

        // Validate based on type
        if (node.type === 'start') {
          const startConfig = config as StartNodeConfig
          if (!startConfig.inputSchema) {
            errors.push({ nodeId: node.id, field: 'inputSchema', message: 'Input schema is required' })
          }
        } else if (node.type === 'model') {
          const modelConfig = config as ModelNodeConfig
          if (!modelConfig.provider) {
            errors.push({ nodeId: node.id, field: 'provider', message: 'Provider is required' })
          }
          if (!modelConfig.modelId) {
            errors.push({ nodeId: node.id, field: 'modelId', message: 'Model is required' })
          }
          if (!modelConfig.credentialId) {
            errors.push({ nodeId: node.id, field: 'credentialId', message: 'Credential is required' })
          }
        } else if (node.type === 'action') {
          const actionConfig = config as ActionNodeConfig
          if (!actionConfig.actionType) {
            errors.push({ nodeId: node.id, field: 'actionType', message: 'Action type is required' })
          }
          // Note: Specific parameter validation would require loading the action schema
          // and checking required fields. This could be added in the future.
        }
      }
    })

    // Check for unique names
    const names = Object.values(nodeConfigurations).map(c => c.name)
    const duplicates = names.filter((name, index) => names.indexOf(name) !== index)
    if (duplicates.length > 0) {
      errors.push({ field: 'names', message: `Duplicate node names found: ${duplicates.join(', ')}` })
    }

    // Check all edges reference existing nodes
    edges.forEach(edge => {
      if (!nodes.find(n => n.id === edge.source)) {
        errors.push({ field: 'edges', message: `Edge references non-existent source node: ${edge.source}` })
      }
      if (!nodes.find(n => n.id === edge.target)) {
        errors.push({ field: 'edges', message: `Edge references non-existent target node: ${edge.target}` })
      }
    })

    return {
      isValid: errors.length === 0,
      errors
    }
  },

  generateNodeName: (type) => {
    const { nodeConfigurations } = get()
    const existingNames = Object.values(nodeConfigurations).map(c => c.name)

    // Convert camelCase to snake_case and ensure lowercase
    const baseName = type
      .replace(/([a-z])([A-Z])/g, '$1_$2')
      .toLowerCase()

    // First try the base name without a counter
    if (!existingNames.includes(baseName)) {
      return baseName
    }

    // If base name exists, start with counter = 2
    let counter = 2
    let name = `${baseName}_${counter}`

    while (existingNames.includes(name)) {
      counter++
      name = `${baseName}_${counter}`
    }

    return name
  },

  tidyUpNodes: () => {
    const { nodes, edges } = get()
    if (nodes.length === 0) return

    // Build adjacency list for topological sort
    const inDegree: Record<string, number> = {}
    const children: Record<string, string[]> = {}

    // Initialize
    nodes.forEach(node => {
      inDegree[node.id] = 0
      children[node.id] = []
    })

    // Build graph
    edges.forEach(edge => {
      if (inDegree[edge.target] !== undefined) {
        inDegree[edge.target]++
      }
      if (children[edge.source]) {
        children[edge.source].push(edge.target)
      }
    })

    // Topological sort using Kahn's algorithm to get levels
    const levels: string[][] = []
    let currentLevel = nodes.filter(n => inDegree[n.id] === 0).map(n => n.id)

    while (currentLevel.length > 0) {
      levels.push(currentLevel)
      const nextLevel: string[] = []

      currentLevel.forEach(nodeId => {
        children[nodeId]?.forEach(childId => {
          inDegree[childId]--
          if (inDegree[childId] === 0) {
            nextLevel.push(childId)
          }
        })
      })

      currentLevel = nextLevel
    }

    // Handle any remaining nodes (disconnected or in cycles)
    const placedNodes = new Set(levels.flat())
    const remainingNodes = nodes.filter(n => !placedNodes.has(n.id)).map(n => n.id)
    if (remainingNodes.length > 0) {
      levels.push(remainingNodes)
    }

    // Calculate positions
    const NODE_WIDTH = 200
    const VERTICAL_SPACING = 120
    const HORIZONTAL_SPACING = 50
    const START_Y = 50

    // Find the widest level to center everything
    const maxNodesInLevel = Math.max(...levels.map(level => level.length))
    const totalWidth = maxNodesInLevel * NODE_WIDTH + (maxNodesInLevel - 1) * HORIZONTAL_SPACING
    const centerX = totalWidth / 2

    const updatedNodes = nodes.map(node => {
      // Find which level this node is in
      const levelIndex = levels.findIndex(level => level.includes(node.id))
      if (levelIndex === -1) return node

      const level = levels[levelIndex]
      const indexInLevel = level.indexOf(node.id)

      // Calculate position
      const levelWidth = level.length * NODE_WIDTH + (level.length - 1) * HORIZONTAL_SPACING
      const levelStartX = centerX - levelWidth / 2

      const x = levelStartX + indexInLevel * (NODE_WIDTH + HORIZONTAL_SPACING)
      const y = START_Y + levelIndex * VERTICAL_SPACING

      return {
        ...node,
        position: { x, y }
      }
    })

    set({ nodes: updatedNodes })
  },

  getReachablePredecessors: (nodeId: string) => {
    const { nodes, edges, nodeConfigurations } = get()

    // Build reverse adjacency list (target -> sources)
    const predecessors: Record<string, string[]> = {}
    nodes.forEach(node => {
      predecessors[node.id] = []
    })
    edges.forEach(edge => {
      if (predecessors[edge.target]) {
        predecessors[edge.target].push(edge.source)
      }
    })

    // BFS to find all reachable predecessors
    const visited = new Set<string>()
    const queue = [...(predecessors[nodeId] || [])]
    const result: Array<{ nodeId: string; nodeName: string; nodeType: string }> = []

    while (queue.length > 0) {
      const currentId = queue.shift()!
      if (visited.has(currentId)) continue
      visited.add(currentId)

      const node = nodes.find(n => n.id === currentId)
      const config = nodeConfigurations[currentId]

      if (node && config) {
        result.push({
          nodeId: currentId,
          nodeName: config.name,
          nodeType: node.type || 'unknown'
        })
      }

      // Add predecessors of current node to queue
      const preds = predecessors[currentId] || []
      queue.push(...preds.filter(p => !visited.has(p)))
    }

    return result
  },

  extractCredentialMappings: () => {
    const { nodes, nodeConfigurations } = get()
    const mappings: Array<{ nodeId: string; credentialId: string }> = []

    for (const node of nodes) {
      const config = nodeConfigurations[node.id]
      if (config && 'credentialId' in config && config.credentialId) {
        mappings.push({
          nodeId: node.id,
          credentialId: config.credentialId
        })
      }
    }

    return mappings
  },

  save: async () => {
    const { agentId, nodes, edges, viewport, nodeConfigurations } = get()

    if (!agentId) {
      throw new Error('No agent ID - create agent first')
    }

    // Find start node and get input schema
    const startNode = nodes.find(n => n.type === 'start')
    const inputSchema = startNode
      ? (nodeConfigurations[startNode.id] as StartNodeConfig)?.inputSchema || defaultInputSchema
      : defaultInputSchema

    const reactFlowData = { nodes, edges, viewport }
    const credentialMappings = get().extractCredentialMappings()

    // Import agents API
    const { agents } = await import('@/lib/api')

    await agents.saveVersion(agentId, {
      reactFlowData,
      nodeConfigurations,
      inputSchema,
      outputSchema: null,
      credentialMappings
    })
  },

  load: async (agentId: string, versionId: string) => {
    // Import agents API
    const { agents } = await import('@/lib/api')

    const version = await agents.getVersion(agentId, versionId)

    set({
      agentId,
      versionId,
      nodes: version.reactFlowData.nodes,
      edges: version.reactFlowData.edges,
      viewport: version.reactFlowData.viewport,
      nodeConfigurations: version.nodeConfigurations,
      isDraft: version.isDraft
    })
  },

  exportToJson: () => {
    const { agentId, agentName, agentDescription, nodes, edges, viewport, nodeConfigurations } = get()

    const startNode = nodes.find(n => n.type === 'start')
    const inputSchema = startNode
      ? (nodeConfigurations[startNode.id] as StartNodeConfig)?.inputSchema || defaultInputSchema
      : defaultInputSchema

    return JSON.stringify({
      agent: {
        id: agentId,
        name: agentName,
        description: agentDescription
      },
      version: {
        reactFlowData: { nodes, edges, viewport },
        nodeConfigurations,
        inputSchema,
        credentialMappings: get().extractCredentialMappings()
      }
    }, null, 2)
  }
}))
