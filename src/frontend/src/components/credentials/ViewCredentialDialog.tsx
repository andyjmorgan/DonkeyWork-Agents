import { useState } from 'react'
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
import { Copy, Eye, EyeOff } from 'lucide-react'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'

interface ViewCredentialDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  credentialId: string | null
  credentialName: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  apiKey: string
  createdAt: string
}

// Inner component that resets when key changes
function ViewCredentialDialogContent({
  credentialName,
  provider,
  apiKey,
  createdAt,
  onClose,
}: {
  credentialName: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  apiKey: string
  createdAt: string
  onClose: () => void
}) {
  const [showKey, setShowKey] = useState(false)
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(apiKey)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  const getProviderIcon = (provider: string) => {
    switch (provider) {
      case 'OpenAi':
        return <OpenAIIcon className="h-5 w-5" />
      case 'Anthropic':
        return <AnthropicIcon className="h-5 w-5" />
      case 'Google':
        return <GoogleIcon className="h-5 w-5" />
      case 'Azure':
        return null // TODO: Add Azure icon
      default:
        return null
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }

  const maskKey = (key: string) => {
    if (key.length <= 8) return '--------'
    return key.substring(0, 4) + '--------' + key.substring(key.length - 4)
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>Credential Details</DialogTitle>
        <DialogDescription>
          View and copy your API key. Keep it secure and never share it publicly.
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-4 py-4">
        <div className="space-y-2">
          <Label>Provider</Label>
          <div className="flex items-center gap-2 rounded-md border border-input bg-muted/50 px-3 py-2">
            {getProviderIcon(provider)}
            <span className="text-sm font-medium">{provider}</span>
          </div>
        </div>

        <div className="space-y-2">
          <Label>Name</Label>
          <div className="rounded-md border border-input bg-muted/50 px-3 py-2">
            <span className="text-sm">{credentialName}</span>
          </div>
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <Label>API Key</Label>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setShowKey(!showKey)}
            >
              {showKey ? (
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
              value={showKey ? apiKey : maskKey(apiKey)}
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
            {showKey ? 'Keep this key secure and never share it' : 'Click Reveal to view the full API key'}
          </p>
        </div>

        <div className="space-y-2">
          <Label>Created</Label>
          <div className="rounded-md border border-input bg-muted/50 px-3 py-2">
            <span className="text-sm">{formatDate(createdAt)}</span>
          </div>
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

export function ViewCredentialDialog({
  open,
  onOpenChange,
  credentialId,
  credentialName,
  provider,
  apiKey,
  createdAt
}: ViewCredentialDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        {/* Use key to reset internal state when dialog opens with different credential */}
        <ViewCredentialDialogContent
          key={credentialId ?? 'none'}
          credentialName={credentialName}
          provider={provider}
          apiKey={apiKey}
          createdAt={createdAt}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
