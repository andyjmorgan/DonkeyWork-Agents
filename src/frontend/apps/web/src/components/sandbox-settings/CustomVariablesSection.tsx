import { useState, useEffect } from 'react'
import { Plus, Trash2, Pencil, Eye, EyeOff } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Badge,
} from '@donkeywork/ui'
import { sandboxCustomVariables, type SandboxCustomVariable } from '@donkeywork/api-client'
import { CreateCustomVariableDialog } from './CreateCustomVariableDialog'
import { EditCustomVariableDialog } from './EditCustomVariableDialog'

export function CustomVariablesSection() {
  const [variables, setVariables] = useState<SandboxCustomVariable[]>([])
  const [loading, setLoading] = useState(true)
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingVariable, setEditingVariable] = useState<SandboxCustomVariable | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  const loadVariables = async () => {
    try {
      setLoading(true)
      const data = await sandboxCustomVariables.list()
      setVariables(data)
    } catch (error) {
      console.error('Failed to load sandbox custom variables:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadVariables()
  }, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this variable? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await sandboxCustomVariables.delete(id)
      setVariables(prev => prev.filter(v => v.id !== id))
    } catch (error) {
      console.error('Failed to delete variable:', error)
      alert('Failed to delete variable')
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
    <section className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold">Custom Variables</h2>
          <p className="text-sm text-muted-foreground">
            Environment variables injected into sandbox pods at runtime
          </p>
        </div>
        <Button onClick={() => setIsCreateDialogOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Add Variable
        </Button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center rounded-lg border border-border p-12">
          <p className="text-sm text-muted-foreground">Loading variables...</p>
        </div>
      ) : variables.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No custom variables yet</h3>
          <p className="mt-2 text-sm text-muted-foreground max-w-sm">
            Add environment variables that will be available in your sandbox execution environment
          </p>
          <Button className="mt-4" onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add Variable
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {variables.map((variable) => (
              <div key={variable.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium font-mono">{variable.key}</span>
                      {variable.isSecret && <Badge variant="secondary">Secret</Badge>}
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Value: </span>
                      <span className="font-mono">
                        {variable.isSecret ? '********' : variable.value}
                      </span>
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{formatDate(variable.createdAt)}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setEditingVariable(variable)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDelete(variable.id)}
                      disabled={deletingId === variable.id}
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
                  <TableHead>Key</TableHead>
                  <TableHead>Value</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {variables.map((variable) => (
                  <TableRow key={variable.id}>
                    <TableCell className="font-medium font-mono">{variable.key}</TableCell>
                    <TableCell className="font-mono">
                      {variable.isSecret ? (
                        <span className="text-muted-foreground">********</span>
                      ) : (
                        variable.value
                      )}
                    </TableCell>
                    <TableCell>
                      {variable.isSecret ? (
                        <Badge variant="secondary">
                          <EyeOff className="h-3 w-3 mr-1" />
                          Secret
                        </Badge>
                      ) : (
                        <Badge variant="outline">
                          <Eye className="h-3 w-3 mr-1" />
                          Visible
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>{formatDate(variable.createdAt)}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => setEditingVariable(variable)}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDelete(variable.id)}
                          disabled={deletingId === variable.id}
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

      <CreateCustomVariableDialog
        open={isCreateDialogOpen}
        onOpenChange={setIsCreateDialogOpen}
        onCreated={loadVariables}
      />

      {editingVariable && (
        <EditCustomVariableDialog
          open={!!editingVariable}
          onOpenChange={(open) => { if (!open) setEditingVariable(null) }}
          variable={editingVariable}
          onUpdated={loadVariables}
        />
      )}
    </section>
  )
}
