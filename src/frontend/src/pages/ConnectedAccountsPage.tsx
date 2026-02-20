import { useState, useEffect } from 'react'
import { Eye, RefreshCw, Unplug } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ProviderIcon } from '@/components/oauth/ProviderIcon'
import { ViewOAuthTokenDialog } from '@/components/oauth/ViewOAuthTokenDialog'
import { oauth, type OAuthToken } from '@/lib/api'
import { useOAuthFlow } from '@/hooks/useOAuthFlow'
import { formatDistanceToNow } from 'date-fns'

export function ConnectedAccountsPage() {
  const [tokens, setTokens] = useState<OAuthToken[]>([])
  const [loading, setLoading] = useState(true)
  const [viewTokenId, setViewTokenId] = useState<string | null>(null)
  const [viewDialogOpen, setViewDialogOpen] = useState(false)
  const { disconnect, refresh } = useOAuthFlow()

  const loadTokens = async () => {
    try {
      setLoading(true)
      const data = await oauth.listTokens()
      setTokens(data)
    } catch (error) {
      console.error('Failed to load OAuth tokens:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadTokens()
  }, [])

  const handleRefresh = async (tokenId: string) => {
    try {
      await refresh(tokenId)
      loadTokens()
    } catch {
      alert('Failed to refresh token')
    }
  }

  const handleDisconnect = async (tokenId: string) => {
    if (!confirm('Are you sure you want to disconnect this account?')) return

    try {
      await disconnect(tokenId)
      loadTokens()
    } catch {
      alert('Failed to disconnect account')
    }
  }

  const getStatusVariant = (status: string): 'default' | 'secondary' | 'destructive' | 'outline' => {
    switch (status) {
      case 'Active':
        return 'default'
      case 'ExpiringSoon':
        return 'secondary'
      case 'Expired':
        return 'destructive'
      default:
        return 'outline'
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold">Connected Accounts</h1>
        <p className="text-muted-foreground">
          Manage your connected OAuth accounts
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center rounded-lg border border-border p-12">
          <p className="text-sm text-muted-foreground">Loading connected accounts...</p>
        </div>
      ) : tokens.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Unplug className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No connected accounts</h3>
          <p className="mt-2 text-sm text-muted-foreground max-w-sm">
            Configure an OAuth client and connect an account to get started
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {tokens.map((token) => (
            <div key={token.id} className="rounded-lg border border-border bg-card p-4 space-y-3">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <ProviderIcon provider={token.provider} className="h-6 w-6 shrink-0" />
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="font-medium truncate">{token.provider}</p>
                      <Badge variant={getStatusVariant(token.status)}>
                        {token.status}
                      </Badge>
                    </div>
                    <p className="text-sm text-muted-foreground truncate">{token.email}</p>
                  </div>
                </div>
                <div className="flex items-center gap-1 shrink-0">
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => {
                      setViewTokenId(token.id)
                      setViewDialogOpen(true)
                    }}
                    title="View access token"
                  >
                    <Eye className="h-4 w-4" />
                  </Button>
                  {token.canRefresh && (
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => handleRefresh(token.id)}
                      title="Refresh token"
                    >
                      <RefreshCw className="h-4 w-4" />
                    </Button>
                  )}
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleDisconnect(token.id)}
                    title="Disconnect"
                  >
                    <Unplug className="h-4 w-4" />
                  </Button>
                </div>
              </div>
              {token.scopes.length > 0 && (
                <div className="flex flex-wrap gap-1">
                  {token.scopes.map((scope) => (
                    <Badge key={scope} variant="outline" className="text-xs font-normal">
                      {scope}
                    </Badge>
                  ))}
                </div>
              )}

              <div className="grid grid-cols-2 gap-2 text-xs text-muted-foreground">
                <div>
                  <span>Expires:</span>{' '}
                  <span className="font-medium">
                    {token.expiresAt
                      ? formatDistanceToNow(new Date(token.expiresAt), { addSuffix: true })
                      : 'Does not expire'}
                  </span>
                </div>
                {token.lastRefreshedAt && (
                  <div>
                    <span>Last refreshed:</span>{' '}
                    <span className="font-medium">
                      {formatDistanceToNow(new Date(token.lastRefreshedAt), { addSuffix: true })}
                    </span>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      <ViewOAuthTokenDialog
        open={viewDialogOpen}
        onOpenChange={setViewDialogOpen}
        tokenId={viewTokenId}
      />
    </div>
  )
}
