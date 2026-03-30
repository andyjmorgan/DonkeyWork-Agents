import { useState, useEffect, useCallback } from 'react'
import { Plus, Trash2, Terminal, Globe, Eye, EyeOff, KeyRound, Zap } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Input,
  Label,
  Textarea,
  Switch,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
} from '@donkeywork/ui'
import {
  mcpServers,
  credentials,
  oauth,
  type McpServerDetails,
  type McpTransportType,
  type McpHttpTransportMode,
  type McpHttpAuthType,
  type CreateMcpServerRequest,
  type UpdateMcpServerRequest,
  type CredentialSummary,
  type OAuthToken,
  type CreateMcpEnvironmentVariableRequest,
  type CreateMcpHttpHeaderConfigurationRequest,
} from '@donkeywork/api-client'
import { McpServerTestDialog } from '@/components/mcp/McpServerTestDialog'

const CREDENTIAL_FIELD_TYPES = [
  'ApiKey',
  'Username',
  'Password',
  'ClientId',
  'ClientSecret',
  'AccessToken',
  'RefreshToken',
  'WebhookSecret',
  'Custom',
] as const

interface McpServerDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved?: () => void
  editingServer?: McpServerDetails | null
}

interface EnvVarEntry {
  key: string
  value: string
  isCredentialRef: boolean
  credentialId: string
  credentialFieldType: string
}

interface HeaderEntry {
  headerName: string
  headerValue: string
  isCredentialRef: boolean
  credentialId: string
  credentialFieldType: string
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
  const [connectToNavi, setConnectToNavi] = useState(false)

  // Stdio fields
  const [command, setCommand] = useState('')
  const [args, setArgs] = useState('')
  const [envVars, setEnvVars] = useState<EnvVarEntry[]>([])
  const [preExecScripts, setPreExecScripts] = useState('')
  const [workingDirectory, setWorkingDirectory] = useState('')

  // HTTP fields
  const [endpoint, setEndpoint] = useState('')
  const [httpTransportMode, setHttpTransportMode] = useState<McpHttpTransportMode>('AutoDetect')
  const [authType, setAuthType] = useState<McpHttpAuthType>('None')

  // OAuth connected account
  const [oauthTokens, setOauthTokens] = useState<OAuthToken[]>([])
  const [selectedOAuthTokenId, setSelectedOAuthTokenId] = useState('')

  // Header auth fields
  const [headers, setHeaders] = useState<HeaderEntry[]>([])

  // Visibility toggles
  const [revealedHeaders, setRevealedHeaders] = useState<Set<number>>(new Set())

  // Credentials list for picker
  const [availableCredentials, setAvailableCredentials] = useState<CredentialSummary[]>([])

