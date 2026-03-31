import { memo } from 'react'
import { Handle, Position, type NodeProps } from '@xyflow/react'
import { FileText, Server, Zap, Box, Bot, type LucideIcon } from 'lucide-react'
import { AgentBaseNode } from './AgentBaseNode'
import { agentNodeTypes } from './agentNodeTypes'

const iconMap: Record<string, LucideIcon> = {
  'file-text': FileText,
  server: Server,
  zap: Zap,
  container: Box,
  bot: Bot,
}

const colorSchemes: Record<string, { border: string; bg: string; handle: string }> = {
  emerald: {
    border: 'border-emerald-500',
    bg: 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-lg shadow-emerald-500/25',
    handle: '!bg-emerald-500',
  },
  purple: {
    border: 'border-purple-500',
    bg: 'bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25',
    handle: '!bg-purple-500',
  },
  amber: {
    border: 'border-amber-500',
    bg: 'bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25',
    handle: '!bg-amber-500',
  },
  cyan: {
    border: 'border-cyan-500',
    bg: 'bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25',
    handle: '!bg-cyan-500',
  },
  rose: {
    border: 'border-rose-500',
    bg: 'bg-gradient-to-br from-rose-500 to-pink-600 shadow-lg shadow-rose-500/25',
    handle: '!bg-rose-500',
  },
  orange: {
    border: 'border-orange-500',
    bg: 'bg-gradient-to-br from-orange-500 to-amber-600 shadow-lg shadow-orange-500/25',
    handle: '!bg-orange-500',
  },
}

const defaultScheme = colorSchemes.purple

/** Map from target handle name to the Position the source handle should be at */
const handlePositionMap: Record<string, Position> = {
  prompts: Position.Bottom,
  tools: Position.Right,
  agents: Position.Left,
}

export interface AgentSatelliteNodeData {
  label: string
  nodeType: string
  displayName: string
  icon?: string
  color?: string
  canDelete?: boolean
}

export const AgentSatelliteNode = memo(({ id, data, selected }: NodeProps) => {
  const nodeData = data as unknown as AgentSatelliteNodeData
  const Icon = iconMap[nodeData.icon || ''] || Zap
  const colors = colorSchemes[nodeData.color || ''] || defaultScheme

  const canDelete = nodeData.canDelete !== false

  // Determine handle position based on which model handle this connects to
  const targetHandle = agentNodeTypes[nodeData.nodeType]?.targetHandle || 'tools'
  const handlePosition = handlePositionMap[targetHandle] || Position.Bottom

  return (
    <AgentBaseNode id={id} selected={selected} borderColor={colors.border} canDelete={canDelete}>
      <div className="flex items-center gap-2">
        <div className={`flex h-8 w-8 items-center justify-center rounded-lg ${colors.bg}`}>
          <Icon className="h-4 w-4 text-white" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="font-medium text-sm">{nodeData.displayName}</div>
          {nodeData.label !== nodeData.displayName && (
            <div className="text-xs text-muted-foreground truncate">{nodeData.label}</div>
          )}
        </div>
      </div>

      {/* Source handle — position depends on which model handle this node connects to */}
      <Handle
        type="source"
        position={handlePosition}
        className={`!w-3 !h-3 ${colors.handle} !border-2 !border-background`}
      />
    </AgentBaseNode>
  )
})

AgentSatelliteNode.displayName = 'AgentSatelliteNode'
