import { useState, useEffect } from 'react'
import { Plus, Trash2, Link as LinkIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { CreateOAuthConfigDialog } from '@/components/oauth/CreateOAuthConfigDialog'
import { ConnectAccountDialog } from '@/components/oauth/ConnectAccountDialog'
import { ProviderIcon } from '@/components/oauth/ProviderIcon'
import { oauth, type OAuthProvider, type OAuthProviderConfig } from '@/lib/api'
import { useOAuthFlow } from '@/hooks/useOAuthFlow'

export function OAuthClientsPage() {
  const [oauthConfigs, setOAuthConfigs] = useState<OAuthProviderConfig[]>([])
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [connectConfig, setConnectConfig] = useState<OAuthProviderConfig | null>(null)
  const { initiateFlow } = useOAuthFlow()

  const loadOAuthConfigs = async () => {
    try {
      setLoading(true)
      const configs = await oauth.listConfigs()
      setOAuthConfigs(configs)
    } catch (error) {
      console.error('Failed to load OAuth configs:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadOAuthConfigs()
  }, [])

  const handleConnectClick = (configId: string) => {
    const config = oauthConfigs.find(c => c.id === configId)
    if (!config) return
    setConnectConfig(config)
  }

  const handleConnect = async (provider: OAuthProvider, scopes: string[]) => {
    try {
      await initiateFlow(provider, scopes.length > 0 ? scopes : undefined)
    } catch {
      alert('Failed to initiate OAuth flow')
    }
  }

  const handleDelete = async (configId: string) => {
    if (!confirm('Are you sure you want to delete this OAuth configuration?')) return

    try {
      await oauth.deleteConfig(configId)
      loadOAuthConfigs()
    } catch {
      alert('Failed to delete OAuth configuration')
    }
  }

  const handleConfigCreated = () => {
    loadOAuthConfigs()
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">OAuth Clients</h1>
          <p className="text-muted-foreground">
            Configure OAuth providers for external integrations
          </p>
        </div>
        <Button onClick={() => setIsCreateDialogOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Add OAuth Client
        </Button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center rounded-lg border border-border p-12">
          <p className="text-sm text-muted-foreground">Loading OAuth configurations...</p>
        </div>
      ) : oauthConfigs.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No OAuth clients configured</h3>
          <p className="mt-2 text-sm text-muted-foreground max-w-sm">
            Add an OAuth client to connect external accounts
          </p>
          <Button className="mt-4" onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add OAuth Client
          </Button>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {oauthConfigs.map((config) => (
            <Card key={config.id}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <ProviderIcon provider={config.provider} />
                    <CardTitle className="text-base">
                      {config.provider === 'Custom'
                        ? (config.customProviderName || 'Custom')
                        : config.provider}
                    </CardTitle>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleDelete(config.id)}
                  >
                    <Trash2 className="h-4 w-4 text-red-500" />
                  </Button>
                </div>
                <CardDescription className="text-xs truncate">
                  {config.redirectUri}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                {config.scopes && config.scopes.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {config.scopes.slice(0, 4).map((scope) => (
                      <Badge key={scope} variant="outline" className="text-[10px] font-mono px-1.5 py-0">
                        {scope.length > 30 ? `${scope.slice(0, 27)}...` : scope}
                      </Badge>
                    ))}
                    {config.scopes.length > 4 && (
                      <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                        +{config.scopes.length - 4} more
                      </Badge>
                    )}
                  </div>
                )}
                {!config.hasToken ? (
                  <Button
                    size="sm"
                    className="w-full"
                    onClick={() => handleConnectClick(config.id)}
                  >
                    <LinkIcon className="h-4 w-4 mr-2" />
                    Connect Account
                  </Button>
                ) : (
                  <div className="text-xs text-muted-foreground text-center">
                    Account connected
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <CreateOAuthConfigDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onSuccess={handleConfigCreated}
      />

      <ConnectAccountDialog
        open={connectConfig !== null}
        onOpenChange={(open) => { if (!open) setConnectConfig(null) }}
        config={connectConfig}
        onConnect={handleConnect}
      />
    </div>
  )
}