  // Form state
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showTestDialog, setShowTestDialog] = useState(false)

  const isEditing = !!editingServer

  useEffect(() => {
    if (open) {
      credentials.list().then(setAvailableCredentials).catch(() => {})
      oauth.listTokens().then(setOauthTokens).catch(() => {})
    }
  }, [open])

  useEffect(() => {
    if (open) {
      if (editingServer) {
        // Populate form with existing data
        setName(editingServer.name)
        setDescription(editingServer.description || '')
        setTransportType(editingServer.transportType)
        setIsEnabled(editingServer.isEnabled)
        setConnectToNavi(editingServer.connectToNavi ?? false)

        if (editingServer.stdioConfiguration) {
          const stdio = editingServer.stdioConfiguration
          setCommand(stdio.command)
          setArgs(stdio.arguments?.join(' ') || '')
          setEnvVars(
            (stdio.environmentVariables || []).map((ev) => ({
              key: ev.name,
              value: ev.value || '',
              isCredentialRef: ev.isCredentialReference,
              credentialId: ev.credentialId || '',
              credentialFieldType: ev.credentialFieldType || '',
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

          if (http.oauthTokenId) {
            setSelectedOAuthTokenId(http.oauthTokenId)
          }

          if (http.headerConfigurations) {
            setHeaders(
              http.headerConfigurations.map((h) => ({
                headerName: h.headerName,
                headerValue: h.headerValue || '',
                isCredentialRef: h.isCredentialReference,
                credentialId: h.credentialId || '',
                credentialFieldType: h.credentialFieldType || '',
              }))
            )
          }
        }
      } else {
        resetForm()
      }
    }
  }, [open, editingServer])

  const resetForm = () => {
    setName('')
    setDescription('')
    setTransportType('Stdio')
    setIsEnabled(true)
    setConnectToNavi(false)
    setCommand('')
    setArgs('')
    setEnvVars([])
    setPreExecScripts('')
    setWorkingDirectory('')
    setEndpoint('')
    setHttpTransportMode('AutoDetect')
    setAuthType('None')
    setSelectedOAuthTokenId('')
    setHeaders([])
    setRevealedHeaders(new Set())
    setError(null)
    setShowTestDialog(false)
  }

  const buildEnvVarsPayload = useCallback((): CreateMcpEnvironmentVariableRequest[] | undefined => {
    if (envVars.length === 0) return undefined
    return envVars.map((ev) =>
      ev.isCredentialRef
        ? { name: ev.key, credentialId: ev.credentialId, credentialFieldType: ev.credentialFieldType }
        : { name: ev.key, value: ev.value }
    )
  }, [envVars])

  const buildHeadersPayload = useCallback((): CreateMcpHttpHeaderConfigurationRequest[] | undefined => {
    if (headers.length === 0) return undefined
    return headers.map((h) =>
      h.isCredentialRef
        ? { headerName: h.headerName, credentialId: h.credentialId, credentialFieldType: h.credentialFieldType }
        : { headerName: h.headerName, headerValue: h.headerValue }
    )
  }, [headers])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const stdioConfig = transportType === 'Stdio' ? {
        stdioConfiguration: {
          command,
          arguments: args ? args.split(/\s+/).filter(Boolean) : undefined,
          environmentVariables: buildEnvVarsPayload(),
          preExecScripts: preExecScripts ? preExecScripts.split('\n').filter(Boolean) : undefined,
          workingDirectory: workingDirectory || undefined,
        },
      } : {}

      const httpConfig = transportType === 'Http' ? {
        httpConfiguration: {
          endpoint,
          transportMode: httpTransportMode,
          authType,
          ...(authType === 'OAuth'
            ? { oauthTokenId: selectedOAuthTokenId || undefined }
            : {}),
          ...(authType === 'Header'
            ? { headerConfigurations: buildHeadersPayload() }
            : {}),
        },
      } : {}

      if (isEditing) {
        const updateRequest: UpdateMcpServerRequest = {
          name,
          description: description || undefined,
          transportType,
          isEnabled,
          connectToNavi,
          ...stdioConfig,
          ...httpConfig,
        }
        await mcpServers.update(editingServer!.id, updateRequest)
      } else {
        const createRequest: CreateMcpServerRequest = {
          name,
          description: description || undefined,
          transportType,
          isEnabled,
          connectToNavi,
          ...stdioConfig,
          ...httpConfig,
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
    setEnvVars([...envVars, { key: '', value: '', isCredentialRef: false, credentialId: '', credentialFieldType: '' }])
  }

  const updateEnvVar = (index: number, field: keyof EnvVarEntry, value: string | boolean) => {
    const updated = [...envVars]
    if (field === 'isCredentialRef') {
      updated[index] = { ...updated[index], isCredentialRef: value as boolean, value: '', credentialId: '', credentialFieldType: '' }
    } else {
      updated[index] = { ...updated[index], [field]: value }
    }
    setEnvVars(updated)
  }

  const removeEnvVar = (index: number) => {
    setEnvVars(envVars.filter((_, i) => i !== index))
  }

  const addHeader = () => {
    setHeaders([...headers, { headerName: '', headerValue: '', isCredentialRef: false, credentialId: '', credentialFieldType: '' }])
  }

  const updateHeader = (index: number, field: keyof HeaderEntry, value: string | boolean) => {
    const updated = [...headers]
    if (field === 'isCredentialRef') {
      updated[index] = { ...updated[index], isCredentialRef: value as boolean, headerValue: '', credentialId: '', credentialFieldType: '' }
    } else {
      updated[index] = { ...updated[index], [field]: value }
    }
    setHeaders(updated)
  }

  const removeHeader = (index: number) => {
    setHeaders(headers.filter((_, i) => i !== index))
    setRevealedHeaders((prev) => {
      const next = new Set<number>()
      for (const i of prev) {
        if (i < index) next.add(i)
        else if (i > index) next.add(i - 1)
      }
      return next
    })
  }

  const toggleHeaderReveal = (index: number) => {
    setRevealedHeaders((prev) => {
      const next = new Set(prev)
      if (next.has(index)) next.delete(index)
      else next.add(index)
      return next
    })
  }

  const renderCredentialPicker = (
    credentialId: string,
    credentialFieldType: string,
    onCredentialChange: (credentialId: string) => void,
    onFieldTypeChange: (fieldType: string) => void,
  ) => (
    <div className="flex items-center gap-2 flex-1">
      <Select value={credentialId} onValueChange={onCredentialChange}>
        <SelectTrigger className="flex-1">
          <SelectValue placeholder="Select credential..." />
        </SelectTrigger>
        <SelectContent>
          {availableCredentials.map((cred) => (
            <SelectItem key={cred.id} value={cred.id}>
              {cred.name} ({cred.provider})
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Select value={credentialFieldType} onValueChange={onFieldTypeChange}>
        <SelectTrigger className="w-[140px]">
          <SelectValue placeholder="Field..." />
        </SelectTrigger>
        <SelectContent>
          {CREDENTIAL_FIELD_TYPES.map((ft) => (
            <SelectItem key={ft} value={ft}>
              {ft}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )

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

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="connectToNavi">Connect to Navi</Label>
                  <p className="text-xs text-muted-foreground">
                    Automatically attach this server to Navi conversations
                  </p>
                </div>
                <Switch
                  id="connectToNavi"
                  checked={connectToNavi}
                  onCheckedChange={setConnectToNavi}
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
                        <div key={index} className="space-y-2">
                          <div className="flex items-center gap-2">
                            <Input
                              placeholder="KEY"
                              value={ev.key}
                              onChange={(e) => updateEnvVar(index, 'key', e.target.value)}
                              className="w-[160px]"
                            />
                            {ev.isCredentialRef ? (
                              renderCredentialPicker(
                                ev.credentialId,
                                ev.credentialFieldType,
                                (id) => updateEnvVar(index, 'credentialId', id),
                                (ft) => updateEnvVar(index, 'credentialFieldType', ft),
                              )
                            ) : (
                              <Input
                                placeholder="value"
                                value={ev.value}
                                onChange={(e) => updateEnvVar(index, 'value', e.target.value)}
                                className="flex-1"
                                type="password"
                              />
                            )}
                            <Button
                              type="button"
                              variant={ev.isCredentialRef ? 'default' : 'ghost'}
                              size="sm"
                              onClick={() => updateEnvVar(index, 'isCredentialRef', !ev.isCredentialRef)}
                              title={ev.isCredentialRef ? 'Switch to literal value' : 'Use credential reference'}
                              tabIndex={-1}
                            >
                              <KeyRound className="h-4 w-4" />
                            </Button>
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              onClick={() => removeEnvVar(index)}
                            >
                              <Trash2 className="h-4 w-4 text-red-500" />
                            </Button>
                          </div>
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
                      <SelectItem value="OAuth">Connected Account</SelectItem>
                      <SelectItem value="Header">API Key / Header</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* OAuth Connected Account */}
                {authType === 'OAuth' && (
                  <div className="space-y-4 p-4 rounded-lg border border-border bg-muted/30">
                    <h4 className="text-sm font-medium">Connected Account</h4>
                    <p className="text-sm text-muted-foreground">
                      Select a connected account to authenticate with. The access token will be sent as a Bearer token in the Authorization header.
                    </p>
                    {oauthTokens.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        No connected accounts found. Connect an account in Settings first.
                      </p>
                    ) : (
                      <Select value={selectedOAuthTokenId} onValueChange={setSelectedOAuthTokenId}>
                        <SelectTrigger>
                          <SelectValue placeholder="Select connected account..." />
                        </SelectTrigger>
                        <SelectContent>
                          {oauthTokens.map((token) => (
                            <SelectItem key={token.id} value={token.id}>
                              {token.email} ({token.provider})
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
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
                              className="w-[160px]"
                            />
                            {header.isCredentialRef ? (
                              renderCredentialPicker(
                                header.credentialId,
                                header.credentialFieldType,
                                (id) => updateHeader(index, 'credentialId', id),
                                (ft) => updateHeader(index, 'credentialFieldType', ft),
                              )
                            ) : (
                              <Input
                                placeholder="Header value"
                                value={header.headerValue}
                                onChange={(e) => updateHeader(index, 'headerValue', e.target.value)}
                                className="flex-1"
                                type={revealedHeaders.has(index) ? 'text' : 'password'}
                              />
                            )}
                            {!header.isCredentialRef && (
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                onClick={() => toggleHeaderReveal(index)}
                                tabIndex={-1}
                              >
                                {revealedHeaders.has(index) ? (
                                  <EyeOff className="h-4 w-4" />
                                ) : (
                                  <Eye className="h-4 w-4" />
                                )}
                              </Button>
                            )}
                            <Button
                              type="button"
                              variant={header.isCredentialRef ? 'default' : 'ghost'}
                              size="sm"
                              onClick={() => updateHeader(index, 'isCredentialRef', !header.isCredentialRef)}
                              title={header.isCredentialRef ? 'Switch to literal value' : 'Use credential reference'}
                              tabIndex={-1}
                            >
                              <KeyRound className="h-4 w-4" />
                            </Button>
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
            {isEditing && transportType === 'Http' && (
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowTestDialog(true)}
                disabled={isSubmitting}
                className="mr-auto"
              >
                <Zap className="h-4 w-4 mr-1.5" />
                Test Connection
              </Button>
            )}
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

      {isEditing && editingServer && showTestDialog && (
        <McpServerTestDialog
          open={showTestDialog}
          onOpenChange={setShowTestDialog}
          serverId={editingServer.id}
          serverName={editingServer.name}
        />
      )}
    </Dialog>
  )
}
