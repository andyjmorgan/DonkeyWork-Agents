import { useState, useEffect } from 'react'
import { Plus, Trash2, Pencil, Globe, Zap } from 'lucide-react'
import {
  Button,
  Badge,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@donkeywork/ui'
import { A2aServerDialog } from '@/components/a2a/A2aServerDialog'
import { A2aServerTestDialog } from '@/components/a2a/A2aServerTestDialog'
import { a2aServers, type A2aServerSummary, type A2aServerDetails } from '@donkeywork/api-client'

export function A2aServersPage() {
  const [servers, setServers] = useState<A2aServerSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [editingServer, setEditingServer] = useState<A2aServerDetails | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [testingServer, setTestingServer] = useState<{ id: string; name: string } | null>(null)

  const loadServers = async () => {
    try {
      setLoading(true)
      const data = await a2aServers.list()
      setServers(data)
    } catch (error) {
      console.error('Failed to load A2A servers:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadServers()
  }, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this A2A server? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await a2aServers.delete(id)
      setServers(prev => prev.filter(s => s.id !== id))
    } catch (error) {
      console.error('Failed to delete A2A server:', error)
      alert('Failed to delete A2A server')
    } finally {
      setDeletingId(null)
    }
  }

  const handleEdit = async (id: string) => {
    try {
      const server = await a2aServers.get(id)
      setEditingServer(server)
      setIsDialogOpen(true)
    } catch (error) {
      console.error('Failed to load A2A server:', error)
      alert('Failed to load A2A server details')
    }
  }

  const handleCreate = () => {
    setEditingServer(null)
    setIsDialogOpen(true)
  }

  const handleDialogClose = () => {
    setIsDialogOpen(false)
    setEditingServer(null)
  }

  const handleSaved = () => {
    loadServers()
    handleDialogClose()
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
          <h1 className="text-2xl font-bold">A2A Servers</h1>
          <p className="text-muted-foreground">
            Connect to external A2A-compatible agents for cross-agent collaboration
          </p>
        </div>
      </div>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Server Configurations</h2>
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Add Server
          </Button>
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-lg border border-border p-12">
            <p className="text-sm text-muted-foreground">Loading A2A servers...</p>
          </div>
        ) : servers.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <Globe className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No A2A servers configured</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Add your first A2A server to enable cross-agent collaboration with external agents
            </p>
            <Button className="mt-4" onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              Add Server
            </Button>
          </div>
        ) : (
          <>
            {/* Mobile view - card layout */}
            <div className="space-y-3 md:hidden">
              {servers.map((server) => (
                <div key={server.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-2 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <Globe className="h-4 w-4" />
                        <span className="text-sm font-medium">{server.name}</span>
                        {server.connectToNavi && (
                          <Badge variant="outline" className="text-xs px-1.5 py-0">Navi</Badge>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={server.isEnabled ? 'default' : 'secondary'}>
                          {server.isEnabled ? 'Enabled' : 'Disabled'}
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground truncate">{server.address}</p>
                      {server.description && (
                        <p className="text-sm text-muted-foreground line-clamp-2">
                          {server.description}
                        </p>
                      )}
                      <div className="text-sm">
                        <span className="text-muted-foreground">Created: </span>
                        <span>{formatDate(server.createdAt)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setTestingServer({ id: server.id, name: server.name })}
                      >
                        <Zap className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleEdit(server.id)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(server.id)}
                        disabled={deletingId === server.id}
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
                    <TableHead>Address</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead className="w-[100px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {servers.map((server) => (
                    <TableRow key={server.id}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Globe className="h-4 w-4" />
                          <span className="font-medium">{server.name}</span>
                          {server.connectToNavi && (
                            <Badge variant="outline" className="text-xs px-1.5 py-0">Navi</Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="max-w-xs">
                        <span className="text-muted-foreground truncate block font-mono text-xs">
                          {server.address}
                        </span>
                      </TableCell>
                      <TableCell>
                        <Badge variant={server.isEnabled ? 'default' : 'secondary'}>
                          {server.isEnabled ? 'Enabled' : 'Disabled'}
                        </Badge>
                      </TableCell>
                      <TableCell className="max-w-xs">
                        <span className="text-muted-foreground truncate block">
                          {server.description || '-'}
                        </span>
                      </TableCell>
                      <TableCell>{formatDate(server.createdAt)}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setTestingServer({ id: server.id, name: server.name })}
                          >
                            <Zap className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleEdit(server.id)}
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(server.id)}
                            disabled={deletingId === server.id}
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

      <A2aServerDialog
        open={isDialogOpen}
        onOpenChange={handleDialogClose}
        onSaved={handleSaved}
        editingServer={editingServer}
      />

      {testingServer && (
        <A2aServerTestDialog
          open={!!testingServer}
          onOpenChange={(open) => { if (!open) setTestingServer(null) }}
          serverId={testingServer.id}
          serverName={testingServer.name}
        />
      )}
    </div>
  )
}
