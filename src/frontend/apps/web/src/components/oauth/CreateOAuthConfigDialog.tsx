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
  Badge,
  Switch,
} from '@donkeywork/ui'
import { ProviderIcon } from './ProviderIcon'
import {
  oauth,
  type OAuthProvider,
  type OAuthProviderMetadata,
  type CreateOAuthProviderConfigRequest,
} from '@/lib/api'
import { Loader2, ExternalLink, Globe, ChevronLeft, Lock } from 'lucide-react'
import { cn } from '@/lib/utils'

interface CreateOAuthConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

const BUILT_IN_PROVIDERS: OAuthProvider[] = ['Google', 'Microsoft', 'GitHub']

export function CreateOAuthConfigDialog({
  open,
  onOpenChange,
  onSuccess,
}: CreateOAuthConfigDialogProps) {
  const [loading, setLoading] = useState(false)
  const [step, setStep] = useState<'select' | 'configure'>('select')
  const [provider, setProvider] = useState<OAuthProvider | null>(null)
  const [metadata, setMetadata] = useState<OAuthProviderMetadata[]>([])
  const [metadataLoading, setMetadataLoading] = useState(true)

  // Form fields
  const [clientId, setClientId] = useState('')
  const [clientSecret, setClientSecret] = useState('')
  const [redirectUri, setRedirectUri] = useState('')
  const [customProviderName, setCustomProviderName] = useState('')
  const [authorizationUrl, setAuthorizationUrl] = useState('')
  const [tokenUrl, setTokenUrl] = useState('')
  const [userInfoUrl, setUserInfoUrl] = useState('')
  const [scopes, setScopes] = useState('')

  // Scope selection for built-in providers
  const [selectedScopes, setSelectedScopes] = useState<Set<string>>(new Set())

  // Load provider metadata on mount
  useEffect(() => {
    if (open) {
      loadMetadata()
    }
  }, [open])

  const loadMetadata = async () => {
    try {
      setMetadataLoading(true)
      const data = await oauth.getProviderMetadata()
      setMetadata(data)
    } catch (error) {
      console.error('Failed to load provider metadata:', error)
    } finally {
      setMetadataLoading(false)
    }
  }

  const getProviderMetadata = (p: OAuthProvider) => {
    return metadata.find((m) => m.provider === p)
  }

  const handleProviderSelect = (p: OAuthProvider) => {
    setProvider(p)
    const meta = getProviderMetadata(p)
    setRedirectUri(`${window.location.origin}/api/v1/oauth/${p.toLowerCase()}/callback`)

    if (p === 'Custom') {
      setAuthorizationUrl('')
      setTokenUrl('')
      setUserInfoUrl('')
      setScopes('')
      setCustomProviderName('')
      setSelectedScopes(new Set())
    } else if (meta) {
      setAuthorizationUrl(meta.authorizationUrl)
      setTokenUrl(meta.tokenUrl)
      setUserInfoUrl(meta.userInfoUrl)
      // Initialize selected scopes from available scopes metadata
      const defaults = new Set(
        (meta.availableScopes ?? [])
          .filter((s) => s.isDefault || s.isRequired)
          .map((s) => s.name)
      )
      setSelectedScopes(defaults)
    }
    setStep('configure')
  }

  const handleScopeToggle = (scopeName: string, checked: boolean) => {
    setSelectedScopes((prev) => {
      const next = new Set(prev)
      if (checked) {
        next.add(scopeName)
      } else {
        next.delete(scopeName)
      }
      return next
    })
  }

  const handleBack = () => {
    setStep('select')
  }

  const resetForm = () => {
    setStep('select')
    setProvider(null)
    setClientId('')
    setClientSecret('')
    setRedirectUri('')
    setCustomProviderName('')
    setAuthorizationUrl('')
    setTokenUrl('')
    setUserInfoUrl('')
    setScopes('')
    setSelectedScopes(new Set())
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!provider || !clientId || !clientSecret || !redirectUri) return
    if (provider === 'Custom' && (!authorizationUrl || !tokenUrl)) return

    setLoading(true)
    try {
      const request: CreateOAuthProviderConfigRequest = {
        provider,
        clientId,
        clientSecret,
        redirectUri,
      }

      if (provider === 'Custom') {
        request.authorizationUrl = authorizationUrl
        request.tokenUrl = tokenUrl
        request.userInfoUrl = userInfoUrl || undefined
        request.customProviderName = customProviderName || undefined
        if (scopes.trim()) {
          request.scopes = scopes.split(',').map((s) => s.trim()).filter(Boolean)
        }
      } else {
        // For built-in providers, send the selected scopes
        if (selectedScopes.size > 0) {
          request.scopes = Array.from(selectedScopes)
        }
      }

      await oauth.createConfig(request)
      onSuccess()
      onOpenChange(false)
      resetForm()
    } catch (error) {
      console.error('Failed to create OAuth config:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      resetForm()
    }
    onOpenChange(isOpen)
  }

  const selectedMeta = provider ? getProviderMetadata(provider) : null

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {step === 'select' ? 'Add OAuth Client' : (
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0"
                  onClick={handleBack}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span>Configure {provider === 'Custom' ? (customProviderName || 'Custom') : provider}</span>
              </div>
            )}
          </DialogTitle>
          <DialogDescription>
            {step === 'select'
              ? 'Choose a provider to connect with your application'
              : selectedMeta?.setupInstructions}
          </DialogDescription>
        </DialogHeader>

        {step === 'select' ? (
          <div className="space-y-3">
            {metadataLoading ? (
              <div className="flex items-center justify-center p-8">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : (
              <>
                {/* Built-in provider cards */}
                <div className="grid gap-2">
                  {BUILT_IN_PROVIDERS.map((p) => {
                    const meta = getProviderMetadata(p)
                    return (
                      <button
                        key={p}
                        onClick={() => handleProviderSelect(p)}
                        className={cn(
                          'flex items-center gap-3 rounded-lg border border-border p-3 text-left',
                          'transition-colors hover:bg-accent hover:border-accent-foreground/20',
                          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'
                        )}
                      >
                        <div className="rounded-md border border-border bg-background p-2">
                          <ProviderIcon provider={p} className="h-5 w-5" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2">
                            <p className="font-medium text-sm">{meta?.displayName ?? p}</p>
                            <Badge variant="secondary" className="text-[10px] px-1.5 py-0">
                              Built-in
                            </Badge>
                          </div>
                          <p className="text-xs text-muted-foreground truncate mt-0.5">
                            {p === 'Google' && 'Gmail, Google Drive, Calendar'}
                            {p === 'Microsoft' && 'Outlook, OneDrive, Microsoft Graph'}
                            {p === 'GitHub' && 'Repositories, Issues, Pull Requests'}
                          </p>
                        </div>
                      </button>
                    )
                  })}
                </div>

                {/* Custom provider option */}
                <div className="relative">
                  <div className="absolute inset-0 flex items-center">
                    <span className="w-full border-t" />
                  </div>
                  <div className="relative flex justify-center text-xs uppercase">
                    <span className="bg-background px-2 text-muted-foreground">or</span>
                  </div>
                </div>

                <button
                  onClick={() => handleProviderSelect('Custom')}
                  className={cn(
                    'flex items-center gap-3 rounded-lg border border-dashed border-border p-3 text-left w-full',
                    'transition-colors hover:bg-accent hover:border-accent-foreground/20',
                    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'
                  )}
                >
                  <div className="rounded-md border border-border bg-background p-2">
                    <Globe className="h-5 w-5 text-muted-foreground" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-sm">Custom Provider</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      Connect any OAuth 2.0 compatible service
                    </p>
                  </div>
                </button>
              </>
            )}
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Setup link for built-in providers */}
            {selectedMeta?.isBuiltIn && selectedMeta.setupUrl && (
              <a
                href={selectedMeta.setupUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 rounded-md border border-border bg-muted/50 p-2.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
              >
                <ExternalLink className="h-4 w-4 shrink-0" />
                <span>Open {provider} developer console to create your OAuth app</span>
              </a>
            )}

            {/* Custom provider name */}
            {provider === 'Custom' && (
              <div className="space-y-2">
                <Label htmlFor="customName">Provider Name</Label>
                <Input
                  id="customName"
                  value={customProviderName}
                  onChange={(e) => setCustomProviderName(e.target.value)}
                  placeholder="e.g. Slack, Salesforce, etc."
                />
              </div>
            )}

            {/* Client credentials */}
            <div className="space-y-2">
              <Label htmlFor="clientId">Client ID</Label>
              <Input
                id="clientId"
                value={clientId}
                onChange={(e) => setClientId(e.target.value)}
                placeholder="Enter client ID from OAuth provider"
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
                placeholder="Enter client secret"
                required
              />
            </div>

            {/* Redirect URI */}
            <div className="space-y-2">
              <Label htmlFor="redirectUri">Redirect URI</Label>
              <Input
                id="redirectUri"
                value={redirectUri}
                onChange={(e) => setRedirectUri(e.target.value)}
                placeholder="OAuth callback URL"
                required
              />
              <p className="text-xs text-muted-foreground">
                Copy this URL into your OAuth app's redirect/callback settings
              </p>
            </div>

            {/* Endpoint URLs and scopes - different for built-in vs custom */}
            {provider === 'Custom' ? (
              <>
                <div className="space-y-2">
                  <Label htmlFor="authUrl">Authorization URL <span className="text-destructive">*</span></Label>
                  <Input
                    id="authUrl"
                    value={authorizationUrl}
                    onChange={(e) => setAuthorizationUrl(e.target.value)}
                    placeholder="https://provider.com/oauth/authorize"
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="tokenUrl">Token URL <span className="text-destructive">*</span></Label>
                  <Input
                    id="tokenUrl"
                    value={tokenUrl}
                    onChange={(e) => setTokenUrl(e.target.value)}
                    placeholder="https://provider.com/oauth/token"
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="userInfoUrl">User Info URL</Label>
                  <Input
                    id="userInfoUrl"
                    value={userInfoUrl}
                    onChange={(e) => setUserInfoUrl(e.target.value)}
                    placeholder="https://provider.com/api/userinfo (optional)"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="scopes">Scopes</Label>
                  <Input
                    id="scopes"
                    value={scopes}
                    onChange={(e) => setScopes(e.target.value)}
                    placeholder="openid, profile, email (comma-separated)"
                  />
                  <p className="text-xs text-muted-foreground">
                    Enter the scopes you need, separated by commas
                  </p>
                </div>
              </>
            ) : selectedMeta && (
              <>
                {/* OAuth endpoints info */}
                <div className="rounded-md border border-border bg-muted/30 p-3 space-y-2">
                  <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                    OAuth Endpoints (auto-configured)
                  </p>
                  <div className="space-y-1.5 text-xs">
                    <div>
                      <span className="text-muted-foreground">Authorization: </span>
                      <span className="font-mono text-[11px] break-all">{selectedMeta.authorizationUrl}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">Token: </span>
                      <span className="font-mono text-[11px] break-all">{selectedMeta.tokenUrl}</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">User Info: </span>
                      <span className="font-mono text-[11px] break-all">{selectedMeta.userInfoUrl}</span>
                    </div>
                  </div>
                </div>

                {/* Scope selection for built-in providers */}
                {(selectedMeta.availableScopes ?? []).length > 0 && (
                  <div className="space-y-2">
                    <Label>Scopes</Label>
                    <p className="text-xs text-muted-foreground">
                      Select the permissions this connector should request
                    </p>
                    <div className="rounded-md border border-border divide-y divide-border">
                      {selectedMeta.availableScopes.map((scope) => (
                        <div
                          key={scope.name}
                          className="flex items-center justify-between gap-3 px-3 py-2.5"
                        >
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-1.5">
                              <span className="text-sm font-medium truncate">{scope.description}</span>
                              {scope.isRequired && (
                                <Lock className="h-3 w-3 text-muted-foreground shrink-0" />
                              )}
                            </div>
                            <p className="text-xs text-muted-foreground font-mono truncate mt-0.5">
                              {scope.name}
                            </p>
                          </div>
                          <Switch
                            checked={selectedScopes.has(scope.name)}
                            onCheckedChange={(checked) => handleScopeToggle(scope.name, checked)}
                            disabled={scope.isRequired}
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={loading}>
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Create
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}
