import { useState, useEffect } from 'react'
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  sandboxCredentialMappings,
  credentials,
  oauth,
  type CredentialFieldType,
  type CredentialSummary,
  type OAuthToken,
} from '@/lib/api'

interface CreateSandboxCredentialMappingDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated?: () => void
}

const CREDENTIAL_FIELD_TYPES: CredentialFieldType[] = [
  'ApiKey',
  'AccessToken',
  'RefreshToken',
  'Username',
  'Password',
  'ClientId',
  'ClientSecret',
  'WebhookSecret',
  'Custom',
]

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

export function CreateSandboxCredentialMappingDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateSandboxCredentialMappingDialogProps) {
  const [baseDomain, setBaseDomain] = useState('')
  const [headerName, setHeaderName] = useState('')
  const [headerValuePrefix, setHeaderValuePrefix] = useState('')
  const [credentialType, setCredentialType] = useState<string>('ExternalApiKey')
  const [credentialId, setCredentialId] = useState('')
  const [credentialFieldType, setCredentialFieldType] = useState<CredentialFieldType>('ApiKey')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [apiKeyCredentials, setApiKeyCredentials] = useState<CredentialSummary[]>([])
  const [oauthTokens, setOauthTokens] = useState<OAuthToken[]>([])
  const [loadingCredentials, setLoadingCredentials] = useState(false)

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

  // Reset credential selection when type changes
  useEffect(() => {
    setCredentialId('')
  }, [credentialType])

  const credentialOptions = credentialType === 'ExternalApiKey'
    ? apiKeyCredentials.map(c => ({ id: c.id, label: `${c.name} (${c.provider})` }))
    : oauthTokens.map(t => ({ id: t.id, label: `${t.email} (${t.provider})` }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await sandboxCredentialMappings.create({
        baseDomain,
        headerName,
        headerValuePrefix: headerValuePrefix || undefined,
        credentialId,
        credentialType,
        credentialFieldType,
      })

      resetForm()
      onCreated?.()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create mapping')
    } finally {
      setIsSubmitting(false)
    }
  }

  const resetForm = () => {
    setBaseDomain('')
    setHeaderName('')
    setHeaderValuePrefix('')
    setCredentialType('ExternalApiKey')
    setCredentialId('')
    setCredentialFieldType('ApiKey')
    setError(null)
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) resetForm()
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Add Sandbox Credential Mapping</DialogTitle>
          <DialogDescription>
            Map a domain to a credential so sandbox code can authenticate with external APIs.
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
              <Label htmlFor="baseDomain">Base Domain</Label>
              <Input
                id="baseDomain"
                placeholder="api.openai.com"
                value={baseDomain}
                onChange={(e) => setBaseDomain(e.target.value)}
                required
              />
              <p className="text-xs text-muted-foreground">
                The domain that requests will be matched against
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="headerName">Header Name</Label>
              <Input
                id="headerName"
                placeholder="Authorization"
                value={headerName}
                onChange={(e) => setHeaderName(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="headerValuePrefix">Header Value Prefix</Label>
              <Input
                id="headerValuePrefix"
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
              <Select value={credentialType} onValueChange={setCredentialType}>
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

            <div className="space-y-2">
              <Label>Credential Field</Label>
              <Select value={credentialFieldType} onValueChange={(v) => setCredentialFieldType(v as CredentialFieldType)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CREDENTIAL_FIELD_TYPES.map(ft => (
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
              {isSubmitting ? 'Creating...' : 'Create Mapping'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
