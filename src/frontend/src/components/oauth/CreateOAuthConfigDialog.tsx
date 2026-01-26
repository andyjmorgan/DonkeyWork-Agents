import { useState } from 'react'
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
import { oauth, type OAuthProvider, type CreateOAuthProviderConfigRequest } from '@/lib/api'
import { Loader2 } from 'lucide-react'

interface CreateOAuthConfigDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function CreateOAuthConfigDialog({
  open,
  onOpenChange,
  onSuccess,
}: CreateOAuthConfigDialogProps) {
  const [loading, setLoading] = useState(false)
  const [provider, setProvider] = useState<OAuthProvider>('Google')
  const [clientId, setClientId] = useState('')
  const [clientSecret, setClientSecret] = useState('')
  const [redirectUri, setRedirectUri] = useState(
    `${window.location.origin}/api/v1/oauth/${provider.toLowerCase()}/callback`
  )

  const handleProviderChange = (value: OAuthProvider) => {
    setProvider(value)
    setRedirectUri(`${window.location.origin}/api/v1/oauth/${value.toLowerCase()}/callback`)
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!clientId || !clientSecret || !redirectUri) return

    setLoading(true)
    try {
      const request: CreateOAuthProviderConfigRequest = {
        provider,
        clientId,
        clientSecret,
        redirectUri,
      }
      await oauth.createConfig(request)
      onSuccess()
      onOpenChange(false)

      // Reset form
      setProvider('Google')
      setClientId('')
      setClientSecret('')
      setRedirectUri(`${window.location.origin}/api/v1/oauth/google/callback`)
    } catch (error) {
      console.error('Failed to create OAuth config:', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add OAuth Client</DialogTitle>
          <DialogDescription>
            Configure OAuth credentials for an external provider
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="provider">Provider</Label>
            <Select value={provider} onValueChange={handleProviderChange}>
              <SelectTrigger id="provider">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Google">Google</SelectItem>
                <SelectItem value="Microsoft">Microsoft</SelectItem>
                <SelectItem value="GitHub">GitHub</SelectItem>
              </SelectContent>
            </Select>
          </div>

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
              Use this URL when configuring your OAuth application
            </p>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Create Config
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
