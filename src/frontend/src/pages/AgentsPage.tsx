import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function AgentsPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Agents</h1>
          <p className="text-muted-foreground">
            Create and manage your AI agents
          </p>
        </div>
        <Button>
          <Plus className="h-4 w-4" />
          <span className="hidden sm:inline">New Agent</span>
        </Button>
      </div>

      {/* Empty state */}
      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <div className="rounded-full bg-muted p-4">
          <Plus className="h-8 w-8 text-muted-foreground" />
        </div>
        <h3 className="mt-4 text-lg font-semibold">No agents yet</h3>
        <p className="mt-2 text-sm text-muted-foreground">
          Get started by creating your first agent
        </p>
        <Button className="mt-4">
          <Plus className="h-4 w-4" />
          Create Agent
        </Button>
      </div>
    </div>
  )
}
