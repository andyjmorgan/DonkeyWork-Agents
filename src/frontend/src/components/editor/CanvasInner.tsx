import { useCallback, useMemo, useEffect } from 'react'
import {
  ReactFlow,
  Background,
  ConnectionMode,
  ConnectionLineType,
  type Node,
  MarkerType,
  Panel,
  useReactFlow,
  useNodesInitialized
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'

import { StartNode, ModelNode, EndNode, ActionNode } from './nodes'
import { useEditorStore } from '@/store/editor'

export function CanvasInner() {
  const { screenToFlowPosition, fitView } = useReactFlow()
  const nodesInitialized = useNodesInitialized()

  const nodes = useEditorStore((state) => state.nodes)
  const edges = useEditorStore((state) => state.edges)
  const onNodesChange = useEditorStore((state) => state.onNodesChange)
  const onEdgesChange = useEditorStore((state) => state.onEdgesChange)
  const onConnect = useEditorStore((state) => state.onConnect)
  const selectNode = useEditorStore((state) => state.selectNode)
  const setViewport = useEditorStore((state) => state.setViewport)
  const addNode = useEditorStore((state) => state.addNode)

  // Fit view when nodes are initialized
  useEffect(() => {
    if (nodesInitialized) {
      fitView({ padding: 0.2 })
    }
  }, [nodesInitialized, fitView])

  // Define custom node types
  const nodeTypes = useMemo(
    () => ({
      start: StartNode,
      model: ModelNode,
      end: EndNode,
      action: ActionNode
    }),
    []
  )

  // Handle canvas click (deselect)
  const handlePaneClick = useCallback(() => {
    selectNode(null)
  }, [selectNode])

  // Handle viewport change
  const handleMove = useCallback(
    (_event: any, viewport: any) => {
      setViewport(viewport)
    },
    [setViewport]
  )

  // Handle drag over
  const handleDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault()
    event.dataTransfer.dropEffect = 'move'
  }, [])

  // Handle drop
  const handleDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()

      const type = event.dataTransfer.getData('application/reactflow')
      if (!type) return

      // Get the drop position
      const position = screenToFlowPosition({
        x: event.clientX,
        y: event.clientY
      })

      // Get additional data if it's a model node
      const jsonData = event.dataTransfer.getData('application/json')
      let additionalConfig = {}

      if (jsonData) {
        try {
          const data = JSON.parse(jsonData)
          additionalConfig = data
        } catch (error) {
          console.error('Failed to parse drop data:', error)
        }
      }

      // Add the node
      addNode(type, position, additionalConfig)
    },
    [screenToFlowPosition, addNode]
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
        connectionMode={ConnectionMode.Strict}
        connectionLineType={ConnectionLineType.SmoothStep}
        defaultEdgeOptions={{
          type: 'smoothstep',
          animated: true,
          style: { stroke: '#94a3b8', strokeWidth: 2 },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#94a3b8' }
        }}
        snapToGrid
        snapGrid={[15, 15]}
        minZoom={0.5}
        maxZoom={2}
        attributionPosition="bottom-left"
        deleteKeyCode={['Backspace', 'Delete']}
        multiSelectionKeyCode="Shift"
      >
        <Background
          gap={16}
          size={1}
          className="bg-muted/20"
        />

        <Panel position="top-center" className="bg-card/80 backdrop-blur-sm border border-border rounded-lg px-4 py-2 shadow-lg">
          <div className="text-xs text-muted-foreground">
            Drag to move • Shift + drag to select multiple • Delete to remove
          </div>
        </Panel>
      </ReactFlow>
    </div>
  )
}
