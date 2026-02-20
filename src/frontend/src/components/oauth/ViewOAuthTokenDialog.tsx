import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Copy, Eye, EyeOff, Loader2 } from 'lucide-react'
import { ProviderIcon } from '@/components/oauth/ProviderIcon'
import { oauth, type GetOAuthAccessTokenResponse, type OAuthTokenStatus } from '@/lib/api'

interface ViewOAuthTokenDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  tokenId: string | null
}

function ViewOAuthTokenDialogContent({
  tokenId,
  onClose,
}: {
  tokenId: string
  onClose: () => void
}) {
  const [tokenData, setTokenData] = useState<GetOAuthAccessTokenResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showToken, setShowToken] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    const fetchToken = async () => {
      try {
        setLoading(true)
        setError(null)
        const data = await oauth.getAccessToken(tokenId)
        setTokenData(data)
      } catch {
        setError('Failed to retrieve access token')
      } finally {
        setLoading(false)
      }
    }

    fetchToken()
  }, [tokenId])

  const handleCopy = async () => {
    if (!tokenData) return
    try {
      await navigator.clipboard.writeText(tokenData.accessToken)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  const getStatusVariant = (status: OAuthTokenStatus): 'default' | 'secondary' | 'destructive' => {
    switch (status) {
      case 'Active':
        return 'default'
      case 'ExpiringSoon':
        return 'secondary'
      case 'Expired':
        return 'destructive'
    }
  }

  const maskToken = (token: string) => {
    if (token.length <= 8) return '--------'
    return token.substring(0, 4) + '--------' + token.substring(token.length - 4)
  }

  if (loading) {
    return (
      <>
        <DialogHeader>
          <DialogTitle>Access Token</DialogTitle>
          <DialogDescription>Loading token details...</DialogDescription>
        </DialogHeader>
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      </>
    )
  }

  if (error || !tokenData) {
    return (
      <>
        <DialogHeader>
          <DialogTitle>Access Token</DialogTitle>
          <DialogDescription>Unable to retrieve the access token.</DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <p className="text-sm text-destructive">{error || 'Token not found'}</p>
        </div>
        <div className="flex justify-end">
          <Button variant="outline" onClick={onClose}>Close</Button>
        </div>
      </>
    )
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>Access Token</DialogTitle>
        <DialogDescription>
          View and copy the access token. Keep it secure and never share it publicly.
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4 py-4">
        <div className="space-y-2">
          <Label>Provider</Label>
          <div className="flex items-center gap-2 rounded-md border border-input bg-muted/50 px-3 py-2">
            <ProviderIcon provider={tokenData.provider} className="h-5 w-5" />
            <span className="text-sm font-medium">{tokenData.provider}</span>
            <Badge variant={getStatusVariant(tokenData.status)} className="ml-auto">
              {tokenData.status}
            </Badge>
          </div>
        </div>

        <div className="space-y-2">
          <Label>Email</Label>
          <div className="rounded-md border border-input bg-muted/50 px-3 py-2">
            <span className="text-sm">{tokenData.email}</span>
          </div>
        </div>

        {tokenData.expiresAt && (
          <div className="space-y-2">
            <Label>Expires</Label>
            <div className="rounded-md border border-input bg-muted/50 px-3 py-2">
              <span className="text-sm">
                {new Date(tokenData.expiresAt).toLocaleString('en-US', {
                  year: 'numeric',
                  month: 'short',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </span>
            </div>
          </div>
        )}

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>Access Token</Label>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setShowToken(!showToken)}
            >
              {showToken ? (
                <>
                  <EyeOff className="h-4 w-4 mr-2" />
                  Hide
                </>
              ) : (
                <>
                  <Eye className="h-4 w-4 mr-2" />
                  Reveal
                </>
              )}
            </Button>
          </div>
          <div className="relative">
            <Input
              type="text"
              value={showToken ? tokenData.accessToken : maskToken(tokenData.accessToken)}
              readOnly
              className="pr-20 font-mono text-xs"
            />
            <Button
              variant="ghost"
              size="sm"
              className="absolute right-1 top-1/2 -translate-y-1/2"
              onClick={handleCopy}
            >
              <Copy className="h-4 w-4 mr-1" />
              {copied ? 'Copied!' : 'Copy'}
            </Button>
          </div>
          <p className="text-xs text-muted-foreground">
            {showToken
              ? 'Keep this token secure and never share it'
              : 'Click Reveal to view the full access token'}
          </p>
        </div>
      </div>

      <div className="flex justify-end">
        <Button variant="outline" onClick={onClose}>
          Close
        </Button>
      </div>
    </>
  )
}

export function ViewOAuthTokenDialog({
  open,
  onOpenChange,
  tokenId,
}: ViewOAuthTokenDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        {tokenId && (
          <ViewOAuthTokenDialogContent
            key={tokenId}
            tokenId={tokenId}
            onClose={() => onOpenChange(false)}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
