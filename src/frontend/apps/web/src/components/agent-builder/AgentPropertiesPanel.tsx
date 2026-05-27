import { useState, useCallback, useEffect, useRef } from 'react'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { ModelProperties } from './properties/ModelProperties'
import { PromptProperties } from './properties/PromptProperties'
import { McpServerProperties } from './properties/McpServerProperties'
import { ToolGroupProperties } from './properties/ToolGroupProperties'
import { SandboxProperties } from './properties/SandboxProperties'
import { SubAgentProperties } from './properties/SubAgentProperties'
import { A2aServerProperties } from './properties/A2aServerProperties'
import { AgentSettingsProperties } from './properties/AgentSettingsProperties'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@donkeywork/ui'
import { GripVertical } from 'lucide-react'

const MIN_WIDTH = 400
const MAX_WIDTH = 1200
const DEFAULT_WIDTH = 600

export function AgentPropertiesPanel() {
  const selectedNodeId = useAgentBuilderStore((s) => s.selectedNodeId)
  const isPropertiesOpen = useAgentBuilderStore((s) => s.isPropertiesOpen)
  const showingAgentSettings = useAgentBuilderStore((s) => s.showingAgentSettings)
  const nodes = useAgentBuilderStore((s) => s.nodes)
  const nodeConfigurations = useAgentBuilderStore((s) => s.nodeConfigurations)
  const selectNode = useAgentBuilderStore((s) => s.selectNode)

  const [panelWidth, setPanelWidth] = useState(DEFAULT_WIDTH)
  const [isResizing, setIsResizing] = useState(false)
  const startXRef = useRef(0)
  const startWidthRef = useRef(DEFAULT_WIDTH)

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault()
      setIsResizing(true)
      startXRef.current = e.clientX
      startWidthRef.current = panelWidth
    },
    [panelWidth]
  )

  useEffect(() => {
    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing) return
      const delta = startXRef.current - e.clientX
      setPanelWidth(Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, startWidthRef.current + delta)))
    }
    const handleMouseUp = () => setIsResizing(false)

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

  const selectedNode = selectedNodeId ? nodes.find((n) => n.id === selectedNodeId) : null
  const selectedConfig = selectedNodeId ? nodeConfigurations[selectedNodeId] : null

  const handleOpenChange = (open: boolean) => {
    if (!open) selectNode(null)
  }

  const getTitle = (): string => {
    if (showingAgentSettings) return 'Agent Settings'
    if (!selectedNode) return 'Properties'
    const nodeType = (selectedConfig?.type || selectedNode.data?.nodeType) as string
    switch (nodeType) {
      case 'agentModel':
        return 'Model Properties'
      case 'agentPrompt':
        return 'Prompt Properties'
      case 'agentMcpServer':
        return `MCP Server: ${(selectedConfig?.mcpServerName as string) || 'Unknown'}`
      case 'agentToolGroup':
        return `Tool Group: ${(selectedConfig?.toolGroupName as string) || 'Unknown'}`
      case 'agentSandbox':
        return 'Sandbox Properties'
      case 'agentSubAgent':
        return `Sub-Agent: ${(selectedConfig?.subAgentName as string) || 'Unknown'}`
      case 'agentA2aServer':
        return `A2A Server: ${(selectedConfig?.a2aServerName as string) || 'Unknown'}`
      default:
        return 'Node Properties'
    }
  }

  const renderProperties = () => {
    if (showingAgentSettings) {
      return <AgentSettingsProperties />
    }

    if (!selectedNode || !selectedNodeId) return null
    const nodeType = (selectedConfig?.type || selectedNode.data?.nodeType) as string

    switch (nodeType) {
      case 'agentModel':
        return <ModelProperties nodeId={selectedNodeId} />
      case 'agentPrompt':
        return <PromptProperties nodeId={selectedNodeId} />
      case 'agentMcpServer':
        return <McpServerProperties nodeId={selectedNodeId} />
      case 'agentToolGroup':
        return <ToolGroupProperties nodeId={selectedNodeId} />
      case 'agentSandbox':
        return <SandboxProperties />
      case 'agentSubAgent':
        return <SubAgentProperties nodeId={selectedNodeId} />
      case 'agentA2aServer':
        return <A2aServerProperties nodeId={selectedNodeId} />
      default:
        return <div className="p-4 text-sm text-muted-foreground">Unknown node type</div>
    }
  }

  return (
    <Sheet open={isPropertiesOpen} onOpenChange={handleOpenChange}>
      <SheetContent
        side="right"
        className="overflow-y-auto p-0"
        style={{ width: `${panelWidth}px`, maxWidth: '90vw' }}
      >
        <div
          onMouseDown={handleMouseDown}
          className="absolute left-0 top-0 bottom-0 w-3 cursor-col-resize flex items-center justify-center hover:bg-accent/50 transition-colors group z-10"
        >
          <GripVertical className="h-4 w-4 text-muted-foreground/50 group-hover:text-muted-foreground" />
        </div>

        <div className="pl-4 pr-6 py-6">
          <SheetHeader>
            <SheetTitle>{getTitle()}</SheetTitle>
          </SheetHeader>
          <div className="mt-6">{renderProperties()}</div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
