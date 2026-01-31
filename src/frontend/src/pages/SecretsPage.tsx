import { useState, useEffect } from 'react'
import { Plus, Trash2, Eye, Link as LinkIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { CreateCredentialDialog } from '@/components/credentials/CreateCredentialDialog'
import { ViewCredentialDialog } from '@/components/credentials/ViewCredentialDialog'
import { CreateOAuthConfigDialog } from '@/components/oauth/CreateOAuthConfigDialog'
import { OAuthTokenList } from '@/components/oauth/OAuthTokenList'
import { ProviderIcon } from '@/components/oauth/ProviderIcon'
import { credentials, oauth, type CredentialSummary, type CredentialDetail, type OAuthProviderConfig, type OAuthToken } from '@/lib/api'
import { useOAuthFlow } from '@/hooks/useOAuthFlow'
import { OpenAIIcon } from '@/components/icons/OpenAIIcon'
import { AnthropicIcon } from '@/components/icons/AnthropicIcon'
import { GoogleIcon } from '@/components/icons/GoogleIcon'

export function SecretsPage() {
  const [allCredentials, setAllCredentials] = useState<CredentialSummary[]>([])
  const [oauthConfigs, setOAuthConfigs] = useState<OAuthProviderConfig[]>([])
  const [oauthTokens, setOAuthTokens] = useState<OAuthToken[]>([])
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [isCreateOAuthConfigDialogOpen, setIsCreateOAuthConfigDialogOpen] = useState(false)
  const [isViewDialogOpen, setIsViewDialogOpen] = useState(false)
  const [viewingCredential, setViewingCredential] = useState<CredentialDetail | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const { initiateFlow, disconnect, refresh } = useOAuthFlow()

  const loadCredentials = async () => {
    try {
      setLoading(true)
      const data = await credentials.list()
      setAllCredentials(data)
    } catch (error) {
      console.error('Failed to load credentials:', error)
    } finally {
      setLoading(false)
    }
  }

  const loadOAuthData = async () => {
    try {
      const [configs, tokens] = await Promise.all([
        oauth.listConfigs(),
        oauth.listTokens()
      ])
      setOAuthConfigs(configs)
      setOAuthTokens(tokens)
    } catch (error) {
      console.error('Failed to load OAuth data:', error)
    }
  }

  useEffect(() => {
    loadCredentials()
    loadOAuthData()
  }, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this credential? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await credentials.delete(id)
      setAllCredentials(prev => prev.filter(c => c.id !== id))
    } catch (error) {
      console.error('Failed to delete credential:', error)
      alert('Failed to delete credential')
    } finally {
      setDeletingId(null)
    }
  }

  const handleCredentialCreated = () => {
    loadCredentials()
  }

  const handleOAuthConfigCreated = () => {
    loadOAuthData()
  }

  const handleConnect = async (providerId: string) => {
    const provider = oauthConfigs.find(c => c.id === providerId)?.provider
    if (!provider) return

    try {
      await initiateFlow(provider)
    } catch (error) {
      alert('Failed to initiate OAuth flow')
    }
  }

  const handleDisconnect = async (tokenId: string) => {
    if (!confirm('Are you sure you want to disconnect this account?')) {
      return
    }

    try {
      await disconnect(tokenId)
      loadOAuthData()
    } catch (error) {
      alert('Failed to disconnect account')
    }
  }

  const handleRefresh = async (tokenId: string) => {
    try {
      await refresh(tokenId)
      loadOAuthData()
    } catch (error) {
      alert('Failed to refresh token')
    }
  }

  const handleDeleteOAuthConfig = async (configId: string) => {
    if (!confirm('Are you sure you want to delete this OAuth configuration?')) {
      return
    }

    try {
      await oauth.deleteConfig(configId)
      loadOAuthData()
    } catch (error) {
      alert('Failed to delete OAuth configuration')
    }
  }

  const handleView = async (id: string) => {
    try {
      const credential = await credentials.get(id)
      setViewingCredential(credential)
      setIsViewDialogOpen(true)
    } catch (error) {
      console.error('Failed to load credential:', error)
      alert('Failed to load credential details')
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
        return null // TODO: Add Azure icon
      default:
        return null
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Secrets & Credentials</h1>
          <p className="text-muted-foreground">
            Manage API keys and OAuth connections
          </p>
        </div>
      </div>

      {/* API Keys Section */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">API Keys</h2>
          <Button onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add API Key
          </Button>
        </div>

      {loading ? (
        <div className="flex items-center justify-center rounded-lg border border-border p-12">
          <p className="text-sm text-muted-foreground">Loading credentials...</p>
        </div>
      ) : allCredentials.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No credentials yet</h3>
          <p className="mt-2 text-sm text-muted-foreground max-w-sm">
            Add your first API key to start using LLM models in your agents
          </p>
          <Button className="mt-4" onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Credential
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {allCredentials.map((credential) => (
              <div key={credential.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      {getProviderIcon(credential.provider)}
                      <span className="text-sm font-medium">{credential.name}</span>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Provider: </span>
                      <span>{credential.provider}</span>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{formatDate(credential.createdAt)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleView(credential.id)}
                    >
                      <Eye className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDelete(credential.id)}
                      disabled={deletingId === credential.id}
                    >
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop view - table layout */}
          <div className="hidden md:block rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Provider</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {allCredentials.map((credential) => (
                  <TableRow key={credential.id}>
                    <TableCell className="font-medium">{credential.name}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {getProviderIcon(credential.provider)}
                        <span>{credential.provider}</span>
                      </div>
                    </TableCell>
                    <TableCell>{formatDate(credential.createdAt)}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleView(credential.id)}
                        >
                          <Eye className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(credential.id)}
                          disabled={deletingId === credential.id}
                        >
                          <Trash2 className="h-4 w-4 text-red-500" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </>
      )}
      </section>

      {/* OAuth Clients Section */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">OAuth Clients</h2>
          <Button onClick={() => setIsCreateOAuthConfigDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add OAuth Client
          </Button>
        </div>

        {oauthConfigs.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-muted/50 p-8 text-center">
            <p className="text-sm text-muted-foreground">
              No OAuth clients configured. Add one to connect external accounts.
            </p>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {oauthConfigs.map((config) => (
              <Card key={config.id}>
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <ProviderIcon provider={config.provider} />
                      <CardTitle className="text-base">{config.provider}</CardTitle>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDeleteOAuthConfig(config.id)}
                    >
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                  <CardDescription className="text-xs truncate">
                    {config.redirectUri}
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {!config.hasToken ? (
                    <Button
                      size="sm"
                      className="w-full"
                      onClick={() => handleConnect(config.id)}
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
      </section>

      {/* Connected Accounts Section */}
      <section className="space-y-4">
        <h2 className="text-lg font-semibold">Connected Accounts</h2>
        <OAuthTokenList
          tokens={oauthTokens}
          onRefresh={handleRefresh}
          onDisconnect={handleDisconnect}
        />
      </section>

      <CreateCredentialDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={handleCredentialCreated}
      />

      <CreateOAuthConfigDialog
        open={isCreateOAuthConfigDialogOpen}
        onOpenChange={setIsCreateOAuthConfigDialogOpen}
        onSuccess={handleOAuthConfigCreated}
      />

      {viewingCredential && (
        <ViewCredentialDialog
          open={isViewDialogOpen}
          onOpenChange={setIsViewDialogOpen}
          credentialId={viewingCredential.id}
          credentialName={viewingCredential.name}
          provider={viewingCredential.provider}
          apiKey={viewingCredential.apiKey}
          createdAt={viewingCredential.createdAt}
        />
      )}
    </div>
  )
}
