import { useState, useEffect } from 'react'
import { Plus, Trash2, Pencil } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@donkeywork/ui'
import { CreateSandboxCredentialMappingDialog } from '@/components/sandbox-credentials/CreateSandboxCredentialMappingDialog'
import { EditSandboxCredentialMappingDialog } from '@/components/sandbox-credentials/EditSandboxCredentialMappingDialog'
import { sandboxCredentialMappings, type SandboxCredentialMapping } from '@donkeywork/api-client'

const fieldTypeLabels: Record<string, string> = {
  ApiKey: 'API Key',
  AccessToken: 'Access Token',
  RefreshToken: 'Refresh Token',
  Username: 'Username',
  Password: 'Password',
  ClientId: 'Client ID',
  ClientSecret: 'Client Secret',
  WebhookSecret: 'Webhook Secret',
  Custom: 'Custom',
}

const credentialTypeLabels: Record<string, string> = {
  ExternalApiKey: 'API Key',
  OAuthToken: 'OAuth Token',
}

export function SandboxCredentialsPage() {
  const [mappings, setMappings] = useState<SandboxCredentialMapping[]>([])
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingMapping, setEditingMapping] = useState<SandboxCredentialMapping | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const loadMappings = async () => {
    try {
      setLoading(true)
      const data = await sandboxCredentialMappings.list()
      setMappings(data)
    } catch (error) {
      console.error('Failed to load sandbox credential mappings:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadMappings()
  }, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this mapping? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await sandboxCredentialMappings.delete(id)
      setMappings(prev => prev.filter(m => m.id !== id))
    } catch (error) {
      console.error('Failed to delete mapping:', error)
      alert('Failed to delete mapping')
    } finally {
      setDeletingId(null)
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Sandbox Credentials</h1>
          <p className="text-muted-foreground">
            Map domains to credentials for sandbox code execution
          </p>
        </div>
      </div>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Domain Mappings</h2>
          <Button onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Mapping
          </Button>
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-lg border border-border p-12">
            <p className="text-sm text-muted-foreground">Loading mappings...</p>
          </div>
        ) : mappings.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <Plus className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No mappings yet</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Add a domain mapping so sandbox code can authenticate with external APIs
            </p>
            <Button className="mt-4" onClick={() => setIsCreateDialogOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Add Mapping
            </Button>
          </div>
        ) : (
          <>
            {/* Mobile view - card layout */}
            <div className="space-y-3 md:hidden">
              {mappings.map((mapping) => (
                <div key={mapping.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-1 min-w-0 flex-1">
                      <div className="text-sm font-medium">{mapping.baseDomain}</div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Header: </span>
                        <span>{mapping.headerName}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Type: </span>
                        <span>{credentialTypeLabels[mapping.credentialType] ?? mapping.credentialType}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Field: </span>
                        <span>{fieldTypeLabels[mapping.credentialFieldType] ?? mapping.credentialFieldType}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Created: </span>
                        <span>{formatDate(mapping.createdAt)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setEditingMapping(mapping)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(mapping.id)}
                        disabled={deletingId === mapping.id}
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
                    <TableHead>Domain</TableHead>
                    <TableHead>Header</TableHead>
                    <TableHead>Credential Type</TableHead>
                    <TableHead>Field</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead className="w-[100px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mappings.map((mapping) => (
                    <TableRow key={mapping.id}>
                      <TableCell className="font-medium">{mapping.baseDomain}</TableCell>
                      <TableCell>{mapping.headerName}</TableCell>
                      <TableCell>{credentialTypeLabels[mapping.credentialType] ?? mapping.credentialType}</TableCell>
                      <TableCell>{fieldTypeLabels[mapping.credentialFieldType] ?? mapping.credentialFieldType}</TableCell>
                      <TableCell>{formatDate(mapping.createdAt)}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setEditingMapping(mapping)}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(mapping.id)}
                            disabled={deletingId === mapping.id}
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

      <CreateSandboxCredentialMappingDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={loadMappings}
      />

      {editingMapping && (
        <EditSandboxCredentialMappingDialog
          open={!!editingMapping}
          onOpenChange={(open) => { if (!open) setEditingMapping(null) }}
          mapping={editingMapping}
          onUpdated={loadMappings}
        />
      )}
    </div>
  )
}
