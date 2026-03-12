import { useState, useEffect, useCallback } from 'react'
import { Loader2, CheckCircle2, XCircle, RotateCw, Wrench, Clock } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  Button,
  Badge,
  Separator,
} from '@donkeywork/ui'
import { mcpServers, type McpServerTestResult } from '@donkeywork/api-client'

interface McpServerTestDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  serverId: string
  serverName: string
}

export function McpServerTestDialog({
  open,
  onOpenChange,
  serverId,
  serverName,
}: McpServerTestDialogProps) {
  const [result, setResult] = useState<McpServerTestResult | null>(null)
  const [loading, setLoading] = useState(false)

  const runTest = useCallback(async () => {
    setLoading(true)
    setResult(null)
    try {
      const data = await mcpServers.test(serverId)
      setResult(data)
    } catch (err) {
      setResult({
        success: false,
        elapsedMs: 0,
        tools: [],
        error: err instanceof Error ? err.message : 'Failed to test connection',
      })
    } finally {
      setLoading(false)
    }
  }, [serverId])

  useEffect(() => {
    if (open) {
      runTest()
    } else {
      setResult(null)
    }
  }, [open, runTest])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[550px] max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Test Connection</DialogTitle>
          <DialogDescription>
            {serverName}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {loading && (
            <div className="flex flex-col items-center justify-center py-12 gap-3">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Connecting to server...</p>
            </div>
          )}

          {!loading && result && (
            <>
              {/* Status */}
              <div className={`rounded-lg border p-4 ${
                result.success
                  ? 'bg-emerald-500/10 border-emerald-500/20'
                  : 'bg-red-500/10 border-red-500/20'
              }`}>
                <div className="flex items-center gap-3">
                  {result.success ? (
                    <CheckCircle2 className="h-5 w-5 text-emerald-500 shrink-0" />
                  ) : (
                    <XCircle className="h-5 w-5 text-red-500 shrink-0" />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className={`text-sm font-medium ${
                      result.success ? 'text-emerald-500' : 'text-red-500'
                    }`}>
                      {result.success ? 'Connection successful' : 'Connection failed'}
                    </p>
                    {result.error && (
                      <p className="text-sm text-red-400 mt-1">{result.error}</p>
                    )}
                  </div>
                </div>
              </div>

              {/* Server Info */}
              {result.success && (
                <div className="grid grid-cols-2 gap-3 text-sm">
                  {result.serverName && (
                    <div>
                      <span className="text-muted-foreground">Server</span>
                      <p className="font-medium">{result.serverName}</p>
                    </div>
                  )}
                  {result.serverVersion && (
                    <div>
                      <span className="text-muted-foreground">Version</span>
                      <p className="font-medium">{result.serverVersion}</p>
                    </div>
                  )}
                </div>
              )}

              {/* Elapsed time */}
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />
                <span>{result.elapsedMs}ms</span>
              </div>

              {/* Tools */}
              {result.success && result.tools.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <div className="flex items-center gap-2 mb-3">
                      <Wrench className="h-4 w-4 text-muted-foreground" />
                      <h3 className="text-sm font-medium">
                        Tools
                        <Badge variant="secondary" className="ml-2">{result.tools.length}</Badge>
                      </h3>
                    </div>
                    <div className="space-y-2 max-h-[300px] overflow-y-auto">
                      {result.tools.map((tool) => (
                        <div
                          key={tool.name}
                          className="rounded-md border border-border bg-muted/30 p-3"
                        >
                          <p className="text-sm font-medium">{tool.name}</p>
                          {tool.description && (
                            <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                              {tool.description}
                            </p>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                </>
              )}

              {result.success && result.tools.length === 0 && (
                <>
                  <Separator />
                  <p className="text-sm text-muted-foreground text-center py-4">
                    No tools discovered on this server
                  </p>
                </>
              )}

              {/* Retry button */}
              <div className="flex justify-end pt-2">
                <Button variant="outline" size="sm" onClick={runTest}>
                  <RotateCw className="h-3.5 w-3.5 mr-1.5" />
                  Retry
                </Button>
              </div>
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
