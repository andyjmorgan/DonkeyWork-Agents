import { useState, useEffect } from 'react'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { toolGroups, type ToolDefinition } from '@donkeywork/api-client'
import { Zap, ChevronDown, ChevronRight, Loader2 } from 'lucide-react'
import { Switch, Label, Checkbox } from '@donkeywork/ui'

interface ToolOverrideConfig {
  toolName: string
  enabled: boolean
  deferred?: boolean
}

interface ToolGroupPropertiesProps {
  nodeId: string
}

export function ToolGroupProperties({ nodeId }: ToolGroupPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])
  const updateNodeConfig = useAgentBuilderStore((s) => s.updateNodeConfig)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)

  const [groupTools, setGroupTools] = useState<ToolDefinition[]>([])
  const [loading, setLoading] = useState(false)
  const [advancedOpen, setAdvancedOpen] = useState(false)

  const groupId = (config?.toolGroupId as string) || ''

  useEffect(() => {
    if (!groupId) return
    setLoading(true)
    toolGroups
      .list()
      .then((groups) => {
        const group = groups.find((g) => g.id === groupId)
        setGroupTools(group?.tools || [])
      })
      .catch(console.error)
      .finally(() => setLoading(false))
  }, [groupId])

  if (!config) return null

  const groupName = (config.toolGroupName as string) || 'Unknown'
  const toolIds = (config.toolIds as string[]) || []
  const deferToolLoading = (config.deferToolLoading as boolean) ?? false
  const toolOverrides = (config.toolOverrides as ToolOverrideConfig[]) || []

  const handleDeferToggle = (checked: boolean) => {
    updateNodeConfig(nodeId, { deferToolLoading: checked })
  }

  const getToolOverride = (toolName: string): ToolOverrideConfig | undefined => {
    return toolOverrides.find((o) => o.toolName === toolName)
  }

  const updateToolOverride = (toolName: string, update: Partial<ToolOverrideConfig>) => {
    const existing = [...toolOverrides]
    const idx = existing.findIndex((o) => o.toolName === toolName)
    if (idx >= 0) {
      existing[idx] = { ...existing[idx], ...update }
    } else {
      existing.push({ toolName, enabled: true, ...update })
    }
    const cleaned = existing.filter(
      (o) => !o.enabled || o.deferred !== undefined
    )
    updateNodeConfig(nodeId, { toolOverrides: cleaned })
  }

  const isToolEnabled = (toolName: string): boolean => {
    const ov = getToolOverride(toolName)
    return ov?.enabled ?? true
  }

  const isToolDeferred = (toolName: string): boolean => {
    const ov = getToolOverride(toolName)
    return ov?.deferred ?? deferToolLoading
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-yellow-500 to-amber-600 shadow-lg shadow-yellow-500/25">
          <Zap className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{groupName}</div>
          <div className="text-xs text-muted-foreground">
            {toolIds.length} tool{toolIds.length !== 1 ? 's' : ''} included
          </div>
        </div>
      </div>

      {/* Defer Tool Loading Toggle */}
      <div className="flex items-center justify-between rounded-lg border border-border p-3">
        <div className="space-y-0.5">
          <Label htmlFor={`defer-${nodeId}`} className="text-sm font-medium">
            Defer Tool Loading
          </Label>
          <p className="text-xs text-muted-foreground">
            Defer all tools by default
          </p>
        </div>
        <Switch
          id={`defer-${nodeId}`}
          checked={deferToolLoading}
          onCheckedChange={handleDeferToggle}
          disabled={isReadOnly}
        />
      </div>

      {/* Advanced Tool Selection */}
      <div className="rounded-lg border border-border">
        <button
          type="button"
          className="flex w-full items-center justify-between p-3 text-sm font-medium hover:bg-muted/50 transition-colors"
          onClick={() => setAdvancedOpen(!advancedOpen)}
        >
          <span>Advanced Tool Selection</span>
          {advancedOpen ? (
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          ) : (
            <ChevronRight className="h-4 w-4 text-muted-foreground" />
          )}
        </button>

        {advancedOpen && (
          <div className="border-t border-border p-3 space-y-2">
            {loading && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground py-2">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading tools...
              </div>
            )}

            {!loading && groupTools.length === 0 && (
              <div className="text-sm text-muted-foreground py-2">No tools in this group</div>
            )}

            {!loading && groupTools.map((tool) => (
              <div
                key={tool.name}
                className="flex items-center justify-between gap-2 rounded-md border border-border/50 px-3 py-2"
              >
                <div className="flex items-center gap-2 min-w-0 flex-1">
                  <Checkbox
                    checked={isToolEnabled(tool.name)}
                    onCheckedChange={(checked) =>
                      updateToolOverride(tool.name, { enabled: !!checked })
                    }
                    disabled={isReadOnly}
                  />
                  <div className="min-w-0">
                    <div className="text-sm font-medium truncate">
                      {tool.displayName || tool.name.replace(/_/g, ' ')}
                    </div>
                    {tool.description && (
                      <div className="text-xs text-muted-foreground truncate">
                        {tool.description}
                      </div>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  <span className="text-xs text-muted-foreground">Defer</span>
                  <Switch
                    checked={isToolDeferred(tool.name)}
                    onCheckedChange={(checked) =>
                      updateToolOverride(tool.name, {
                        enabled: isToolEnabled(tool.name),
                        deferred: checked === deferToolLoading ? undefined : checked,
                      })
                    }
                    disabled={isReadOnly || !isToolEnabled(tool.name)}
                    className="scale-75"
                  />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        Remove the node from the canvas to detach this tool group.
      </p>
    </div>
  )
}
