import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  Clock,
  CheckCircle2,
  XCircle,
  Loader2,
  Cpu,
  Timer,
  Ban,
  MessageSquare,
  ChevronDown,
  ChevronRight,
  Wrench,
  AlertTriangle,
  Brain,
  Globe,
  Quote,
  User,
  Bot,
  Settings,
  Pause,
  Skull,
} from 'lucide-react'
import {
  Button,
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@donkeywork/ui'
import { JsonViewer } from '@/components/ui/json-viewer'
import { agentExecutions, type AgentExecutionDetail } from '@donkeywork/api-client'
import type { InternalMessage, InternalContentBlock } from '@donkeywork/api-client'

function StatusIcon({ status, size = 'h-4 w-4' }: { status: string; size?: string }) {
  switch (status.toLowerCase()) {
    case 'completed':
      return <CheckCircle2 className={`${size} text-green-600`} />
    case 'failed':
      return <XCircle className={`${size} text-destructive`} />
    case 'running':
      return <Loader2 className={`${size} animate-spin text-blue-600`} />
    case 'cancelled':
      return <Ban className={`${size} text-amber-500`} />
    case 'idle':
      return <Pause className={`${size} text-cyan-500`} />
    case 'stale':
      return <Skull className={`${size} text-orange-500`} />
    default:
      return <Clock className={`${size} text-muted-foreground`} />
  }
}

function getStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status.toLowerCase()) {
    case 'completed': return 'outline'
    case 'failed': return 'destructive'
    case 'running': return 'secondary'
    case 'idle': return 'secondary'
    case 'stale': return 'destructive'
    default: return 'default'
  }
}

function formatDuration(ms?: number) {
  if (ms == null) return '-'
  if (ms < 1000) return `${ms}ms`
  if (ms < 60000) return `${(ms / 1000).toFixed(2)}s`
  return `${(ms / 60000).toFixed(2)}m`
}

function formatTokens(count?: number) {
  if (count == null) return '-'
  if (count < 1000) return count.toLocaleString()
  return `${(count / 1000).toFixed(1)}k`
}

const TYPE_COLORS: Record<string, string> = {
  conversation: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20',
  delegate: 'bg-purple-500/10 text-purple-400 border-purple-500/20',
  agent: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  scheduled: 'bg-orange-500/10 text-orange-400 border-orange-500/20',
}

function RoleIcon({ role }: { role: string }) {
  switch (role.toLowerCase()) {
    case 'user': return <User className="h-4 w-4 text-blue-400" />
    case 'assistant': return <Bot className="h-4 w-4 text-cyan-400" />
    case 'system': return <Settings className="h-4 w-4 text-amber-400" />
    default: return <MessageSquare className="h-4 w-4 text-muted-foreground" />
  }
}

function ContentBlockRenderer({ block }: { block: InternalContentBlock }) {
  switch (block.$type) {
    case 'InternalTextBlock':
      return (
        <div className="whitespace-pre-wrap text-sm">{block.text}</div>
      )
    case 'InternalThinkingBlock':
      return (
        <div className="rounded-md border border-purple-500/20 bg-purple-500/5 p-3">
          <div className="flex items-center gap-1.5 text-xs font-medium text-purple-400 mb-1">
            <Brain className="h-3 w-3" />
            Thinking
          </div>
          <div className="whitespace-pre-wrap text-sm text-muted-foreground">{block.text}</div>
        </div>
      )
    case 'InternalToolUseBlock':
    case 'InternalServerToolUseBlock':
      return (
        <div className="rounded-md border border-amber-500/20 bg-amber-500/5 p-3">
          <div className="flex items-center gap-1.5 text-xs font-medium text-amber-400 mb-1">
            <Wrench className="h-3 w-3" />
            Tool Call: {block.name}
          </div>
          <div className="text-[10px] font-mono text-muted-foreground mb-2">ID: {block.id}</div>
          <JsonViewer data={block.input} collapsed={1} className="text-xs" />
        </div>
      )
    case 'InternalWebSearchResultBlock':
    case 'InternalWebFetchToolResultBlock':
      return (
        <div className="rounded-md border border-teal-500/20 bg-teal-500/5 p-3">
          <div className="flex items-center gap-1.5 text-xs font-medium text-teal-400 mb-1">
            <Globe className="h-3 w-3" />
            {block.$type === 'InternalWebSearchResultBlock' ? 'Web Search Result' : 'Web Fetch Result'}
          </div>
          <pre className="whitespace-pre-wrap text-xs text-muted-foreground max-h-48 overflow-y-auto">
            {block.rawJson}
          </pre>
        </div>
      )
    case 'InternalCitationBlock':
      return (
        <div className="rounded-md border border-blue-500/20 bg-blue-500/5 p-3">
          <div className="flex items-center gap-1.5 text-xs font-medium text-blue-400 mb-1">
            <Quote className="h-3 w-3" />
            Citation: {block.title}
          </div>
          <div className="text-xs text-muted-foreground">{block.citedText}</div>
          <div className="text-[10px] text-blue-400/60 mt-1 truncate">{block.url}</div>
        </div>
      )
    default:
      return <JsonViewer data={block} collapsed={1} className="text-xs" />
  }
}

