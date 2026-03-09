import { useAgentBuilderStore } from '@/store/agentBuilder'
import {
  Label,
  Input,
  Switch,
  RadioGroup,
  RadioGroupItem,
} from '@donkeywork/ui'

export function AgentSettingsProperties() {
  const lifecycle = useAgentBuilderStore((s) => s.lifecycle)
  const lingerSeconds = useAgentBuilderStore((s) => s.lingerSeconds)
  const timeoutSeconds = useAgentBuilderStore((s) => s.timeoutSeconds)
  const persistMessages = useAgentBuilderStore((s) => s.persistMessages)
  const setAgentSettings = useAgentBuilderStore((s) => s.setAgentSettings)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)

  return (
    <div className="space-y-6">
      {/* Lifecycle */}
      <div className="space-y-2">
        <Label>Lifecycle</Label>
        <RadioGroup
          value={lifecycle}
          onValueChange={(v) => setAgentSettings({ lifecycle: v as 'Task' | 'Linger' })}
          disabled={isReadOnly}
        >
          <div className="flex items-center gap-2">
            <RadioGroupItem value="Task" id="lifecycle-task" />
            <Label htmlFor="lifecycle-task" className="font-normal">
              Task — runs to completion then stops
            </Label>
          </div>
          <div className="flex items-center gap-2">
            <RadioGroupItem value="Linger" id="lifecycle-linger" />
            <Label htmlFor="lifecycle-linger" className="font-normal">
              Linger — stays alive for follow-up messages
            </Label>
          </div>
        </RadioGroup>
      </div>

      {/* Linger Seconds */}
      {lifecycle === 'Linger' && (
        <div className="space-y-2">
          <Label htmlFor="linger-seconds">Linger Duration (seconds)</Label>
          <Input
            id="linger-seconds"
            type="number"
            value={lingerSeconds}
            onChange={(e) => setAgentSettings({ lingerSeconds: Number(e.target.value) })}
            disabled={isReadOnly}
          />
        </div>
      )}

      {/* Timeout */}
      <div className="space-y-2">
        <Label htmlFor="timeout-seconds">Timeout (seconds)</Label>
        <Input
          id="timeout-seconds"
          type="number"
          value={timeoutSeconds}
          onChange={(e) => setAgentSettings({ timeoutSeconds: Number(e.target.value) })}
          disabled={isReadOnly}
        />
        <p className="text-xs text-muted-foreground">
          Maximum time the agent can run before being stopped
        </p>
      </div>

      {/* Persist Messages */}
      <div className="flex items-center justify-between">
        <div>
          <Label>Persist Messages</Label>
          <p className="text-xs text-muted-foreground">Save conversation history across sessions</p>
        </div>
        <Switch
          checked={persistMessages}
          onCheckedChange={(v) => setAgentSettings({ persistMessages: v })}
          disabled={isReadOnly}
        />
      </div>
    </div>
  )
}
