import { useState, useEffect, useCallback } from 'react'
import { Loader2, CheckCircle2, XCircle, RotateCw, Clock, Sparkles, Tag, Shield } from 'lucide-react'
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
import { a2aServers, type A2aServerTestResult } from '@donkeywork/api-client'

interface A2aServerTestDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  serverId: string
  serverName: string
}

export function A2aServerTestDialog({
  open,
  onOpenChange,
  serverId,
  serverName,
}: A2aServerTestDialogProps) {
  const [result, setResult] = useState<A2aServerTestResult | null>(null)
  const [loading, setLoading] = useState(false)

  const runTest = useCallback(async () => {
    setLoading(true)
    setResult(null)
    try {
      const data = await a2aServers.test(serverId)
      setResult(data)
    } catch (err) {
      setResult({
        success: false,
        elapsedMs: 0,
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
          <DialogTitle>Test A2A Connection</DialogTitle>
          <DialogDescription>
            {serverName}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {loading && (
            <div className="flex flex-col items-center justify-center py-12 gap-3">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Fetching agent card...</p>
            </div>
          )}

          {!loading && result && (
            <>
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

              {result.success && result.agentCard && (
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <span className="text-muted-foreground">Agent</span>
                      <p className="font-medium">{result.agentCard.name}</p>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Version</span>
                      <p className="font-medium">{result.agentCard.version}</p>
                    </div>
                  </div>

                  {result.agentCard.description && (
                    <div className="text-sm">
                      <span className="text-muted-foreground">Description</span>
                      <p className="mt-0.5">{result.agentCard.description}</p>
                    </div>
                  )}

                  {result.agentCard.capabilities && (
                    <div className="flex gap-2">
                      {result.agentCard.capabilities.streaming && (
                        <Badge variant="secondary">Streaming</Badge>
                      )}
                      {result.agentCard.capabilities.pushNotifications && (
                        <Badge variant="secondary">Push Notifications</Badge>
                      )}
                    </div>
                  )}

                  {result.agentCard.defaultInputModes && result.agentCard.defaultInputModes.length > 0 && (
                    <div className="text-sm">
                      <span className="text-muted-foreground">Input Modes: </span>
                      <span>{result.agentCard.defaultInputModes.join(', ')}</span>
                    </div>
                  )}

                  {result.agentCard.defaultOutputModes && result.agentCard.defaultOutputModes.length > 0 && (
                    <div className="text-sm">
                      <span className="text-muted-foreground">Output Modes: </span>
                      <span>{result.agentCard.defaultOutputModes.join(', ')}</span>
                    </div>
                  )}
                </div>
              )}

              {result.agentCard?.securitySchemes && Object.keys(result.agentCard.securitySchemes).length > 0 && (
                <div className="text-sm">
                  <div className="flex items-center gap-1.5 mb-1">
                    <Shield className="h-3.5 w-3.5 text-muted-foreground" />
                    <span className="text-muted-foreground">Authentication</span>
                  </div>
                  <div className="flex gap-1.5 flex-wrap">
                    {Object.entries(result.agentCard!.securitySchemes!).map(([key, scheme]) => (
                      <Badge key={key} variant="outline" className="text-xs">
                        {scheme.type === 'apiKey' && scheme.name
                          ? `${scheme.name} (${scheme.in || 'header'})`
                          : scheme.type || key}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <Clock className="h-3 w-3" />
                <span>{result.elapsedMs}ms</span>
              </div>

              {result.success && result.agentCard && result.agentCard.skills.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <div className="flex items-center gap-2 mb-3">
                      <Sparkles className="h-4 w-4 text-muted-foreground" />
                      <h3 className="text-sm font-medium">
                        Skills
                        <Badge variant="secondary" className="ml-2">{result.agentCard.skills.length}</Badge>
                      </h3>
                    </div>
                    <div className="space-y-2 max-h-[300px] overflow-y-auto">
                      {result.agentCard.skills.map((skill) => (
                        <div
                          key={skill.id}
                          className="rounded-md border border-border bg-muted/30 p-3"
                        >
                          <p className="text-sm font-medium">{skill.name}</p>
                          {skill.description && (
                            <p className="text-xs text-muted-foreground mt-1 line-clamp-2">
                              {skill.description}
                            </p>
                          )}
                          {skill.tags && skill.tags.length > 0 && (
                            <div className="flex items-center gap-1 mt-2">
                              <Tag className="h-3 w-3 text-muted-foreground" />
                              <div className="flex gap-1 flex-wrap">
                                {skill.tags.map((tag) => (
                                  <Badge key={tag} variant="outline" className="text-xs px-1.5 py-0">
                                    {tag}
                                  </Badge>
                                ))}
                              </div>
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                </>
              )}

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
