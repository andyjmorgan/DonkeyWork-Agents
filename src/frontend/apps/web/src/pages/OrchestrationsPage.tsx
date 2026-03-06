import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
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
import { orchestrations, type Orchestration } from '@/lib/api'

interface OrchestrationWithVersion extends Orchestration {
  versionNumber?: number
}

export function OrchestrationsPage() {
  const navigate = useNavigate()
  const [isCreating, setIsCreating] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [orchestrationsList, setOrchestrationsList] = useState<OrchestrationWithVersion[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

  // Fetch orchestrations on mount
  useEffect(() => {
    loadOrchestrations()
  }, [])

  const loadOrchestrations = async () => {
    try {
      setIsLoading(true)
      const data = await orchestrations.list()

      // Fetch version numbers for orchestrations that have a current version
      const orchestrationsWithVersions = await Promise.all(
        data.map(async (orchestration) => {
          if (orchestration.currentVersionId) {
            try {
              const versions = await orchestrations.listVersions(orchestration.id)
              const currentVersion = versions.find(v => v.id === orchestration.currentVersionId)
              return {
                ...orchestration,
                versionNumber: currentVersion?.versionNumber
              }
            } catch (error) {
              console.error(`Failed to load version for orchestration ${orchestration.id}:`, error)
              return orchestration
            }
          }
          return orchestration
        })
      )

      setOrchestrationsList(orchestrationsWithVersions)
    } catch (error) {
      console.error('Failed to load orchestrations:', error)
      // TODO: Show error toast
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = async () => {
    try {
      setIsCreating(true)

      // Create orchestration with default name
      const response = await orchestrations.create({
        name: `orchestration_${Date.now()}`,
        description: ''
      })

      // Navigate to editor with returned ID
      navigate(`/orchestrations/${response.id}/edit`)
    } catch (error) {
      console.error('Failed to create orchestration:', error)
      // TODO: Show error toast
    } finally {
      setIsCreating(false)
    }
  }

  const handleEdit = (orchestrationId: string) => {
    navigate(`/orchestrations/${orchestrationId}/edit`)
  }

  const handleDelete = async (orchestrationId: string, orchestrationName: string) => {
    if (!window.confirm(`Are you sure you want to delete "${orchestrationName}"? This action cannot be undone.`)) {
      return
    }

    try {
      setDeletingId(orchestrationId)
      await orchestrations.delete(orchestrationId)
      // Reload the list
      await loadOrchestrations()
      // TODO: Show success toast
    } catch (error) {
      console.error('Failed to delete orchestration:', error)
      // TODO: Show error toast
    } finally {
      setDeletingId(null)
    }
  }

  if (isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Orchestrations</h1>
          <p className="text-muted-foreground">
            Create and manage your AI orchestrations
          </p>
        </div>
        <Button onClick={handleCreate} disabled={isCreating}>
          {isCreating ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Plus className="h-4 w-4" />
          )}
          <span className="hidden sm:inline">New Orchestration</span>
        </Button>
      </div>

      {orchestrationsList.length === 0 ? (
        /* Empty state */
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No orchestrations yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Get started by creating your first orchestration
          </p>
          <Button className="mt-4" onClick={handleCreate} disabled={isCreating}>
            {isCreating ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
            Create Orchestration
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {orchestrationsList.map((orchestration) => (
              <div key={orchestration.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="text-sm">
                      <span className="text-muted-foreground">Name: </span>
                      <span className="font-medium">{orchestration.name}</span>
                    </div>
                    {orchestration.description && (
                      <div className="text-sm">
                        <span className="text-muted-foreground">Description: </span>
                        <span>{orchestration.description}</span>
                      </div>
                    )}
                    <div className="text-sm">
                      <span className="text-muted-foreground">Status: </span>
                      {orchestration.currentVersionId ? (
                        <span className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">
                            Published
                          </Badge>
                          {orchestration.versionNumber && (
                            <span className="text-xs">v{orchestration.versionNumber}</span>
                          )}
                        </span>
                      ) : (
                        <Badge variant="secondary" className="text-xs">
                          Draft
                        </Badge>
                      )}
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{new Date(orchestration.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleEdit(orchestration.id)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive"
                      onClick={() => handleDelete(orchestration.id, orchestration.name)}
                      disabled={deletingId === orchestration.id}
                    >
                      {deletingId === orchestration.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
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
                  <TableHead>Description</TableHead>
                  <TableHead>Version</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {orchestrationsList.map((orchestration) => (
                  <TableRow key={orchestration.id}>
                    <TableCell className="font-medium">{orchestration.name}</TableCell>
                    <TableCell className="max-w-md">
                      {orchestration.description || (
                        <span className="text-muted-foreground italic">No description</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {orchestration.versionNumber ? (
                        <span className="font-mono text-sm">v{orchestration.versionNumber}</span>
                      ) : (
                        <span className="text-muted-foreground text-sm">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {orchestration.currentVersionId ? (
                        <Badge variant="outline">Published</Badge>
                      ) : (
                        <Badge variant="secondary">Draft</Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(orchestration.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleEdit(orchestration.id)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={() => handleDelete(orchestration.id, orchestration.name)}
                          disabled={deletingId === orchestration.id}
                        >
                          {deletingId === orchestration.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4" />
                          )}
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
    </div>
  )
}
