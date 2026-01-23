import { Lock } from 'lucide-react'

export function SecretsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Secrets</h1>
        <p className="text-muted-foreground">
          Store sensitive values securely for use in agents
        </p>
      </div>

      <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
        <div className="rounded-full bg-muted p-4">
          <Lock className="h-8 w-8 text-muted-foreground" />
        </div>
        <h3 className="mt-4 text-lg font-semibold">Coming Soon</h3>
        <p className="mt-2 text-sm text-muted-foreground max-w-sm">
          Securely store API keys, tokens, and other sensitive values for your agents to use
        </p>
      </div>
    </div>
  )
}
