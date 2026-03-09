import { useCallback, useMemo, useEffect } from 'react'
import {
  ReactFlow,
  Background,
  ConnectionMode,
  ConnectionLineType,
  Panel,
  useReactFlow,
  useNodesInitialized,
  type Connection,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { LayoutGrid } from 'lucide-react'

import { AgentModelNode } from './AgentModelNode'
import { AgentSatelliteNode } from './AgentSatelliteNode'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { agentNodeTypes } from './agentNodeTypes'
import { Button } from '@donkeywork/ui'

export function AgentCanvasInner() {
  const { screenToFlowPosition, fitView } = useReactFlow()
  const nodesInitialized = useNodesInitialized()

  const nodes = useAgentBuilderStore((s) => s.nodes)
  const edges = useAgentBuilderStore((s) => s.edges)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)
  const onNodesChange = useAgentBuilderStore((s) => s.onNodesChange)
  const onEdgesChange = useAgentBuilderStore((s) => s.onEdgesChange)
  const onConnect = useAgentBuilderStore((s) => s.onConnect)
  const selectNode = useAgentBuilderStore((s) => s.selectNode)
  const setViewport = useAgentBuilderStore((s) => s.setViewport)
  const addNode = useAgentBuilderStore((s) => s.addNode)
  const arrangeNodes = useAgentBuilderStore((s) => s.arrangeNodes)

  useEffect(() => {
    if (nodesInitialized) {
      fitView({ padding: 0.2 })
    }
  }, [nodesInitialized, fitView])

  const nodeTypes = useMemo(
    () => ({
      agentModel: AgentModelNode,
      agentSatellite: AgentSatelliteNode,
    }),
    []
  )

  const handlePaneClick = useCallback(() => selectNode(null), [selectNode])

  const handleMove = useCallback(
    (_event: MouseEvent | TouchEvent | null, viewport: { x: number; y: number; zoom: number }) => {
      setViewport(viewport)
    },
    [setViewport]
  )

  const handleDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault()
    event.dataTransfer.dropEffect = 'move'
  }, [])

  const handleDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()
      const jsonData = event.dataTransfer.getData('application/json')
      if (!jsonData) return

      let nodeTypeInfo: Record<string, unknown> = {}
      try {
        nodeTypeInfo = JSON.parse(jsonData)
      } catch {
        return
      }

      const position = screenToFlowPosition({
        x: event.clientX,
        y: event.clientY,
      })

      addNode(position, nodeTypeInfo)
    },
    [screenToFlowPosition, addNode]
  )

  const handleArrange = useCallback(() => {
    arrangeNodes()
    setTimeout(() => fitView({ padding: 0.2 }), 50)
  }, [arrangeNodes, fitView])

  // Validate connections: source must be a satellite, target must be Model,
  // and the targetHandle must match the node type's expected handle
  const isValidConnection = useCallback(
    (connection: Connection) => {
      const targetNode = nodes.find((n) => n.id === connection.target)
      if (targetNode?.data?.nodeType !== 'agentModel') return false

      const sourceNode = nodes.find((n) => n.id === connection.source)
      if (!sourceNode) return false

      const sourceType = sourceNode.data?.nodeType as string
      const expectedHandle = agentNodeTypes[sourceType]?.targetHandle
      if (!expectedHandle) return false

      return connection.targetHandle === expectedHandle
    },
    [nodes]
  )

  return (
    <div className="h-full w-full bg-background">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onPaneClick={handlePaneClick}
        onMove={handleMove}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        nodeTypes={nodeTypes}
        isValidConnection={isValidConnection}
        connectionMode={ConnectionMode.Strict}
        connectionLineType={ConnectionLineType.Bezier}
        defaultEdgeOptions={{
          type: 'default',
          animated: true,
          style: { stroke: '#22d3ee', strokeWidth: 2 },
        }}
        nodesDraggable={!isReadOnly}
        nodesConnectable={!isReadOnly}
        deleteKeyCode={isReadOnly ? null : ['Backspace', 'Delete']}
        snapToGrid
        snapGrid={[15, 15]}
        minZoom={0.5}
        maxZoom={2}
        attributionPosition="bottom-left"
        multiSelectionKeyCode="Shift"
      >
        <Background gap={16} size={1} className="bg-muted/20" />

        <Panel position="bottom-right" className="mb-2 mr-2">
          <Button
            variant="outline"
            size="icon"
            onClick={handleArrange}
            title="Arrange nodes"
            className="bg-card/80 backdrop-blur-sm shadow-lg rounded-xl hover:border-accent/50"
          >
            <LayoutGrid className="h-4 w-4" />
          </Button>
        </Panel>
      </ReactFlow>
    </div>
  )
}
