import { useState, useCallback, useEffect, useRef } from 'react'
import { useEditorStore } from '@/store/editor'
import { StartNodeProperties } from './properties/StartNodeProperties'
import { ModelNodeProperties } from './properties/ModelNodeProperties'
import { EndNodeProperties } from './properties/EndNodeProperties'
import { ActionNodeProperties } from './properties/ActionNodeProperties'
import { MessageFormatterNodeProperties } from './properties/MessageFormatterNodeProperties'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { GripVertical } from 'lucide-react'

const MIN_WIDTH = 400
const MAX_WIDTH = 1200
const DEFAULT_WIDTH = 600

export function PropertiesPanel() {
  const selectedNodeId = useEditorStore((state) => state.selectedNodeId)
  const isPropertiesOpen = useEditorStore((state) => state.isPropertiesOpen)
  const nodes = useEditorStore((state) => state.nodes)
  const selectNode = useEditorStore((state) => state.selectNode)

  const [panelWidth, setPanelWidth] = useState(DEFAULT_WIDTH)
  const [isResizing, setIsResizing] = useState(false)
  const startXRef = useRef(0)
  const startWidthRef = useRef(DEFAULT_WIDTH)

  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault()
    setIsResizing(true)
    startXRef.current = e.clientX
    startWidthRef.current = panelWidth
  }, [panelWidth])

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing) return

      const delta = startXRef.current - e.clientX
      const newWidth = Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, startWidthRef.current + delta))
      setPanelWidth(newWidth)
    }

    const handleMouseUp = () => {
      setIsResizing(false)
    }

    if (isResizing) {
      document.addEventListener('mousemove', handleMouseMove)
      document.addEventListener('mouseup', handleMouseUp)
      document.body.style.cursor = 'col-resize'
      document.body.style.userSelect = 'none'
    }

    return () => {
      document.removeEventListener('mousemove', handleMouseMove)
      document.removeEventListener('mouseup', handleMouseUp)
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
    }
  }, [isResizing])

  // Find the selected node
  const selectedNode = selectedNodeId
    ? nodes.find(n => n.id === selectedNodeId)
    : null

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      selectNode(null)
    }
  }

  const getNodeTitle = (): string => {
    if (!selectedNode) return 'Properties'

    switch (selectedNode.type) {
      case 'start':
        return 'Start Node Properties'
      case 'model':
        return 'Model Node Properties'
      case 'end':
        return 'End Node Properties'
      case 'action':
        return String(selectedNode.data?.displayName || 'Action Node Properties')
      case 'messageFormatter':
        return 'Message Formatter Properties'
      default:
        return 'Node Properties'
    }
  }

  const renderNodeProperties = () => {
    if (!selectedNode) return null

    switch (selectedNode.type) {
      case 'start':
        return <StartNodeProperties nodeId={selectedNode.id} />
      case 'model':
        return <ModelNodeProperties nodeId={selectedNode.id} />
      case 'end':
        return <EndNodeProperties nodeId={selectedNode.id} />
      case 'action':
        return <ActionNodeProperties nodeId={selectedNode.id} />
      case 'messageFormatter':
        return <MessageFormatterNodeProperties nodeId={selectedNode.id} />
      default:
        return (
          <div className="p-4 text-sm text-muted-foreground">
            Unknown node type: {selectedNode.type}
          </div>
        )
    }
  }

  return (
    <Sheet open={isPropertiesOpen} onOpenChange={handleOpenChange}>
      <SheetContent
        side="right"
        className="overflow-y-auto p-0"
        style={{ width: `${panelWidth}px`, maxWidth: '90vw' }}
      >
        {/* Resize handle */}
        <div
          onMouseDown={handleMouseDown}
          className="absolute left-0 top-0 bottom-0 w-3 cursor-col-resize flex items-center justify-center hover:bg-accent/50 transition-colors group z-10"
        >
          <GripVertical className="h-4 w-4 text-muted-foreground/50 group-hover:text-muted-foreground" />
        </div>

        <div className="pl-4 pr-6 py-6">
          <SheetHeader>
            <SheetTitle>{getNodeTitle()}</SheetTitle>
          </SheetHeader>
          <div className="mt-6">
            {renderNodeProperties()}
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
