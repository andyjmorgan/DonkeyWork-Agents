import { create } from 'zustand'
import type { Node, Edge, Viewport, NodeChange, EdgeChange, Connection } from '@xyflow/react'
import type { InterfaceConfig } from '@/lib/api'

/**
 * Schema lookup for node display properties.
 * Used to enrich loaded nodes that may be missing display data.
 */
const nodeSchemaLookup: Record<string, { displayName: string; icon: string; color: string; hasInputHandle: boolean; hasOutputHandle: boolean; canDelete: boolean }> = {
  Start: { displayName: 'Start', icon: 'play', color: 'green', hasInputHandle: false, hasOutputHandle: true, canDelete: false },
  End: { displayName: 'End', icon: 'flag', color: 'orange', hasInputHandle: true, hasOutputHandle: false, canDelete: false },
  Model: { displayName: 'Model', icon: 'brain', color: 'blue', hasInputHandle: true, hasOutputHandle: true, canDelete: true },
  MultimodalChatModel: { displayName: 'Multimodal Chat', icon: 'brain', color: 'blue', hasInputHandle: true, hasOutputHandle: true, canDelete: true },
  HttpRequest: { displayName: 'HTTP Request', icon: 'globe', color: 'purple', hasInputHandle: true, hasOutputHandle: true, canDelete: true },
  Sleep: { displayName: 'Sleep', icon: 'clock', color: 'cyan', hasInputHandle: true, hasOutputHandle: true, canDelete: true },
  MessageFormatter: { displayName: 'Message Formatter', icon: 'file-text', color: 'cyan', hasInputHandle: true, hasOutputHandle: true, canDelete: true },
}

/**
 * Enriches a node with schema display data if missing.
 */
function enrichNodeWithSchema(node: Node, config?: NodeConfig): Node {
  const nodeType = (node.data?.nodeType as string) || config?.type
  if (!nodeType) return node

  const schema = nodeSchemaLookup[nodeType]
  if (!schema) return node

  return {
    ...node,
    type: 'schemaNode', // Ensure correct type
    data: {
      ...node.data,
      nodeType,
      label: config?.name || node.data?.label || nodeType.toLowerCase(),
      displayName: node.data?.displayName || schema.displayName,
      icon: node.data?.icon || schema.icon,
      color: node.data?.color || schema.color,
      hasInputHandle: node.data?.hasInputHandle ?? schema.hasInputHandle,
      hasOutputHandle: node.data?.hasOutputHandle ?? schema.hasOutputHandle,
      canDelete: node.data?.canDelete ?? schema.canDelete,
    }
  }
}

/**
 * Schema-driven node configuration.
 * All nodes use the same structure - the 'type' field is the polymorphic discriminator
 * that the backend uses to deserialize to the correct NodeConfiguration class.
 */
