import { useState, useEffect } from 'react'
import { Plus, Loader2, Edit, Trash2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { agents, type Agent } from '@/lib/api'

interface AgentWithVersion extends Agent {
  versionNumber?: number
}

export function AgentsPage() {
  const navigate = useNavigate()
  const [isCreating, setIsCreating] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [agentsList, setAgentsList] = useState<AgentWithVersion[]>([])
  const [deletingId, setDeletingId] = useState<string | null>(null)

  // Fetch agents on mount
  useEffect(() => {
    loadAgents()
  }, [])

  const loadAgents = async () => {
    try {
      setIsLoading(true)
      const data = await agents.list()

      // Fetch version numbers for agents that have a current version
      const agentsWithVersions = await Promise.all(
        data.map(async (agent) => {
          if (agent.currentVersionId) {
            try {
              const versions = await agents.listVersions(agent.id)
              const currentVersion = versions.find(v => v.id === agent.currentVersionId)
              return {
                ...agent,
                versionNumber: currentVersion?.versionNumber
              }
            } catch (error) {
              console.error(`Failed to load version for agent ${agent.id}:`, error)
              return agent
            }
          }
          return agent
        })
      )

      setAgentsList(agentsWithVersions)
    } catch (error) {
      console.error('Failed to load agents:', error)
      // TODO: Show error toast
    } finally {
      setIsLoading(false)
    }
  }

  const handleCreate = async () => {
    try {
      setIsCreating(true)

      // Create agent with default name
      const response = await agents.create({
        name: `agent_${Date.now()}`,
        description: ''
      })

      // Navigate to editor with returned ID
      navigate(`/agents/${response.id}/edit`)
    } catch (error) {
      console.error('Failed to create agent:', error)
      // TODO: Show error toast
    } finally {
      setIsCreating(false)
    }
  }

  const handleEdit = (agentId: string) => {
    navigate(`/agents/${agentId}/edit`)
  }

  const handleDelete = async (agentId: string, agentName: string) => {
    if (!window.confirm(`Are you sure you want to delete "${agentName}"? This action cannot be undone.`)) {
      return
    }

    try {
      setDeletingId(agentId)
      await agents.delete(agentId)
      // Reload the list
      await loadAgents()
      // TODO: Show success toast
    } catch (error) {
      console.error('Failed to delete agent:', error)
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
          <h1 className="text-2xl font-bold">Agents</h1>
          <p className="text-muted-foreground">
            Create and manage your AI agents
          </p>
        </div>
        <Button onClick={handleCreate} disabled={isCreating}>
          {isCreating ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Plus className="h-4 w-4" />
          )}
          <span className="hidden sm:inline">New Agent</span>
        </Button>
      </div>

      {agentsList.length === 0 ? (
        /* Empty state */
        <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
          <div className="rounded-full bg-muted p-4">
            <Plus className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mt-4 text-lg font-semibold">No agents yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Get started by creating your first agent
          </p>
          <Button className="mt-4" onClick={handleCreate} disabled={isCreating}>
            {isCreating ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Plus className="h-4 w-4" />
            )}
            Create Agent
          </Button>
        </div>
      ) : (
        <>
          {/* Mobile view - card layout */}
          <div className="space-y-3 md:hidden">
            {agentsList.map((agent) => (
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
                    <div className="text-sm">
                      <span className="text-muted-foreground">Status: </span>
                      {agent.currentVersionId ? (
                        <span className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">
                            Published
                          </Badge>
                          {agent.versionNumber && (
                            <span className="text-xs">v{agent.versionNumber}</span>
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
                {agentsList.map((agent) => (
                  <TableRow key={agent.id}>
                    <TableCell className="font-medium">{agent.name}</TableCell>
                    <TableCell className="max-w-md">
                      {agent.description || (
                        <span className="text-muted-foreground italic">No description</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {agent.versionNumber ? (
                        <span className="font-mono text-sm">v{agent.versionNumber}</span>
                      ) : (
                        <span className="text-muted-foreground text-sm">—</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {agent.currentVersionId ? (
                        <Badge variant="outline">Published</Badge>
                      ) : (
                        <Badge variant="secondary">Draft</Badge>
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
