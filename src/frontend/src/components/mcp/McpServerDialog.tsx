import { useState, useEffect } from 'react'
import { Plus, Trash2, Terminal, Globe } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Separator } from '@/components/ui/separator'
import {
  mcpServers,
  type McpServerDetails,
  type McpTransportType,
  type McpHttpTransportMode,
  type McpHttpAuthType,
  type CreateMcpServerRequest,
  type UpdateMcpServerRequest,
} from '@/lib/api'

interface McpServerDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved?: () => void
  editingServer?: McpServerDetails | null
}

interface KeyValuePair {
  key: string
  value: string
}

interface HeaderConfig {
  headerName: string
  headerValue: string
}

export function McpServerDialog({
  open,
  onOpenChange,
  onSaved,
  editingServer,
}: McpServerDialogProps) {
  // Basic fields
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [transportType, setTransportType] = useState<McpTransportType>('Stdio')
  const [isEnabled, setIsEnabled] = useState(true)

  // Stdio fields
  const [command, setCommand] = useState('')
  const [args, setArgs] = useState('')
  const [envVars, setEnvVars] = useState<KeyValuePair[]>([])
  const [preExecScripts, setPreExecScripts] = useState('')
  const [workingDirectory, setWorkingDirectory] = useState('')

  // HTTP fields
  const [endpoint, setEndpoint] = useState('')
  const [httpTransportMode, setHttpTransportMode] = useState<McpHttpTransportMode>('AutoDetect')
  const [authType, setAuthType] = useState<McpHttpAuthType>('None')

  // OAuth fields
  const [clientId, setClientId] = useState('')
  const [clientSecret, setClientSecret] = useState('')
  const [redirectUri, setRedirectUri] = useState('')
  const [scopes, setScopes] = useState('')
  const [authorizationEndpoint, setAuthorizationEndpoint] = useState('')
  const [tokenEndpoint, setTokenEndpoint] = useState('')

  // Header auth fields
  const [headers, setHeaders] = useState<HeaderConfig[]>([])

  // Form state
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const isEditing = !!editingServer

  // Reset form when dialog opens/closes or editing server changes
  useEffect(() => {
    if (open) {
      if (editingServer) {
        // Populate form with existing data
        setName(editingServer.name)
        setDescription(editingServer.description || '')
        setTransportType(editingServer.transportType)
        setIsEnabled(editingServer.isEnabled)

        if (editingServer.stdioConfiguration) {
          const stdio = editingServer.stdioConfiguration
          setCommand(stdio.command)
          setArgs(stdio.arguments?.join(' ') || '')
          setEnvVars(
            Object.entries(stdio.environmentVariables || {}).map(([key, value]) => ({
              key,
              value,
            }))
          )
          setPreExecScripts(stdio.preExecScripts?.join('\n') || '')
          setWorkingDirectory(stdio.workingDirectory || '')
        }

        if (editingServer.httpConfiguration) {
          const http = editingServer.httpConfiguration
          setEndpoint(http.endpoint)
          setHttpTransportMode(http.transportMode)
          setAuthType(http.authType)

          if (http.oauthConfiguration) {
            const oauth = http.oauthConfiguration
            setClientId(oauth.clientId)
            setClientSecret(oauth.clientSecret || '')
            setRedirectUri(oauth.redirectUri)
            setScopes(oauth.scopes?.join(' ') || '')
            setAuthorizationEndpoint(oauth.authorizationEndpoint)
            setTokenEndpoint(oauth.tokenEndpoint)
          }

          if (http.headerConfigurations) {
            setHeaders(
              http.headerConfigurations.map((h) => ({
                headerName: h.headerName,
                headerValue: h.headerValue,
              }))
            )
          }
        }
      } else {
        // Reset to defaults for new server
        resetForm()
      }
    }
  }, [open, editingServer])

  const resetForm = () => {
    setName('')
    setDescription('')
    setTransportType('Stdio')
    setIsEnabled(true)
    setCommand('')
    setArgs('')
    setEnvVars([])
    setPreExecScripts('')
    setWorkingDirectory('')
    setEndpoint('')
    setHttpTransportMode('AutoDetect')
    setAuthType('None')
    setClientId('')
    setClientSecret('')
    setRedirectUri('')
    setScopes('')
    setAuthorizationEndpoint('')
    setTokenEndpoint('')
    setHeaders([])
    setError(null)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      if (isEditing) {
        const updateRequest: UpdateMcpServerRequest = {
          name,
          description: description || undefined,
          isEnabled,
          ...(transportType === 'Stdio'
            ? {
                stdioConfiguration: {
                  command,
                  arguments: args ? args.split(/\s+/).filter(Boolean) : undefined,
                  environmentVariables:
                    envVars.length > 0
                      ? Object.fromEntries(envVars.map((ev) => [ev.key, ev.value]))
                      : undefined,
                  preExecScripts: preExecScripts
                    ? preExecScripts.split('\n').filter(Boolean)
                    : undefined,
                  workingDirectory: workingDirectory || undefined,
                },
              }
            : {
                httpConfiguration: {
                  endpoint,
                  transportMode: httpTransportMode,
                  authType,
                  ...(authType === 'OAuth'
                    ? {
                        oauthConfiguration: {
                          clientId,
                          clientSecret: clientSecret || undefined,
                          redirectUri,
                          scopes: scopes ? scopes.split(/\s+/).filter(Boolean) : undefined,
                          authorizationEndpoint,
                          tokenEndpoint,
                        },
                      }
                    : {}),
                  ...(authType === 'Header' && headers.length > 0
                    ? {
                        headerConfigurations: headers.map((h) => ({
                          headerName: h.headerName,
                          headerValue: h.headerValue,
                        })),
                      }
                    : {}),
                },
              }),
        }
        await mcpServers.update(editingServer!.id, updateRequest)
      } else {
        const createRequest: CreateMcpServerRequest = {
          name,
          description: description || undefined,
          transportType,
          isEnabled,
          ...(transportType === 'Stdio'
            ? {
                stdioConfiguration: {
                  command,
                  arguments: args ? args.split(/\s+/).filter(Boolean) : undefined,
                  environmentVariables:
                    envVars.length > 0
                      ? Object.fromEntries(envVars.map((ev) => [ev.key, ev.value]))
                      : undefined,
                  preExecScripts: preExecScripts
                    ? preExecScripts.split('\n').filter(Boolean)
                    : undefined,
                  workingDirectory: workingDirectory || undefined,
                },
              }
            : {
                httpConfiguration: {
                  endpoint,
                  transportMode: httpTransportMode,
                  authType,
                  ...(authType === 'OAuth'
                    ? {
                        oauthConfiguration: {
                          clientId,
                          clientSecret: clientSecret || undefined,
                          redirectUri,
                          scopes: scopes ? scopes.split(/\s+/).filter(Boolean) : undefined,
                          authorizationEndpoint,
                          tokenEndpoint,
                        },
                      }
                    : {}),
                  ...(authType === 'Header' && headers.length > 0
                    ? {
                        headerConfigurations: headers.map((h) => ({
                          headerName: h.headerName,
                          headerValue: h.headerValue,
                        })),
                      }
                    : {}),
                },
              }),
        }
        await mcpServers.create(createRequest)
      }

      onSaved?.()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save MCP server')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      resetForm()
    }
    onOpenChange(newOpen)
  }

  const addEnvVar = () => {
    setEnvVars([...envVars, { key: '', value: '' }])
  }

  const updateEnvVar = (index: number, field: 'key' | 'value', value: string) => {
    const updated = [...envVars]
    updated[index][field] = value
    setEnvVars(updated)
  }

  const removeEnvVar = (index: number) => {
    setEnvVars(envVars.filter((_, i) => i !== index))
  }

  const addHeader = () => {
    setHeaders([...headers, { headerName: '', headerValue: '' }])
  }

  const updateHeader = (index: number, field: 'headerName' | 'headerValue', value: string) => {
    const updated = [...headers]
    updated[index][field] = value
    setHeaders(updated)
  }

  const removeHeader = (index: number) => {
    setHeaders(headers.filter((_, i) => i !== index))
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit MCP Server' : 'Add MCP Server'}</DialogTitle>
          <DialogDescription>
            Configure an MCP server to extend your agents with external tools
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="space-y-6 py-4">
            {error && (
              <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
                {error}
              </div>
            )}

            {/* Basic Info */}
            <div className="space-y-4">
              <h3 className="text-sm font-medium">Basic Information</h3>

              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  placeholder="My MCP Server"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  placeholder="Optional description..."
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="transportType">Transport Type</Label>
                <Select
                  value={transportType}
                  onValueChange={(value) => setTransportType(value as McpTransportType)}
                  disabled={isEditing}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Stdio">
                      <div className="flex items-center gap-2">
                        <Terminal className="h-4 w-4" />
                        <span>Stdio (Local Process)</span>
                      </div>
                    </SelectItem>
                    <SelectItem value="Http">
                      <div className="flex items-center gap-2">
                        <Globe className="h-4 w-4" />
                        <span>HTTP (Remote Server)</span>
                      </div>
                    </SelectItem>
                  </SelectContent>
                </Select>
                {isEditing && (
                  <p className="text-xs text-muted-foreground">
                    Transport type cannot be changed after creation
                  </p>
                )}
              </div>

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="isEnabled">Enabled</Label>
                  <p className="text-xs text-muted-foreground">
                    Disabled servers won't be available to agents
                  </p>
                </div>
                <Switch
                  id="isEnabled"
                  checked={isEnabled}
                  onCheckedChange={setIsEnabled}
                />
              </div>
            </div>

            <Separator />

            {/* Stdio Configuration */}
            {transportType === 'Stdio' && (
              <div className="space-y-4">
                <h3 className="text-sm font-medium">Stdio Configuration</h3>

                <div className="space-y-2">
                  <Label htmlFor="command">Command</Label>
                  <Input
                    id="command"
                    placeholder="npx, python, node..."
                    value={command}
                    onChange={(e) => setCommand(e.target.value)}
                    required
                  />
                  <p className="text-xs text-muted-foreground">
                    The executable to run (e.g., npx, python, node)
                  </p>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="args">Arguments</Label>
                  <Input
                    id="args"
                    placeholder="-y @modelcontextprotocol/server-filesystem /path"
                    value={args}
                    onChange={(e) => setArgs(e.target.value)}
                  />
                  <p className="text-xs text-muted-foreground">
                    Space-separated arguments to pass to the command
                  </p>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="workingDirectory">Working Directory</Label>
                  <Input
                    id="workingDirectory"
                    placeholder="/home/user/project"
                    value={workingDirectory}
                    onChange={(e) => setWorkingDirectory(e.target.value)}
                  />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label>Environment Variables</Label>
                    <Button type="button" variant="ghost" size="sm" onClick={addEnvVar}>
                      <Plus className="h-4 w-4 mr-1" />
                      Add
                    </Button>
                  </div>
                  {envVars.length > 0 && (
                    <div className="space-y-2">
                      {envVars.map((ev, index) => (
                        <div key={index} className="flex items-center gap-2">
                          <Input
                            placeholder="KEY"
                            value={ev.key}
                            onChange={(e) => updateEnvVar(index, 'key', e.target.value)}
                            className="flex-1"
                          />
                          <Input
                            placeholder="value"
                            value={ev.value}
                            onChange={(e) => updateEnvVar(index, 'value', e.target.value)}
                            className="flex-1"
                            type="password"
                          />
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => removeEnvVar(index)}
                          >
                            <Trash2 className="h-4 w-4 text-red-500" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="preExecScripts">Pre-Execution Scripts</Label>
                  <Textarea
                    id="preExecScripts"
                    placeholder="pip install some-package&#10;export PATH=$PATH:/custom/bin"
                    value={preExecScripts}
                    onChange={(e) => setPreExecScripts(e.target.value)}
                    rows={3}
                  />
                  <p className="text-xs text-muted-foreground">
                    Shell commands to run before starting the MCP server (one per line)
                  </p>
                </div>
              </div>
            )}

            {/* HTTP Configuration */}
            {transportType === 'Http' && (
              <div className="space-y-4">
                <h3 className="text-sm font-medium">HTTP Configuration</h3>

                <div className="space-y-2">
                  <Label htmlFor="endpoint">Endpoint URL</Label>
                  <Input
                    id="endpoint"
                    placeholder="https://api.example.com/mcp"
                    value={endpoint}
                    onChange={(e) => setEndpoint(e.target.value)}
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="httpTransportMode">Transport Mode</Label>
                  <Select
                    value={httpTransportMode}
                    onValueChange={(value) => setHttpTransportMode(value as McpHttpTransportMode)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="AutoDetect">Auto-detect</SelectItem>
                      <SelectItem value="Sse">Server-Sent Events (SSE)</SelectItem>
                      <SelectItem value="StreamableHttp">Streamable HTTP</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="authType">Authentication</Label>
                  <Select
                    value={authType}
                    onValueChange={(value) => setAuthType(value as McpHttpAuthType)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="None">None</SelectItem>
                      <SelectItem value="OAuth">OAuth 2.0</SelectItem>
                      <SelectItem value="Header">API Key / Header</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* OAuth Configuration */}
                {authType === 'OAuth' && (
                  <div className="space-y-4 p-4 rounded-lg border border-border bg-muted/30">
                    <h4 className="text-sm font-medium">OAuth 2.0 Configuration</h4>

                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="clientId">Client ID</Label>
                        <Input
                          id="clientId"
                          value={clientId}
                          onChange={(e) => setClientId(e.target.value)}
                          required
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="clientSecret">Client Secret</Label>
                        <Input
                          id="clientSecret"
                          type="password"
                          value={clientSecret}
                          onChange={(e) => setClientSecret(e.target.value)}
                        />
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="redirectUri">Redirect URI</Label>
                      <Input
                        id="redirectUri"
                        placeholder="https://your-app.com/oauth/callback"
                        value={redirectUri}
                        onChange={(e) => setRedirectUri(e.target.value)}
                        required
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="scopes">Scopes</Label>
                      <Input
                        id="scopes"
                        placeholder="read write"
                        value={scopes}
                        onChange={(e) => setScopes(e.target.value)}
                      />
                      <p className="text-xs text-muted-foreground">Space-separated list of scopes</p>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="authorizationEndpoint">Authorization Endpoint</Label>
                        <Input
                          id="authorizationEndpoint"
                          placeholder="https://auth.example.com/authorize"
                          value={authorizationEndpoint}
                          onChange={(e) => setAuthorizationEndpoint(e.target.value)}
                          required
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="tokenEndpoint">Token Endpoint</Label>
                        <Input
                          id="tokenEndpoint"
                          placeholder="https://auth.example.com/token"
                          value={tokenEndpoint}
                          onChange={(e) => setTokenEndpoint(e.target.value)}
                          required
                        />
                      </div>
                    </div>
                  </div>
                )}

                {/* Header Configuration */}
                {authType === 'Header' && (
                  <div className="space-y-4 p-4 rounded-lg border border-border bg-muted/30">
                    <div className="flex items-center justify-between">
                      <h4 className="text-sm font-medium">HTTP Headers</h4>
                      <Button type="button" variant="ghost" size="sm" onClick={addHeader}>
                        <Plus className="h-4 w-4 mr-1" />
                        Add Header
                      </Button>
                    </div>

                    {headers.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        Add headers to include with each request (e.g., X-API-Key)
                      </p>
                    ) : (
                      <div className="space-y-2">
                        {headers.map((header, index) => (
                          <div key={index} className="flex items-center gap-2">
                            <Input
                              placeholder="Header-Name"
                              value={header.headerName}
                              onChange={(e) => updateHeader(index, 'headerName', e.target.value)}
                              className="flex-1"
                            />
                            <Input
                              placeholder="Header value"
                              value={header.headerValue}
                              onChange={(e) => updateHeader(index, 'headerValue', e.target.value)}
                              className="flex-1"
                              type="password"
                            />
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => removeHeader(index)}
                            >
                              <Trash2 className="h-4 w-4 text-red-500" />
                            </Button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Server'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
