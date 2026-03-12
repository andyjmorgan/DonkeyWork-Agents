import { useState, useEffect } from 'react'
import { Plus, Trash2, Pencil, Server, Terminal, Globe, Zap } from 'lucide-react'
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
import { McpServerDialog } from '@/components/mcp/McpServerDialog'
import { McpServerTestDialog } from '@/components/mcp/McpServerTestDialog'
import { mcpServers, type McpServerSummary, type McpServerDetails } from '@donkeywork/api-client'

export function McpServersPage() {
  const [servers, setServers] = useState<McpServerSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [editingServer, setEditingServer] = useState<McpServerDetails | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [testingServer, setTestingServer] = useState<{ id: string; name: string } | null>(null)

  const loadServers = async () => {
    try {
      setLoading(true)
      const data = await mcpServers.list()
      setServers(data)
    } catch (error) {
      console.error('Failed to load MCP servers:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadServers()
  }, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this MCP server? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await mcpServers.delete(id)
      setServers(prev => prev.filter(s => s.id !== id))
    } catch (error) {
      console.error('Failed to delete MCP server:', error)
      alert('Failed to delete MCP server')
    } finally {
      setDeletingId(null)
    }
  }

  const handleEdit = async (id: string) => {
    try {
      const server = await mcpServers.get(id)
      setEditingServer(server)
      setIsDialogOpen(true)
    } catch (error) {
      console.error('Failed to load MCP server:', error)
      alert('Failed to load MCP server details')
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

  const getTransportIcon = (transportType: string) => {
    return transportType === 'Stdio'
      ? <Terminal className="h-4 w-4" />
      : <Globe className="h-4 w-4" />
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
          <h1 className="text-2xl font-bold">MCP Servers</h1>
          <p className="text-muted-foreground">
            Connect to external MCP servers to extend your agents with additional tools
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
            <p className="text-sm text-muted-foreground">Loading MCP servers...</p>
          </div>
        ) : servers.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <Server className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No MCP servers configured</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Add your first MCP server to give your agents access to external tools and capabilities
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
                        {getTransportIcon(server.transportType)}
                        <span className="text-sm font-medium">{server.name}</span>
                        {server.connectToNavi && (
                          <Badge variant="outline" className="text-xs px-1.5 py-0">Navi</Badge>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={server.transportType === 'Stdio' ? 'secondary' : 'outline'}>
                          {server.transportType}
                        </Badge>
                        <Badge variant={server.isEnabled ? 'default' : 'secondary'}>
                          {server.isEnabled ? 'Enabled' : 'Disabled'}
                        </Badge>
                      </div>
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
                      {server.transportType === 'Http' && (
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setTestingServer({ id: server.id, name: server.name })}
                        >
                          <Zap className="h-4 w-4" />
                        </Button>
                      )}
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
                    <TableHead>Type</TableHead>
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
                          {getTransportIcon(server.transportType)}
                          <span className="font-medium">{server.name}</span>
                          {server.connectToNavi && (
                            <Badge variant="outline" className="text-xs px-1.5 py-0">Navi</Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={server.transportType === 'Stdio' ? 'secondary' : 'outline'}>
                          {server.transportType}
                        </Badge>
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
                          {server.transportType === 'Http' && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => setTestingServer({ id: server.id, name: server.name })}
                            >
                              <Zap className="h-4 w-4" />
                            </Button>
                          )}
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

      <McpServerDialog
        open={isDialogOpen}
        onOpenChange={handleDialogClose}
        onSaved={handleSaved}
        editingServer={editingServer}
      />

      {testingServer && (
        <McpServerTestDialog
          open={!!testingServer}
          onOpenChange={(open) => { if (!open) setTestingServer(null) }}
          serverId={testingServer.id}
          serverName={testingServer.name}
        />
      )}
    </div>
  )
}
