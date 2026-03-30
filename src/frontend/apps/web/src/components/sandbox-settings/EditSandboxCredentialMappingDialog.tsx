import { useState, useEffect } from 'react'
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import {
  sandboxCredentialMappings,
  credentials,
  oauth,
  type SandboxCredentialMapping,
  type CredentialFieldType,
  type CredentialSummary,
  type OAuthToken,
} from '@donkeywork/api-client'

interface EditSandboxCredentialMappingDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  mapping: SandboxCredentialMapping
  onUpdated?: () => void
}

const fieldTypeLabels: Record<CredentialFieldType, string> = {
  ApiKey: 'API Key',
  AccessToken: 'Access Token',
  RefreshToken: 'Refresh Token',
  Username: 'Username',
  Password: 'Password',
  ClientId: 'Client ID',
  ClientSecret: 'Client Secret',
  WebhookSecret: 'Webhook Secret',
  Custom: 'Custom',
}

export function EditSandboxCredentialMappingDialog({
  open,
  onOpenChange,
  mapping,
  onUpdated,
}: EditSandboxCredentialMappingDialogProps) {
  const [headerName, setHeaderName] = useState(mapping.headerName)
  const [headerValuePrefix, setHeaderValuePrefix] = useState(mapping.headerValuePrefix ?? '')
  const [credentialType, setCredentialType] = useState(mapping.credentialType)
  const [credentialId, setCredentialId] = useState(mapping.credentialId)
  const [credentialFieldType, setCredentialFieldType] = useState<CredentialFieldType>(mapping.credentialFieldType)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [availableFields, setAvailableFields] = useState<CredentialFieldType[]>([])
  const [loadingFields, setLoadingFields] = useState(false)

  const [apiKeyCredentials, setApiKeyCredentials] = useState<CredentialSummary[]>([])
  const [oauthTokens, setOauthTokens] = useState<OAuthToken[]>([])
  const [loadingCredentials, setLoadingCredentials] = useState(false)

  useEffect(() => {
    setHeaderName(mapping.headerName)
    setHeaderValuePrefix(mapping.headerValuePrefix ?? '')
    setCredentialType(mapping.credentialType)
    setCredentialId(mapping.credentialId)
    setCredentialFieldType(mapping.credentialFieldType)
    setError(null)
  }, [mapping])

  useEffect(() => {
    if (!open) return
    setLoadingCredentials(true)
    const load = async () => {
      try {
        const [creds, tokens] = await Promise.all([
          credentials.list(),
          oauth.listTokens(),
        ])
        setApiKeyCredentials(creds)
        setOauthTokens(tokens)
      } catch (err) {
        console.error('Failed to load credentials:', err)
      } finally {
        setLoadingCredentials(false)
      }
    }
    load()
  }, [open])

  const [hasChangedType, setHasChangedType] = useState(false)
  useEffect(() => {
    if (hasChangedType) {
      setCredentialId('')
      setAvailableFields([])
    }
  }, [credentialType, hasChangedType])

  useEffect(() => {
    if (!credentialId || !credentialType) {
      setAvailableFields([])
      return
    }
    setLoadingFields(true)
    sandboxCredentialMappings.getCredentialFields(credentialId, credentialType)
      .then(res => {
        setAvailableFields(res.fields)
        if (res.fields.length === 1) {
          setCredentialFieldType(res.fields[0])
        }
      })
      .catch(err => console.error('Failed to load credential fields:', err))
      .finally(() => setLoadingFields(false))
  }, [credentialId, credentialType])

  const credentialOptions = credentialType === 'ExternalApiKey'
    ? apiKeyCredentials.map(c => ({ id: c.id, label: `${c.name} (${c.provider})` }))
    : oauthTokens.map(t => ({ id: t.id, label: `${t.email} (${t.provider})` }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await sandboxCredentialMappings.update(mapping.id, {
        headerName,
        headerValuePrefix: headerValuePrefix || undefined,
        credentialId,
        credentialType,
        credentialFieldType,
      })

      onUpdated?.()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update mapping')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      setError(null)
      setHasChangedType(false)
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Edit Sandbox Credential Mapping</DialogTitle>
          <DialogDescription>
            Update the credential mapping for <span className="font-medium">{mapping.baseDomain}</span>
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="space-y-4 py-4">
            {error && (
              <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label>Base Domain</Label>
              <div className="rounded-md border border-input bg-muted/50 px-3 py-2 text-sm">
                {mapping.baseDomain}
              </div>
              <p className="text-xs text-muted-foreground">
                Domain cannot be changed. Delete and recreate to use a different domain.
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-headerName">Header Name</Label>
              <Input
                id="edit-headerName"
                placeholder="Authorization"
                value={headerName}
                onChange={(e) => setHeaderName(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-headerValuePrefix">Header Value Prefix</Label>
              <Input
                id="edit-headerValuePrefix"
                placeholder="Bearer "
                value={headerValuePrefix}
                onChange={(e) => setHeaderValuePrefix(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Optional prefix prepended to the credential value (e.g. &quot;Bearer &quot; with trailing space)
              </p>
            </div>

            <div className="space-y-2">
              <Label>Credential Type</Label>
              <Select
                value={credentialType}
                onValueChange={(v) => {
                  setCredentialType(v)
                  setHasChangedType(true)
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ExternalApiKey">External API Key</SelectItem>
                  <SelectItem value="OAuthToken">OAuth Token</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Credential</Label>
              <Select value={credentialId} onValueChange={setCredentialId}>
                <SelectTrigger>
                  <SelectValue placeholder={loadingCredentials ? 'Loading...' : 'Select a credential'} />
                </SelectTrigger>
                <SelectContent>
                  {credentialOptions.map(opt => (
                    <SelectItem key={opt.id} value={opt.id}>
                      {opt.label}
                    </SelectItem>
                  ))}
                  {credentialOptions.length === 0 && !loadingCredentials && (
                    <div className="px-2 py-1.5 text-sm text-muted-foreground">
                      No {credentialType === 'ExternalApiKey' ? 'API key credentials' : 'connected accounts'} found
                    </div>
                  )}
                </SelectContent>
              </Select>
            </div>

            {availableFields.length > 1 && (
              <div className="space-y-2">
                <Label>Credential Field</Label>
                <Select value={credentialFieldType} onValueChange={(v) => setCredentialFieldType(v as CredentialFieldType)}>
                  <SelectTrigger>
                    <SelectValue placeholder={loadingFields ? 'Loading...' : 'Select a field'} />
                  </SelectTrigger>
                  <SelectContent>
                    {availableFields.map(ft => (
                      <SelectItem key={ft} value={ft}>
                        {fieldTypeLabels[ft]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground">
                  Which field from the credential to use as the header value
                </p>
              </div>
            )}

            {availableFields.length === 1 && (
              <div className="space-y-2">
                <Label>Credential Field</Label>
                <div className="rounded-md border border-input bg-muted/50 px-3 py-2 text-sm">
                  {fieldTypeLabels[availableFields[0]]}
                </div>
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
            <Button type="submit" disabled={isSubmitting || !credentialId}>
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
