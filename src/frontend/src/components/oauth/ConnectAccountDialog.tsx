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
import { Switch } from '@/components/ui/switch'
import { Badge } from '@/components/ui/badge'
import { ProviderIcon } from './ProviderIcon'
import {
  oauth,
  type OAuthProvider,
  type OAuthProviderConfig,
  type OAuthProviderMetadata,
  type OAuthScopeMetadata,
} from '@/lib/api'
import { Loader2, Lock, LinkIcon } from 'lucide-react'

interface ConnectAccountDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  config: OAuthProviderConfig | null
  onConnect: (provider: OAuthProvider, scopes: string[]) => Promise<void>
}

export function ConnectAccountDialog({
  open,
  onOpenChange,
  config,
  onConnect,
}: ConnectAccountDialogProps) {
  const [loading, setLoading] = useState(false)
  const [metadata, setMetadata] = useState<OAuthProviderMetadata | null>(null)
  const [metadataLoading, setMetadataLoading] = useState(false)
  const [selectedScopes, setSelectedScopes] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (open && config) {
      loadMetadataAndInitScopes()
    }
  }, [open, config])

  const loadMetadataAndInitScopes = async () => {
    if (!config) return

    try {
      setMetadataLoading(true)
      const allMetadata = await oauth.getProviderMetadata()
      const providerMeta = allMetadata.find((m) => m.provider === config.provider) ?? null
      setMetadata(providerMeta)

      // Initialize from the config's saved scopes, or fall back to the provider's defaults
      if (config.scopes && config.scopes.length > 0) {
        setSelectedScopes(new Set(config.scopes))
      } else if (providerMeta) {
        const defaults = new Set(
          providerMeta.availableScopes
            .filter((s) => s.isDefault || s.isRequired)
            .map((s) => s.name)
        )
        setSelectedScopes(defaults)
      }
    } catch (error) {
      console.error('Failed to load provider metadata:', error)
    } finally {
      setMetadataLoading(false)
    }
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

  const handleConnect = async () => {
    if (!config) return

    setLoading(true)
    try {
      await onConnect(config.provider, Array.from(selectedScopes))
    } catch {
      // Error handling is done by the parent
    } finally {
      setLoading(false)
    }
  }

  const providerName = config?.provider === 'Custom'
    ? (config?.customProviderName || 'Custom')
    : config?.provider

  // Build a lookup of scope metadata for the provider
  const scopeMetadataMap = new Map<string, OAuthScopeMetadata>()
  if (metadata) {
    for (const s of metadata.availableScopes) {
      scopeMetadataMap.set(s.name, s)
    }
  }

  // Determine the list of scopes to display.
  // For built-in providers: show availableScopes from metadata.
  // For custom providers or when we have saved scopes not in metadata: show them as plain list.
  const isBuiltIn = metadata?.isBuiltIn ?? false
  const availableScopes = metadata?.availableScopes ?? []

  // Gather any custom scopes that are in the config but not in availableScopes
  const extraScopes = (config?.scopes ?? []).filter(
    (s) => !scopeMetadataMap.has(s)
  )

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            <div className="flex items-center gap-2">
              {config && <ProviderIcon provider={config.provider} className="h-5 w-5" />}
              <span>Connect {providerName}</span>
            </div>
          </DialogTitle>
          <DialogDescription>
            Review the permissions that will be requested when you connect your account.
          </DialogDescription>
        </DialogHeader>

        {metadataLoading ? (
          <div className="flex items-center justify-center p-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <div className="space-y-4">
            {/* Scope selection for built-in providers */}
            {isBuiltIn && availableScopes.length > 0 && (
              <div className="space-y-2">
                <p className="text-sm font-medium">Permissions</p>
                <div className="rounded-md border border-border divide-y divide-border">
                  {availableScopes.map((scope) => {
                    const isSelected = selectedScopes.has(scope.name)
                    return (
                      <div
                        key={scope.name}
                        className="flex items-center justify-between gap-3 px-3 py-2.5"
                      >
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-1.5">
                            <span className="text-sm font-medium truncate">
                              {scope.description}
                            </span>
                            {scope.isRequired && (
                              <Lock className="h-3 w-3 text-muted-foreground shrink-0" />
                            )}
                          </div>
                          <p className="text-xs text-muted-foreground font-mono truncate mt-0.5">
                            {scope.name}
                          </p>
                        </div>
                        <Switch
                          checked={isSelected}
                          onCheckedChange={(checked) =>
                            handleScopeToggle(scope.name, checked)
                          }
                          disabled={scope.isRequired}
                        />
                      </div>
                    )
                  })}
                </div>
              </div>
            )}

            {/* For custom providers or extra scopes: show as badges */}
            {(!isBuiltIn || extraScopes.length > 0) && selectedScopes.size > 0 && (
              <div className="space-y-2">
                <p className="text-sm font-medium">
                  {isBuiltIn ? 'Additional Scopes' : 'Scopes'}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {(isBuiltIn ? extraScopes : Array.from(selectedScopes)).map(
                    (scope) => (
                      <Badge
                        key={scope}
                        variant="outline"
                        className="text-xs font-mono"
                      >
                        {scope}
                      </Badge>
                    )
                  )}
                </div>
              </div>
            )}

            {/* No scopes message */}
            {selectedScopes.size === 0 && availableScopes.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-4">
                No scopes configured. The provider's defaults will be used.
              </p>
            )}
          </div>
        )}

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
          >
            Cancel
          </Button>
          <Button
            onClick={handleConnect}
            disabled={loading || metadataLoading}
          >
            {loading ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <LinkIcon className="mr-2 h-4 w-4" />
            )}
            Connect
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