export interface NodeConfig {
  type: string                          // Backend type discriminator (e.g., "Start", "Model", "HttpRequest")
  name: string                          // Instance name (e.g., "start", "http_request_1")
  [key: string]: unknown                // All other fields from schema
}

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
  orchestrationId: string | null
  orchestrationName: string
  orchestrationDescription: string

  // Version data
  versionId: string | null
  isDraft: boolean
  interface: InterfaceConfig

  // ReactFlow state
  nodes: Node[]
  edges: Edge[]
  viewport: Viewport

  // Node configurations (source of truth for backend)
  nodeConfigurations: Record<string, NodeConfig>

  // UI state
  selectedNodeId: string | null
  isPaletteOpen: boolean
  isPropertiesOpen: boolean

  // Actions
  setOrchestrationMetadata: (name: string, description: string) => void
  setNodes: (nodes: Node[]) => void
  setEdges: (edges: Edge[]) => void
  onNodesChange: (changes: NodeChange[]) => void
  onEdgesChange: (changes: EdgeChange[]) => void
  onConnect: (connection: Connection) => void
  addNode: (position: { x: number; y: number }, schemaInfo: Record<string, unknown>) => void
  removeNode: (nodeId: string) => void
  updateNodeConfig: (nodeId: string, config: Partial<NodeConfig>) => void
  updateNodeData: (nodeId: string, data: Record<string, unknown>) => void
  selectNode: (nodeId: string | null) => void
  togglePalette: () => void
  toggleProperties: () => void
  setViewport: (viewport: Viewport) => void
  reset: () => void
  loadOrchestration: (
    orchestrationId: string,
    orchestrationName: string,
    orchestrationDescription: string,
    versionId?: string,
    isDraft?: boolean,
    reactFlowData?: { nodes: Node[], edges: Edge[], viewport: Viewport },
    nodeConfigurations?: Record<string, NodeConfig>,
    interfaceConfig?: InterfaceConfig
  ) => void

  // Persistence
  extractCredentialMappings: () => Array<{ nodeId: string; credentialId: string }>
  save: () => Promise<void>
  load: (orchestrationId: string, versionId: string) => Promise<void>
  exportToJson: () => string

  // Validation
  validate: () => ValidationResult

  // Helpers
  generateNodeName: (type: string) => string

  // Layout
  tidyUpNodes: () => void

  // Graph helpers
  getReachablePredecessors: (nodeId: string) => Array<{ nodeId: string; nodeName: string; nodeType: string }>

  // Interface
  setInterface: (interfaceConfig: InterfaceConfig) => void
}

