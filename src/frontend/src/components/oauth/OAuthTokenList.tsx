import { type OAuthToken } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ProviderIcon } from './ProviderIcon'
import { RefreshCw, Unplug } from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'

interface OAuthTokenListProps {
  tokens: OAuthToken[]
  onRefresh: (tokenId: string) => Promise<void>
  onDisconnect: (tokenId: string) => Promise<void>
}

function getStatusColor(status: string) {
  switch (status) {
    case 'Active':
      return 'default'
    case 'ExpiringSoon':
      return 'destructive'
    case 'Expired':
      return 'destructive'
    default:
      return 'secondary'
  }
}

function getStatusVariant(status: string) {
  switch (status) {
    case 'Active':
      return 'success'
    case 'ExpiringSoon':
      return 'warning'
    case 'Expired':
      return 'destructive'
    default:
      return 'secondary'
  }
}

export function OAuthTokenList({ tokens, onRefresh, onDisconnect }: OAuthTokenListProps) {
  if (tokens.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-border bg-muted/50 p-8 text-center">
        <p className="text-sm text-muted-foreground">
          No connected accounts. Add an OAuth client above to connect.
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {tokens.map((token) => (
        <div
          key={token.id}
          className="rounded-lg border border-border bg-card p-4 space-y-3"
        >
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-3 min-w-0 flex-1">
              <ProviderIcon provider={token.provider} className="h-6 w-6 shrink-0" />
              <div className="space-y-1 min-w-0 flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <p className="font-medium truncate">{token.provider}</p>
                  <Badge variant={getStatusVariant(token.status) as any}>
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
                onClick={() => onRefresh(token.id)}
                title="Refresh token"
              >
                <RefreshCw className="h-4 w-4" />
              </Button>
              <Button
                size="sm"
                variant="ghost"
                onClick={() => onDisconnect(token.id)}
                title="Disconnect"
              >
                <Unplug className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-2 text-xs text-muted-foreground">
            <div>
              <span>Expires:</span>{' '}
              <span className="font-medium">
                {formatDistanceToNow(new Date(token.expiresAt), { addSuffix: true })}
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
  )
}
