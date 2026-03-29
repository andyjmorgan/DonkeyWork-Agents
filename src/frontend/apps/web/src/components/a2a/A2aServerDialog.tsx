import { useState, useEffect } from 'react'
import { Plus, Trash2, Eye, EyeOff, KeyRound, Zap } from 'lucide-react'
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
  a2aServers,
  credentials,
  type A2aServerDetails,
  type CreateA2aServerRequest,
  type UpdateA2aServerRequest,
  type CredentialSummary,
} from '@donkeywork/api-client'
import { A2aServerTestDialog } from '@/components/a2a/A2aServerTestDialog'

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

export function A2aServerDialog({
  open,
  onOpenChange,
  onSaved,
  editingServer,
}: A2aServerDialogProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [address, setAddress] = useState('')
  const [isEnabled, setIsEnabled] = useState(true)
  const [connectToNavi, setConnectToNavi] = useState(false)
  const [headers, setHeaders] = useState<HeaderEntry[]>([])
  const [revealedHeaders, setRevealedHeaders] = useState<Set<number>>(new Set())
  const [availableCredentials, setAvailableCredentials] = useState<CredentialSummary[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [showTestDialog, setShowTestDialog] = useState(false)

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
    setName('')
    setDescription('')
    setAddress('')
    setIsEnabled(true)
    setConnectToNavi(false)
    setHeaders([])
    setRevealedHeaders(new Set())
    setError(null)
    setShowTestDialog(false)
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

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Edit A2A Server' : 'Add A2A Server'}</DialogTitle>
          <DialogDescription>
            Configure an A2A server to connect your agents with external agents
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="space-y-6 py-4">
            {error && (
              <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
                {error}
              </div>
            )}

            <div className="space-y-4">
              <h3 className="text-sm font-medium">Basic Information</h3>

              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  placeholder="My A2A Server"
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
                <Label htmlFor="address">Address</Label>
                <Input
                  id="address"
                  type="url"
                  placeholder="https://agent.example.com"
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  required
                />
                <p className="text-xs text-muted-foreground">
                  The base URL of the A2A-compatible agent
                </p>
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
                  Add headers to include with each request (e.g., Authorization, X-API-Key)
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
          </div>

          <DialogFooter>
            {isEditing && (
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
        <A2aServerTestDialog
          open={showTestDialog}
          onOpenChange={setShowTestDialog}
          serverId={editingServer.id}
          serverName={editingServer.name}
        />
      )}
    </Dialog>
  )
}