// Default input schema for Start node
const defaultInputSchema = {
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

// Create initial state with Start and End nodes
const createInitialState = () => {
  const startId = generateGuid()
  const endId = generateGuid()

  return {
    orchestrationId: null,
    orchestrationName: 'Untitled Orchestration',
    orchestrationDescription: '',
    versionId: null,
    isDraft: true,
    interface: { type: 'ChatInterfaceConfig' } as InterfaceConfig,
    nodes: [
      {
        id: startId,
        type: 'schemaNode',
        position: { x: 250, y: 50 },
        data: {
          label: 'start',
          nodeType: 'Start',
          displayName: 'Start',
          icon: 'play',
          color: 'green',
          hasInputHandle: false,
          hasOutputHandle: true,
          canDelete: false
        }
      },
      {
        id: endId,
        type: 'schemaNode',
        position: { x: 250, y: 250 },
        data: {
          label: 'end',
          nodeType: 'End',
          displayName: 'End',
          icon: 'flag',
          color: 'orange',
          hasInputHandle: true,
          hasOutputHandle: false,
          canDelete: false
        }
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
    nodeConfigurations: {
      [startId]: {
        type: 'Start',
        name: 'start',
        inputSchema: defaultInputSchema
      },
      [endId]: {
        type: 'End',
        name: 'end'
      }
    } as Record<string, NodeConfig>,
    selectedNodeId: null,
    isPaletteOpen: true,
    isPropertiesOpen: false
  }
}

export const useEditorStore = create<EditorState>((set, get) => ({
  ...createInitialState(),

  setOrchestrationMetadata: (name, description) => {
    set({ orchestrationName: name, orchestrationDescription: description })
  },

  setNodes: (nodes) => {
    set({ nodes })
  },

  setEdges: (edges) => {
    set({ edges })
  },

  onNodesChange: (changes) => {
    const { nodes } = get()
    set({ nodes: nodes.map(node => {
      const change = changes.find((c) => 'id' in c && c.id === node.id)
      if (!change) return node

      if (change.type === 'position' && 'position' in change && change.position) {
        return { ...node, position: change.position }
      }
      if (change.type === 'select' && 'selected' in change) {
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
      const change = changes.find((c) => 'id' in c && c.id === edge.id)
      if (!change) return edge

      if (change.type === 'select' && 'selected' in change) {
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

  addNode: (position, schemaInfo) => {
    const { nodes, nodeConfigurations, generateNodeName } = get()
    const nodeId = generateGuid()

    // Get node type from schema info
    const nodeType = schemaInfo.nodeType as string
    const displayName = schemaInfo.displayName as string || nodeType

    // Generate instance name based on display name (allows A-Za-z0-9_-)
    const baseName = displayName.replace(/\s+/g, '_').replace(/[^A-Za-z0-9_-]/g, '')
    const nodeName = generateNodeName(baseName)

    // Create ReactFlow node with all display data from schema
    const newNode: Node = {
      id: nodeId,
      type: 'schemaNode',
      position,
      data: {
        label: nodeName,
        nodeType,
        displayName,
        icon: schemaInfo.icon,
        color: schemaInfo.color,
        hasInputHandle: schemaInfo.hasInputHandle,
        hasOutputHandle: schemaInfo.hasOutputHandle,
        canDelete: schemaInfo.canDelete
      }
    }

    // Create config with type discriminator for backend serialization
    const newConfig: NodeConfig = {
      type: nodeType,
      name: nodeName
    }

    // Add default values based on node type
    if (nodeType === 'Start') {
      newConfig.inputSchema = defaultInputSchema
    } else if (nodeType === 'Model') {
      // Copy model-specific data from schema info
      newConfig.provider = schemaInfo.provider
      newConfig.modelId = schemaInfo.modelId
    } else if (nodeType === 'MultimodalChatModel') {
      // Copy model-specific data from schema info - provider and modelId are immutable
      newConfig.provider = schemaInfo.provider
      newConfig.modelId = schemaInfo.modelId
      // Initialize required fields
      newConfig.userMessages = []
      // Initialize providerConfig with type discriminator for polymorphic deserialization
      newConfig.providerConfig = { type: schemaInfo.provider }
    }

    set({
      nodes: [...nodes, newNode],
      nodeConfigurations: {
        ...nodeConfigurations,
        [nodeId]: newConfig
      }
    })
  },

  removeNode: (nodeId) => {
    const { nodes, edges, nodeConfigurations } = get()

    const newNodes = nodes.filter(n => n.id !== nodeId)
    const newEdges = edges.filter(e => e.source !== nodeId && e.target !== nodeId)
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
    const { nodes } = get()

    const updatedNodes = nodes.map(n =>
      n.id === nodeId
        ? { ...n, data }
        : n
    )

    set({ nodes: updatedNodes })
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

  loadOrchestration: (orchestrationId, orchestrationName, orchestrationDescription, versionId, isDraft, reactFlowData, nodeConfigurations, interfaceConfig) => {
    if (reactFlowData && nodeConfigurations) {
      // Enrich nodes with schema data (for backward compatibility with old saved nodes)
      const enrichedNodes = reactFlowData.nodes.map(node => {
        const config = nodeConfigurations[node.id]
        return enrichNodeWithSchema(node, config)
      })

      set({
        orchestrationId,
        orchestrationName,
        orchestrationDescription,
        versionId: versionId || null,
        isDraft: isDraft ?? true,
        interface: interfaceConfig ?? { type: 'ChatInterfaceConfig' } as InterfaceConfig,
        nodes: enrichedNodes,
        edges: reactFlowData.edges,
        viewport: reactFlowData.viewport,
        nodeConfigurations,
        selectedNodeId: null,
        isPaletteOpen: true,
        isPropertiesOpen: false
      })
    } else {
      const initial = createInitialState()
      set({
        ...initial,
        orchestrationId,
        orchestrationName,
        orchestrationDescription
      })
    }
  },

  validate: () => {
    const { nodes, edges, nodeConfigurations } = get()
    const errors: ValidationError[] = []

    // Check for exactly one Start node
    const startNodes = nodes.filter(n => n.data?.nodeType === 'Start')
    if (startNodes.length === 0) {
      errors.push({ field: 'nodes', message: 'Missing Start node' })
    } else if (startNodes.length > 1) {
      errors.push({ field: 'nodes', message: 'Multiple Start nodes found. Only one allowed.' })
    }

    // Check for exactly one End node
    const endNodes = nodes.filter(n => n.data?.nodeType === 'End')
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

        // Check type discriminator
        if (!config.type) {
          errors.push({ nodeId: node.id, field: 'type', message: 'Node type is required' })
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

    // Use the type as base name (preserves case), insert underscore before capitals
    const baseName = type.replace(/([a-z])([A-Z])/g, '$1_$2')

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

    nodes.forEach(node => {
      inDegree[node.id] = 0
      children[node.id] = []
    })

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

    const maxNodesInLevel = Math.max(...levels.map(level => level.length))
    const totalWidth = maxNodesInLevel * NODE_WIDTH + (maxNodesInLevel - 1) * HORIZONTAL_SPACING
    const centerX = totalWidth / 2

    const updatedNodes = nodes.map(node => {
      const levelIndex = levels.findIndex(level => level.includes(node.id))
      if (levelIndex === -1) return node

      const level = levels[levelIndex]
      const indexInLevel = level.indexOf(node.id)

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
          nodeType: (node.data?.nodeType as string) || 'unknown'
        })
      }

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
          credentialId: config.credentialId as string
        })
      }
    }

    return mappings
  },

  save: async () => {
    const state = get()
    const { orchestrationId, nodes, edges, viewport, nodeConfigurations } = state
    const interfaceConfig = state.interface

    if (!orchestrationId) {
      throw new Error('No orchestration ID - create orchestration first')
    }

    // Find start node and get input schema
    const startNode = nodes.find(n => n.data?.nodeType === 'Start')
    const startConfig = startNode ? nodeConfigurations[startNode.id] : null
    const inputSchema = (startConfig?.inputSchema as { type: string; properties?: Record<string, unknown>; required?: string[] }) || defaultInputSchema

    const reactFlowData = { nodes, edges, viewport }
    const credentialMappings = get().extractCredentialMappings()

    const { orchestrations } = await import('@/lib/api')

    await orchestrations.saveVersion(orchestrationId, {
      reactFlowData,
      nodeConfigurations,
      inputSchema,
      outputSchema: null,
      credentialMappings,
      interface: interfaceConfig
    })
  },

  load: async (orchestrationId: string, versionId: string) => {
    const { orchestrations } = await import('@/lib/api')

    const version = await orchestrations.getVersion(orchestrationId, versionId)

    // Enrich nodes with schema data (for backward compatibility with old saved nodes)
    const enrichedNodes = version.reactFlowData.nodes.map((node: Node) => {
      const config = version.nodeConfigurations[node.id]
      return enrichNodeWithSchema(node, config)
    })

    set({
      orchestrationId,
      versionId,
      nodes: enrichedNodes,
      edges: version.reactFlowData.edges,
      viewport: version.reactFlowData.viewport,
      nodeConfigurations: version.nodeConfigurations,
      isDraft: version.isDraft
    })
  },

  exportToJson: () => {
    const state = get()
    const { orchestrationId, orchestrationName, orchestrationDescription, nodes, edges, viewport, nodeConfigurations } = state
    const interfaceConfig = state.interface

    const startNode = nodes.find(n => n.data?.nodeType === 'Start')
    const startConfig = startNode ? nodeConfigurations[startNode.id] : null
    const inputSchema = (startConfig?.inputSchema as Record<string, unknown>) || defaultInputSchema

    return JSON.stringify({
      orchestration: {
        id: orchestrationId,
        name: orchestrationName,
        description: orchestrationDescription
      },
      version: {
        reactFlowData: { nodes, edges, viewport },
        nodeConfigurations,
        inputSchema,
        interface: interfaceConfig,
        credentialMappings: get().extractCredentialMappings()
      }
    }, null, 2)
  },

  setInterface: (interfaceConfig) => {
    set({ interface: interfaceConfig })
  }
}))