function MessageRenderer({ message, index }: { message: InternalMessage; index: number }) {
  const [expanded, setExpanded] = useState(true)

  const roleBg: Record<string, string> = {
    user: 'border-blue-500/20',
    assistant: 'border-cyan-500/20',
    system: 'border-amber-500/20',
  }

  const role = message.role?.toLowerCase() ?? 'unknown'
  const borderColor = roleBg[role] ?? 'border-border'

  return (
    <div className={`rounded-lg border ${borderColor} overflow-hidden`}>
      <button
        type="button"
        onClick={() => setExpanded(v => !v)}
        className="w-full flex items-center gap-2 px-3 py-2 text-left hover:bg-muted/30 transition-colors cursor-pointer"
      >
        <span className="text-xs font-mono text-muted-foreground w-6">{index + 1}</span>
        {expanded ? <ChevronDown className="h-3 w-3 text-muted-foreground" /> : <ChevronRight className="h-3 w-3 text-muted-foreground" />}
        <RoleIcon role={role} />
        <span className="text-xs font-semibold uppercase tracking-wider">{message.role}</span>
        {'agentName' in message && message.agentName && (
          <span className="text-[10px] text-cyan-400 font-medium">{message.agentName}</span>
        )}
        <span className="text-[10px] text-muted-foreground font-mono ml-auto">{message.$type}</span>
      </button>
      {expanded && (
        <div className="px-3 pb-3 border-t border-border space-y-2 pt-2">
          {(message.turnId || message.parentTurnId) && (
            <div className="flex gap-3 text-[10px] font-mono text-muted-foreground">
              {message.turnId && <span>turn: {message.turnId.slice(0, 8)}</span>}
              {message.parentTurnId && <span>parent: {message.parentTurnId.slice(0, 8)}</span>}
            </div>
          )}
          {message.$type === 'InternalContentMessage' && (
            <div className="whitespace-pre-wrap text-sm">{message.content}</div>
          )}
          {message.$type === 'InternalAssistantMessage' && (
            <>
              {message.textContent && (
                <div className="whitespace-pre-wrap text-sm">{message.textContent}</div>
              )}
              {message.contentBlocks?.map((block, i) => (
                <ContentBlockRenderer key={i} block={block} />
              ))}
              {message.toolUses?.length > 0 && (
                <div className="space-y-2">
                  {message.toolUses.map((tool, i) => (
                    <div key={i} className="rounded-md border border-amber-500/20 bg-amber-500/5 p-3">
                      <div className="flex items-center gap-1.5 text-xs font-medium text-amber-400 mb-1">
                        <Wrench className="h-3 w-3" />
                        Tool: {tool.name}
                      </div>
                      <div className="text-[10px] font-mono text-muted-foreground mb-2">ID: {tool.id}</div>
                      <JsonViewer data={tool.input} collapsed={1} className="text-xs" />
                    </div>
                  ))}
                </div>
              )}
            </>
          )}
          {message.$type === 'InternalToolResultMessage' && (
            <div className={`rounded-md border p-3 ${message.isError ? 'border-red-500/20 bg-red-500/5' : 'border-green-500/20 bg-green-500/5'}`}>
              <div className="flex items-center gap-1.5 text-xs font-medium mb-1">
                {message.isError ? (
                  <><AlertTriangle className="h-3 w-3 text-red-400" /><span className="text-red-400">Tool Error</span></>
                ) : (
                  <><CheckCircle2 className="h-3 w-3 text-green-400" /><span className="text-green-400">Tool Result</span></>
                )}
              </div>
              <div className="text-[10px] font-mono text-muted-foreground mb-2">Tool Use ID: {message.toolUseId}</div>
              <pre className="whitespace-pre-wrap text-xs text-muted-foreground max-h-64 overflow-y-auto">{message.content}</pre>
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function CollapsibleSection({ title, defaultOpen = false, children }: { title: string; defaultOpen?: boolean; children: React.ReactNode }) {
  const [open, setOpen] = useState(defaultOpen)
  return (
    <div className="border border-border rounded-lg overflow-hidden">
      <button
        type="button"
        onClick={() => setOpen(v => !v)}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs font-medium text-foreground hover:bg-muted/30 transition-colors cursor-pointer"
      >
        {open ? <ChevronDown className="w-3 h-3" /> : <ChevronRight className="w-3 h-3" />}
        {title}
      </button>
      {open && <div className="px-3 pb-3 border-t border-border">{children}</div>}
    </div>
  )
}

export function AgentExecutionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [execution, setExecution] = useState<AgentExecutionDetail | null>(null)
  const [messages, setMessages] = useState<InternalMessage[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [messagesLoading, setMessagesLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (id) loadExecution()
  }, [id])

  const loadExecution = async () => {
    if (!id) return
    try {
      setLoading(true)
      setError(null)
      const exec = await agentExecutions.get(id)
      setExecution(exec)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load execution')
    } finally {
      setLoading(false)
    }
  }

  const loadMessages = async () => {
    if (!id || messages !== null) return
    try {
      setMessagesLoading(true)
      const response = await agentExecutions.getMessages(id)
      setMessages(response.messages)
    } catch {
      setMessages([])
    } finally {
      setMessagesLoading(false)
    }
  }

  useEffect(() => {
    if (execution) loadMessages()
  }, [execution])

  if (loading) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <div className="flex h-64 items-center justify-center">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      </div>
    )
  }

  if (error || !execution) {
    return (
      <div className="p-4 md:p-6 lg:p-8">
        <Button variant="ghost" onClick={() => navigate('/agent-executions')} className="mb-4">
          <ArrowLeft className="h-4 w-4 mr-2" />
          Back
        </Button>
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-8 text-center">
          <XCircle className="mx-auto h-12 w-12 text-destructive" />
          <h3 className="mt-4 text-lg font-medium">Failed to load execution</h3>
          <p className="mt-2 text-sm text-muted-foreground">{error || 'Execution not found'}</p>
        </div>
      </div>
    )
  }

  const typeColor = TYPE_COLORS[execution.agentType.toLowerCase()] ?? TYPE_COLORS.agent

  let contractJson: unknown = null
  if (execution.contractSnapshot) {
    try { contractJson = JSON.parse(execution.contractSnapshot) } catch { /* ignore */ }
  }

  let outputJson: unknown = null
  if (execution.output) {
    try { outputJson = JSON.parse(execution.output) } catch { /* ignore */ }
  }

  return (
    <div className="p-4 md:p-6 lg:p-8 space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/agent-executions')} aria-label="Back to agent executions" title="Back to agent executions">
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h1 className="text-2xl font-bold">{execution.label}</h1>
            <span className={`rounded px-2 py-0.5 text-[10px] uppercase tracking-wider font-semibold border ${typeColor}`}>
              {execution.agentType}
            </span>
          </div>
          <p className="text-xs text-muted-foreground font-mono truncate">{execution.grainKey}</p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <StatusIcon status={execution.status} size="h-5 w-5" />
          <Badge variant={getStatusVariant(execution.status)} className="text-sm">
            {execution.status}
          </Badge>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Model</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm font-semibold font-mono truncate">{execution.modelId ?? '-'}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Started</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm font-semibold">{new Date(execution.startedAt).toLocaleString()}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Duration</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm font-semibold flex items-center gap-2">
              <Timer className="h-4 w-4 text-muted-foreground" />
              {formatDuration(execution.durationMs)}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Input Tokens</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm font-semibold flex items-center gap-2">
              <Cpu className="h-4 w-4 text-muted-foreground" />
              {formatTokens(execution.inputTokensUsed)}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Output Tokens</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm font-semibold flex items-center gap-2">
              <Cpu className="h-4 w-4 text-muted-foreground" />
              {formatTokens(execution.outputTokensUsed)}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Error Message */}
      {execution.errorMessage && (
        <Card className="border-destructive/50 bg-destructive/10">
          <CardHeader className="pb-2">
            <CardTitle className="text-destructive flex items-center gap-2">
              <XCircle className="h-5 w-5" />
              Error
            </CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="text-sm text-destructive whitespace-pre-wrap">{execution.errorMessage}</pre>
          </CardContent>
        </Card>
      )}

      {/* Input / Output / Contract */}
      <div className="space-y-3">
        {execution.input && (
          <CollapsibleSection title="Input">
            <pre className="text-xs text-muted-foreground whitespace-pre-wrap mt-2 font-mono">{execution.input}</pre>
          </CollapsibleSection>
        )}
        {outputJson != null && (
          <CollapsibleSection title="Output" defaultOpen>
            <div className="mt-2">
              <JsonViewer data={outputJson} collapsed={2} className="text-xs" />
            </div>
          </CollapsibleSection>
        )}
        {contractJson != null && (
          <CollapsibleSection title="Contract Snapshot">
            <div className="mt-2">
              <JsonViewer data={contractJson} collapsed={2} className="text-xs" />
            </div>
          </CollapsibleSection>
        )}
      </div>

      {/* Raw Message Exchange */}
      <Card className="flex flex-col min-h-0" style={{ maxHeight: 'calc(100vh - 2rem)' }}>
        <CardHeader className="shrink-0">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                <MessageSquare className="h-5 w-5" />
                Raw Message Exchange
              </CardTitle>
              <CardDescription>
                {messages !== null ? `${messages.length} message${messages.length !== 1 ? 's' : ''}` : 'Loading...'}
              </CardDescription>
            </div>
            <div className="text-right text-xs text-muted-foreground space-y-0.5">
              <div className="flex items-center gap-1 justify-end">
                <Clock className="h-3 w-3" />
                {new Date(execution.startedAt).toLocaleString()}
              </div>
              {execution.completedAt && (
                <div className="flex items-center gap-1 justify-end">
                  <CheckCircle2 className="h-3 w-3" />
                  {new Date(execution.completedAt).toLocaleString()}
                </div>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="flex-1 min-h-0">
          {messagesLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : messages !== null && messages.length === 0 ? (
            <div className="text-sm text-muted-foreground text-center py-8">
              No messages recorded for this execution
            </div>
          ) : messages !== null ? (
            <Tabs defaultValue="interpreted" className="flex flex-col h-full">
              <TabsList className="shrink-0">
                <TabsTrigger value="interpreted">Interpreted</TabsTrigger>
                <TabsTrigger value="json">JSON</TabsTrigger>
              </TabsList>
              <TabsContent value="interpreted" className="flex-1 min-h-0 overflow-y-auto mt-2">
                <div className="space-y-2">
                  {messages.map((msg, i) => (
                    <MessageRenderer key={i} message={msg} index={i} />
                  ))}
                </div>
              </TabsContent>
              <TabsContent value="json" className="flex-1 min-h-0 overflow-y-auto mt-2">
                <div className="space-y-2">
                  {messages.map((msg, i) => (
                    <div key={i} className="rounded-lg border border-border overflow-hidden">
                      <div className="flex items-center gap-2 px-3 py-2 bg-muted/30">
                        <span className="text-xs font-mono text-muted-foreground w-6">{i + 1}</span>
                        <RoleIcon role={msg.role?.toLowerCase() ?? 'unknown'} />
                        <span className="text-xs font-semibold uppercase tracking-wider">{msg.role}</span>
                        <span className="text-[10px] text-muted-foreground font-mono ml-auto">{msg.$type}</span>
                      </div>
                      <div className="p-3">
                        <JsonViewer data={msg} collapsed={2} className="text-xs" />
                      </div>
                    </div>
                  ))}
                </div>
              </TabsContent>
            </Tabs>
          ) : null}
        </CardContent>
      </Card>
    </div>
  )
}
