import { useState, useEffect } from 'react'
import { Plus, Trash2, Eye, EyeOff, KeyRound, Loader2, Sparkles, Tag, ArrowLeft } from 'lucide-react'
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
  Badge,
} from '@donkeywork/ui'
import {
  a2aServers,
  credentials,
  type A2aServerDetails,
  type CreateA2aServerRequest,
  type UpdateA2aServerRequest,
  type CredentialSummary,
  type A2aAgentCard,
  type A2aSecurityScheme,
} from '@donkeywork/api-client'

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

type WizardStep = 'basic' | 'configure'

interface A2aServerDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaved?: () => void
  editingServer?: A2aServerDetails | null
}

interface HeaderEntry {
  headerName: string
  headerValue: string
  isCredentialRef: boolean
  credentialId: string
  credentialFieldType: string
}

function getApiKeyHeaderSchemes(card: A2aAgentCard): { key: string; scheme: A2aSecurityScheme }[] {
  if (!card.securitySchemes) return []
  return Object.entries(card.securitySchemes)
    .filter(([, scheme]) => scheme.type === 'apiKey' && scheme.in === 'header')
    .map(([key, scheme]) => ({ key, scheme }))
}

export function A2aServerDialog({
  open,
  onOpenChange,
  onSaved,
  editingServer,
}: A2aServerDialogProps) {
  const [step, setStep] = useState<WizardStep>('basic')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [address, setAddress] = useState('')
  const [isEnabled, setIsEnabled] = useState(true)
  const [connectToNavi, setConnectToNavi] = useState(false)
  const [publishToMcp, setPublishToMcp] = useState(false)
  const [headers, setHeaders] = useState<HeaderEntry[]>([])
  const [revealedHeaders, setRevealedHeaders] = useState<Set<number>>(new Set())
  const [availableCredentials, setAvailableCredentials] = useState<CredentialSummary[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDiscovering, setIsDiscovering] = useState(false)
  const [discoveredCard, setDiscoveredCard] = useState<A2aAgentCard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [discoverError, setDiscoverError] = useState<string | null>(null)

  const isEditing = !!editingServer

  useEffect(() => {
    if (open) {
      credentials.list().then(setAvailableCredentials).catch(() => {})
    }
  }, [open])

  useEffect(() => {
    if (open) {
      if (editingServer) {
        setName(editingServer.name)
        setDescription(editingServer.description || '')
        setAddress(editingServer.address)
        setIsEnabled(editingServer.isEnabled)
        setConnectToNavi(editingServer.connectToNavi)
        setPublishToMcp(editingServer.publishToMcp ?? false)
        setHeaders(
          (editingServer.headerConfigurations || []).map((h) => ({
            headerName: h.headerName,
            headerValue: h.headerValue || '',
            isCredentialRef: h.isCredentialReference,
            credentialId: h.credentialId || '',
            credentialFieldType: h.credentialFieldType || '',
          }))
        )
      } else {
        resetForm()
      }
    }
  }, [open, editingServer])

  const resetForm = () => {
    setStep('basic')
    setName('')
    setDescription('')
    setAddress('')
    setIsEnabled(true)
    setConnectToNavi(false)
    setPublishToMcp(false)
    setHeaders([])
    setRevealedHeaders(new Set())
    setError(null)
    setDiscoverError(null)
    setDiscoveredCard(null)
    setIsDiscovering(false)
  }

  const handleDiscover = async () => {
    setDiscoverError(null)
    setIsDiscovering(true)
    try {
      const result = await a2aServers.discover(address)
      if (result.success && result.agentCard) {
        setDiscoveredCard(result.agentCard)
        seedHeadersFromCard(result.agentCard)
        setStep('configure')
      } else {
        setDiscoverError(result.error || 'Could not fetch agent card from this address')
      }
    } catch (err) {
      setDiscoverError(err instanceof Error ? err.message : 'Failed to discover agent')
    } finally {
      setIsDiscovering(false)
    }
  }

  const seedHeadersFromCard = (card: A2aAgentCard) => {
    const apiKeySchemes = getApiKeyHeaderSchemes(card)
    if (apiKeySchemes.length === 0) return

    setHeaders((prev) => {
      const existingNames = new Set(prev.map((h) => h.headerName.toLowerCase()))
      const newHeaders = apiKeySchemes
        .filter((s) => s.scheme.name && !existingNames.has(s.scheme.name.toLowerCase()))
        .map((s) => ({
          headerName: s.scheme.name!,
          headerValue: '',
          isCredentialRef: false,
          credentialId: '',
          credentialFieldType: '',
        }))
      return [...prev, ...newHeaders]
    })
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const headerConfigurations = headers.length > 0
        ? headers.map((h) =>
            h.isCredentialRef
              ? { headerName: h.headerName, credentialId: h.credentialId, credentialFieldType: h.credentialFieldType }
              : { headerName: h.headerName, headerValue: h.headerValue }
          )
        : undefined

      if (isEditing) {
        const updateRequest: UpdateA2aServerRequest = {
          name,
          description: description || undefined,
          address,
          isEnabled,
          connectToNavi,
          publishToMcp,
          headerConfigurations,
        }
        await a2aServers.update(editingServer!.id, updateRequest)
      } else {
        const createRequest: CreateA2aServerRequest = {
          name,
          description: description || undefined,
          address,
          isEnabled,
          connectToNavi,
          publishToMcp,
          headerConfigurations,
        }
        await a2aServers.create(createRequest)
      }

      onSaved?.()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save A2A server')
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

  const renderBasicStep = () => (
    <div className="space-y-4 py-4">
      {discoverError && (
        <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
          {discoverError}
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="name">Name</Label>
        <Input
          id="name"
          placeholder="My A2A Server"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          disabled={isDiscovering}
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
          disabled={isDiscovering}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="address">Address</Label>
        <Input
          id="address"
          type="url"
          placeholder="https://agent.example.com"
          value={address}
          onChange={(e) => setAddress(e.target.value)}
          required
          disabled={isDiscovering}
        />
        <p className="text-xs text-muted-foreground">
          The base URL of the A2A-compatible agent (e.g., http://host:8420)
        </p>
      </div>
    </div>
  )

  const renderAgentCardDisplay = () => {
    if (!discoveredCard) return null
    return (
      <div className="space-y-3">
        <div className="grid grid-cols-2 gap-3 text-sm">
          <div>
            <span className="text-muted-foreground">Agent</span>
            <p className="font-medium">{discoveredCard.name}</p>
          </div>
          <div>
            <span className="text-muted-foreground">Version</span>
            <p className="font-medium">{discoveredCard.version}</p>
          </div>
        </div>

        {discoveredCard.description && (
          <div className="text-sm">
            <span className="text-muted-foreground">Description</span>
            <p className="mt-0.5">{discoveredCard.description}</p>
          </div>
        )}

        {discoveredCard.capabilities && (
          <div className="flex gap-2">
            {discoveredCard.capabilities.streaming && (
              <Badge variant="secondary">Streaming</Badge>
            )}
            {discoveredCard.capabilities.pushNotifications && (
              <Badge variant="secondary">Push Notifications</Badge>
            )}
          </div>
        )}

        {discoveredCard.defaultInputModes && discoveredCard.defaultInputModes.length > 0 && (
          <div className="text-sm">
            <span className="text-muted-foreground">Input Modes: </span>
            <span>{discoveredCard.defaultInputModes.join(', ')}</span>
          </div>
        )}

        {discoveredCard.defaultOutputModes && discoveredCard.defaultOutputModes.length > 0 && (
          <div className="text-sm">
            <span className="text-muted-foreground">Output Modes: </span>
            <span>{discoveredCard.defaultOutputModes.join(', ')}</span>
          </div>
        )}

        {discoveredCard.skills.length > 0 && (
          <>
            <Separator />
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Sparkles className="h-4 w-4 text-muted-foreground" />
                <h3 className="text-sm font-medium">
                  Skills
                  <Badge variant="secondary" className="ml-2">{discoveredCard.skills.length}</Badge>
                </h3>
              </div>
              <div className="space-y-2 max-h-[200px] overflow-y-auto">
                {discoveredCard.skills.map((skill) => (
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
      </div>
    )
  }

  const renderConfigureStep = () => (
    <div className="space-y-6 py-4">
      {error && (
        <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
          {error}
        </div>
      )}

      {renderAgentCardDisplay()}

      <Separator />

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-medium">HTTP Headers</h3>
          <Button type="button" variant="ghost" size="sm" onClick={addHeader}>
            <Plus className="h-4 w-4 mr-1" />
            Add Header
          </Button>
        </div>

        {headers.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No authentication headers required. Add custom headers if needed.
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

      <Separator />

      <div className="space-y-4">
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

        <div className="flex items-center justify-between">
          <div className="space-y-0.5">
            <Label htmlFor="publishToMcp">Publish to MCP</Label>
            <p className="text-xs text-muted-foreground">
              Expose this agent as a tool on the DonkeyWork MCP server
            </p>
          </div>
          <Switch
            id="publishToMcp"
            checked={publishToMcp}
            onCheckedChange={setPublishToMcp}
          />
        </div>
      </div>
    </div>
  )

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Edit A2A Server' : 'Add A2A Server'}
          </DialogTitle>
          <DialogDescription>
            {step === 'basic'
              ? 'Enter the server details and discover the remote agent'
              : 'Review the agent card and configure authentication'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          {step === 'basic' && renderBasicStep()}
          {step === 'configure' && renderConfigureStep()}

          <DialogFooter>
            {step === 'basic' && (
              <>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => handleOpenChange(false)}
                  disabled={isDiscovering}
                >
                  Cancel
                </Button>
                <Button
                  type="button"
                  onClick={handleDiscover}
                  disabled={isDiscovering || !name.trim() || !address.trim()}
                >
                  {isDiscovering ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                      Discovering...
                    </>
                  ) : (
                    'Next'
                  )}
                </Button>
              </>
            )}
            {step === 'configure' && (
              <>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setStep('basic')}
                  disabled={isSubmitting}
                  className="mr-auto"
                >
                  <ArrowLeft className="h-4 w-4 mr-1.5" />
                  Back
                </Button>
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
              </>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
