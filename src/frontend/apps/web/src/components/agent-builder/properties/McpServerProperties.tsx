import { useState, useEffect, useCallback } from 'react'
import { useAgentBuilderStore } from '@/store/agentBuilder'
import { mcpServers, type McpToolInfo } from '@donkeywork/api-client'
import { Server, ChevronDown, ChevronRight, RefreshCw, Loader2 } from 'lucide-react'
import { Switch, Label, Button, Checkbox } from '@donkeywork/ui'

interface ToolOverrideConfig {
  toolName: string
  enabled: boolean
  deferred?: boolean
}

interface McpServerPropertiesProps {
  nodeId: string
}

export function McpServerProperties({ nodeId }: McpServerPropertiesProps) {
  const config = useAgentBuilderStore((s) => s.nodeConfigurations[nodeId])
  const updateNodeConfig = useAgentBuilderStore((s) => s.updateNodeConfig)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)

  const [tools, setTools] = useState<McpToolInfo[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [advancedOpen, setAdvancedOpen] = useState(false)

  const serverId = (config?.mcpServerId as string) || ''

  const fetchTools = useCallback(async () => {
    if (!serverId) return
    setLoading(true)
    setError(null)
    try {
      const result = await mcpServers.test(serverId)
      setTools(result.tools || [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to fetch tools')
    } finally {
      setLoading(false)
    }
  }, [serverId])

  useEffect(() => {
    fetchTools()
  }, [fetchTools])

  if (!config) return null

  const serverName = (config.mcpServerName as string) || 'Unknown Server'
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
    // Remove overrides that match defaults (enabled=true, deferred=undefined)
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
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-purple-500 to-fuchsia-600 shadow-lg shadow-purple-500/25">
          <Server className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">{serverName}</div>
          <div className="text-xs text-muted-foreground font-mono">{serverId}</div>
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

            {error && (
              <div className="text-sm text-destructive py-2">{error}</div>
            )}

            {!loading && !error && tools.length === 0 && (
              <div className="text-sm text-muted-foreground py-2">No tools discovered</div>
            )}

            {!loading && tools.map((tool) => (
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
                    <div className="text-sm font-medium truncate">{tool.name}</div>
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

            <Button
              variant="outline"
              size="sm"
              onClick={fetchTools}
              disabled={loading}
              className="w-full mt-2"
            >
              <RefreshCw className={`h-3 w-3 mr-1.5 ${loading ? 'animate-spin' : ''}`} />
              Refresh Tools
            </Button>
          </div>
        )}
      </div>

      <p className="text-xs text-muted-foreground">
        This MCP server is connected to the agent. Remove the node from the canvas to disconnect it.
      </p>
    </div>
  )
}
