import { Box } from 'lucide-react'

export function SandboxProperties() {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 rounded-lg border border-border p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-gradient-to-br from-cyan-500 to-teal-600 shadow-lg shadow-cyan-500/25">
          <Box className="h-5 w-5 text-white" />
        </div>
        <div>
          <div className="font-medium">Code Sandbox</div>
          <div className="text-xs text-muted-foreground">
            Execute code in an isolated sandbox environment
          </div>
        </div>
      </div>
      <p className="text-xs text-muted-foreground">
        The sandbox is enabled for this agent. Remove the node from the canvas to disable it.
      </p>
    </div>
  )
}
