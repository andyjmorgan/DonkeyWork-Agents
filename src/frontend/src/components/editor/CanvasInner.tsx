import { useCallback, useMemo, useEffect } from 'react'
import {
  ReactFlow,
  Background,
  ConnectionMode,
  ConnectionLineType,
  MarkerType,
  Panel,
  useReactFlow,
  useNodesInitialized
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { AlignVerticalSpaceAround } from 'lucide-react'

import { SchemaNode } from './nodes'
import { useEditorStore } from '@/store/editor'
import { Button } from '@/components/ui/button'

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
  const tidyUpNodes = useEditorStore((state) => state.tidyUpNodes)

  // Fit view when nodes are initialized
  useEffect(() => {
    if (nodesInitialized) {
      fitView({ padding: 0.2 })
    }
  }, [nodesInitialized, fitView])

  // All nodes use SchemaNode - the component reads nodeType from data
  // to determine icon, color, handles, and delete behavior
  const nodeTypes = useMemo(
    () => ({
      schemaNode: SchemaNode
    }),
    []
  )

  // Handle canvas click (deselect)
  const handlePaneClick = useCallback(() => {
    selectNode(null)
  }, [selectNode])

  // Handle viewport change
  const handleMove = useCallback(
    (_event: MouseEvent | TouchEvent | null, viewport: { x: number; y: number; zoom: number }) => {
      setViewport(viewport)
    },
    [setViewport]
  )

  // Handle drag over
  const handleDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault()
    event.dataTransfer.dropEffect = 'move'
  }, [])

  // Handle format/tidy up
  const handleTidyUp = useCallback(() => {
    tidyUpNodes()
    // Fit view after layout to show all nodes
    setTimeout(() => fitView({ padding: 0.2 }), 50)
  }, [tidyUpNodes, fitView])

  // Handle drop
  const handleDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()

      // Get node type info from JSON data
      const jsonData = event.dataTransfer.getData('application/json')
      if (!jsonData) return

      let nodeTypeInfo: Record<string, unknown> = {}
      try {
        nodeTypeInfo = JSON.parse(jsonData)
      } catch (error) {
        console.error('Failed to parse drop data:', error)
        return
      }

      // Get the drop position
      const position = screenToFlowPosition({
        x: event.clientX,
        y: event.clientY
      })

      // Add the node with schema data
      addNode(position, nodeTypeInfo)
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
          style: { stroke: '#22d3ee', strokeWidth: 2 },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#22d3ee' }
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


        <Panel position="bottom-right" className="mb-2 mr-2">
          <Button
            variant="outline"
            size="icon"
            onClick={handleTidyUp}
            title="Format layout"
            className="bg-card/80 backdrop-blur-sm shadow-lg rounded-xl hover:border-accent/50"
          >
            <AlignVerticalSpaceAround className="h-4 w-4" />
          </Button>
        </Panel>
      </ReactFlow>
    </div>
  )
}
