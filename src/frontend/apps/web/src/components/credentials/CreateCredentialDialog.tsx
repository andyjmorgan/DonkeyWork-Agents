import { useState } from 'react'
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@donkeywork/ui'
import { credentials } from '@donkeywork/api-client'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'

interface CreateCredentialDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated?: (credentialId: string) => void
  defaultProvider?: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
}

export function CreateCredentialDialog({
  open,
  onOpenChange,
  onCreated,
  defaultProvider
}: CreateCredentialDialogProps) {
  const [provider, setProvider] = useState<'OpenAi' | 'Anthropic' | 'Google' | 'Azure'>(defaultProvider || 'OpenAi')
  const [name, setName] = useState('')
  const [apiKey, setApiKey] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const requestBody = {
        provider,
        name,
        apiKey
      }

      const response = await credentials.create(requestBody)

      // Reset form
      setName('')
      setApiKey('')
      setProvider(defaultProvider || 'OpenAi')

      // Notify parent and close
      onCreated?.(response.id)
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create credential')
    } finally {
      setIsSubmitting(false)
    }
  }

  const getProviderIcon = (provider: string) => {
    switch (provider) {
      case 'OpenAi':
        return <OpenAIIcon className="h-4 w-4" />
      case 'Anthropic':
        return <AnthropicIcon className="h-4 w-4" />
      case 'Google':
        return <GoogleIcon className="h-4 w-4" />
      case 'Azure':
        return <OpenAIIcon className="h-4 w-4" /> // Use OpenAI icon for Azure
      default:
        return null
    }
  }

  const getProviderDisplayName = (provider: string) => {
    switch (provider) {
      case 'OpenAi':
        return 'OpenAI'
      case 'Anthropic':
        return 'Anthropic'
      case 'Google':
        return 'Google'
      case 'Azure':
        return 'Azure OpenAI'
      default:
        return provider
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      // Reset form when closing
      setName('')
      setApiKey('')
      setProvider(defaultProvider || 'OpenAi')
      setError(null)
    }
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Add Credential</DialogTitle>
          <DialogDescription>
            Add an API key for an LLM provider. Your key is encrypted and stored securely.
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
              <Label htmlFor="provider">Provider</Label>
              {defaultProvider ? (
                <div className="flex items-center gap-2 rounded-md border border-input bg-muted/50 px-3 py-2">
                  {getProviderIcon(provider)}
                  <span className="text-sm">{getProviderDisplayName(provider)}</span>
                </div>
              ) : (
                <Select value={provider} onValueChange={(value) => setProvider(value as 'OpenAi' | 'Anthropic' | 'Google' | 'Azure')}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="OpenAi">
                      <div className="flex items-center gap-2">
                        {getProviderIcon('OpenAi')}
                        <span>OpenAI</span>
                      </div>
                    </SelectItem>
                    <SelectItem value="Anthropic">
                      <div className="flex items-center gap-2">
                        {getProviderIcon('Anthropic')}
                        <span>Anthropic</span>
                      </div>
                    </SelectItem>
                    <SelectItem value="Google">
                      <div className="flex items-center gap-2">
                        {getProviderIcon('Google')}
                        <span>Google</span>
                      </div>
                    </SelectItem>
                    <SelectItem value="Azure">
                      <div className="flex items-center gap-2">
                        {getProviderIcon('Azure')}
                        <span>Azure OpenAI</span>
                      </div>
                    </SelectItem>
                  </SelectContent>
                </Select>
              )}
              {defaultProvider && (
                <p className="text-xs text-muted-foreground">
                  Locked to {getProviderDisplayName(provider)} for this model
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                placeholder="My OpenAI Key"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="apiKey">API Key</Label>
              <Input
                id="apiKey"
                type="password"
                placeholder="sk-..."
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                required
              />
              <p className="text-xs text-muted-foreground">
                Your API key will be encrypted before storage
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
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Creating...' : 'Create Credential'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
