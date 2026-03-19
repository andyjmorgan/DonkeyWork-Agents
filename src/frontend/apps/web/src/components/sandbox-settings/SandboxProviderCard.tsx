import { useState } from 'react'
import { ExternalLink, Globe } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button, Badge } from '@donkeywork/ui'
import type { SandboxProviderStatus } from '@donkeywork/api-client'

interface SandboxProviderCardProps {
  status: SandboxProviderStatus
  onEnable: (provider: string) => Promise<void>
  onDisable: (provider: string) => Promise<void>
}

export function SandboxProviderCard({ status, onEnable, onDisable }: SandboxProviderCardProps) {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)

  const handleAction = async () => {
    if (!status.hasOAuthToken) {
      navigate('/connected-accounts')
      return
    }

    try {
      setLoading(true)
      if (status.isEnabled) {
        await onDisable(status.provider)
      } else {
        await onEnable(status.provider)
      }
    } finally {
      setLoading(false)
    }
  }

  const getStatusBadge = () => {
    if (!status.hasOAuthToken) {
      return <Badge variant="pending">Not Connected</Badge>
    }
    if (status.isEnabled) {
      return <Badge variant="success">Enabled</Badge>
    }
    return <Badge variant="secondary">Ready</Badge>
  }

  const getActionLabel = () => {
    if (!status.hasOAuthToken) return 'Connect'
    if (status.isEnabled) return 'Disable'
    return 'Enable'
  }

  const getActionVariant = (): 'default' | 'outline' | 'destructive' => {
    if (!status.hasOAuthToken) return 'outline'
    if (status.isEnabled) return 'destructive'
    return 'default'
  }

  return (
    <div className="rounded-lg border border-border bg-card p-4 space-y-3">
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-1 min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <p className="font-medium">{status.displayName}</p>
            {getStatusBadge()}
          </div>
          {status.domains.length > 0 && (
            <div className="flex flex-wrap gap-1 mt-2">
              {status.domains.map((domain) => (
                <span key={domain} className="inline-flex items-center gap-1 text-xs text-muted-foreground">
                  <Globe className="h-3 w-3" />
                  {domain}
                </span>
              ))}
            </div>
          )}
        </div>
        <Button
          size="sm"
          variant={getActionVariant()}
          onClick={handleAction}
          disabled={loading}
        >
          {!status.hasOAuthToken && <ExternalLink className="h-3.5 w-3.5 mr-1" />}
          {getActionLabel()}
        </Button>
      </div>
    </div>
  )
}
