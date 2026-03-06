import { useState, useEffect } from 'react'
import { Plus, Key, Trash2, Copy, Check, Loader2, Eye, ChevronLeft, ChevronRight } from 'lucide-react'
import {
  Button,
  Input,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@donkeywork/ui'
import { apiKeys, type ApiKeyItem } from '@/lib/api'

const PAGE_SIZE = 20

export function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKeyItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(0)
  const [loading, setLoading] = useState(true)
  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [newKeyName, setNewKeyName] = useState('')
  const [newKeyDescription, setNewKeyDescription] = useState('')
  const [creating, setCreating] = useState(false)
  const [createdKey, setCreatedKey] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [viewKey, setViewKey] = useState<{ id: string; name: string; key: string } | null>(null)
  const [loadingKey, setLoadingKey] = useState<string | null>(null)

  const loadKeys = async (offset = 0) => {
    setLoading(true)
    try {
      const response = await apiKeys.list(offset, PAGE_SIZE)
      setKeys(response.items)
      setTotalCount(response.totalCount)
    } catch (error) {
      console.error('Failed to load API keys:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadKeys(page * PAGE_SIZE)
  }, [page])

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

  const handleCreate = async () => {
    if (!newKeyName.trim()) return

    setCreating(true)
    try {
      const response = await apiKeys.create({
        name: newKeyName.trim(),
        description: newKeyDescription.trim() || undefined,
      })
      setCreatedKey(response.key)
      setNewKeyName('')
      setNewKeyDescription('')
      loadKeys()
    } catch (error) {
      console.error('Failed to create API key:', error)
    } finally {
      setCreating(false)
    }
  }

  const handleCopy = async (key: string) => {
    await navigator.clipboard.writeText(key)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleCloseCreateDialog = () => {
    setCreateDialogOpen(false)
    setCreatedKey(null)
    setNewKeyName('')
    setNewKeyDescription('')
  }

  const handleView = async (id: string, name: string) => {
    setLoadingKey(id)
    try {
      const response = await apiKeys.get(id)
      setViewKey({ id, name, key: response.key })
    } catch (error) {
      console.error('Failed to get API key:', error)
    } finally {
      setLoadingKey(null)
    }
  }

  const handleDelete = async () => {
    if (!deleteId) return

    setDeleting(true)
    try {
      await apiKeys.delete(deleteId)
      setKeys(keys.filter(k => k.id !== deleteId))
      setDeleteId(null)
    } catch (error) {
      console.error('Failed to delete API key:', error)
    } finally {
      setDeleting(false)
    }
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">API Keys</h1>
          <p className="text-muted-foreground">
            Keys for programmatic access to the API
          </p>
        </div>
        <Button onClick={() => setCreateDialogOpen(true)}>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">Create Key</span>
        </Button>
      </div>

      {keys.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Key className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No API keys yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Create an API key to access the API programmatically
          </p>
          <Button className="mt-4" onClick={() => setCreateDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Create Key
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {keys.map((key) => (
              <div
                key={key.id}
                className="rounded-lg border border-border bg-card p-4 space-y-2"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="text-sm">
                      <span className="text-muted-foreground">Name: </span>
                      <span className="font-medium">{key.name}</span>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Key: </span>
                      <span className="font-mono">{key.maskedKey}</span>
                    </div>
                    {key.description && (
                      <div className="text-sm">
                        <span className="text-muted-foreground">Description: </span>
                        <span>{key.description}</span>
                      </div>
                    )}
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{formatDate(key.createdAt)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleView(key.id, key.name)}
                      disabled={loadingKey === key.id}
                    >
                      {loadingKey === key.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Eye className="h-4 w-4" />
                      )}
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                      onClick={() => setDeleteId(key.id)}
                    >
                      <Trash2 className="h-4 w-4" />
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
                  <TableHead>Key</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {keys.map((key) => (
                  <TableRow key={key.id}>
                    <TableCell className="font-medium">{key.name}</TableCell>
                    <TableCell className="font-mono text-muted-foreground">
                      {key.maskedKey}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {key.description || '-'}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {formatDate(key.createdAt)}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleView(key.id, key.name)}
                          disabled={loadingKey === key.id}
                        >
                          {loadingKey === key.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Eye className="h-4 w-4" />
                          )}
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => setDeleteId(key.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <p className="text-sm text-muted-foreground">
                Showing {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p - 1)}
                  disabled={!canGoBack || loading}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage(p => p + 1)}
                  disabled={!canGoForward || loading}
                >
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Create Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={handleCloseCreateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {createdKey ? 'API Key Created' : 'Create API Key'}
            </DialogTitle>
            <DialogDescription>
              {createdKey
                ? 'Copy your API key now. You won\'t be able to see it again.'
                : 'Give your API key a name to help you identify it later.'}
            </DialogDescription>
          </DialogHeader>

          {createdKey ? (
            <div className="space-y-4">
              <div className="flex items-center gap-2">
                <Input
                  value={createdKey}
                  readOnly
                  className="font-mono text-sm"
                />
                <Button
                  variant="outline"
                  size="icon"
                  onClick={() => handleCopy(createdKey)}
                >
                  {copied ? (
                    <Check className="h-4 w-4 text-green-500" />
                  ) : (
                    <Copy className="h-4 w-4" />
                  )}
                </Button>
              </div>
              <DialogFooter>
                <Button onClick={handleCloseCreateDialog}>Done</Button>
              </DialogFooter>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">Name</label>
                <Input
                  placeholder="My API Key"
                  value={newKeyName}
                  onChange={(e) => setNewKeyName(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  Description <span className="text-muted-foreground">(optional)</span>
                </label>
                <Input
                  placeholder="Used for..."
                  value={newKeyDescription}
                  onChange={(e) => setNewKeyDescription(e.target.value)}
                />
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={handleCloseCreateDialog}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleCreate}
                  disabled={!newKeyName.trim() || creating}
                >
                  {creating && <Loader2 className="h-4 w-4 animate-spin" />}
                  Create
                </Button>
              </DialogFooter>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* View Key Dialog */}
      <Dialog open={!!viewKey} onOpenChange={() => setViewKey(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{viewKey?.name}</DialogTitle>
            <DialogDescription>
              Your API key value. Keep it secure.
            </DialogDescription>
          </DialogHeader>
          <div className="flex items-center gap-2">
            <Input
              value={viewKey?.key || ''}
              readOnly
              className="font-mono text-sm"
            />
            <Button
              variant="outline"
              size="icon"
              onClick={() => viewKey && handleCopy(viewKey.key)}
            >
              {copied ? (
                <Check className="h-4 w-4 text-green-500" />
              ) : (
                <Copy className="h-4 w-4" />
              )}
            </Button>
          </div>
          <DialogFooter>
            <Button onClick={() => setViewKey(null)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteId} onOpenChange={() => setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete API Key</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this API key? Any applications using this key will lose access immediately.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteId(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleting}
            >
              {deleting && <Loader2 className="h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
