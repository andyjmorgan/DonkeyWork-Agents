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
import { agentDefinitions, type AgentDefinitionSummary } from '@donkeywork/api-client'

export function AgentDefinitionsPage() {
  const navigate = useNavigate()
  const [isCreating, setIsCreating] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [agents, setAgents] = useState<AgentDefinitionSummary[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    loadAgents()
  }, [])

  const loadAgents = async () => {
    try {
      setIsLoading(true)
      const data = await agentDefinitions.list()
      setAgents(data)
    } catch (error) {
      console.error('Failed to load agent definitions:', error)
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = async () => {
    try {
      setIsCreating(true)
      const response = await agentDefinitions.create({
        name: `agent_${Date.now()}`,
        description: '',
      })
      navigate(`/agent-definitions/${response.id}/edit`)
    } catch (error) {
      console.error('Failed to create agent definition:', error)
    } finally {
      setIsCreating(false)
    }
  }

  const handleEdit = (id: string) => {
    navigate(`/agent-definitions/${id}/edit`)
  }

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(`Are you sure you want to delete "${name}"? This action cannot be undone.`)) {
      return
    }
    try {
      setDeletingId(id)
      await agentDefinitions.delete(id)
      await loadAgents()
    } catch (error) {
      console.error('Failed to delete agent definition:', error)
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
          <h1 className="text-2xl font-bold">Agent Definitions</h1>
          <p className="text-muted-foreground">Create and manage your AI agent configurations</p>
        </div>
        <Button onClick={handleCreate} disabled={isCreating}>
          {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
          <span className="hidden sm:inline">New Agent</span>
        </Button>
      </div>

      {agents.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No agent definitions yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Get started by creating your first agent definition
          </p>
          <Button className="mt-4" onClick={handleCreate} disabled={isCreating}>
            {isCreating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
            Create Agent Definition
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {agents.map((agent) => (
              <div key={agent.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="space-y-1 min-w-0 flex-1">
                    <div className="text-sm">
                      <span className="text-muted-foreground">Name: </span>
                      <span className="font-medium">{agent.name}</span>
                    </div>
                    {agent.description && (
                      <div className="text-sm">
                        <span className="text-muted-foreground">Description: </span>
                        <span>{agent.description}</span>
                      </div>
                    )}
                    <div className="text-sm flex items-center gap-2">
                      <span className="text-muted-foreground">Type: </span>
                      {agent.isSystem ? (
                        <Badge variant="secondary">System</Badge>
                      ) : (
                        <Badge variant="outline">Custom</Badge>
                      )}
                    </div>
                    <div className="text-sm">
                      <span className="text-muted-foreground">Created: </span>
                      <span>{new Date(agent.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 shrink-0">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => handleEdit(agent.id)}
                    >
                      <Edit className="h-4 w-4" />
                    </Button>
                    {!agent.isSystem && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-destructive"
                        onClick={() => handleDelete(agent.id, agent.name)}
                        disabled={deletingId === agent.id}
                      >
                        {deletingId === agent.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="h-4 w-4" />
                        )}
                      </Button>
                    )}
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
                  <TableHead>Type</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {agents.map((agent) => (
                  <TableRow key={agent.id}>
                    <TableCell className="font-medium">{agent.name}</TableCell>
                    <TableCell className="max-w-md">
                      {agent.description || (
                        <span className="text-muted-foreground italic">No description</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {agent.isSystem ? (
                        <Badge variant="secondary">System</Badge>
                      ) : (
                        <Badge variant="outline">Custom</Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(agent.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8"
                          onClick={() => handleEdit(agent.id)}
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        {!agent.isSystem && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                            onClick={() => handleDelete(agent.id, agent.name)}
                            disabled={deletingId === agent.id}
                          >
                            {deletingId === agent.id ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Trash2 className="h-4 w-4" />
                            )}
                          </Button>
                        )}
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
