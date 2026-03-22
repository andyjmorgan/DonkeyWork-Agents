import { useAgentBuilderStore } from '@/store/agentBuilder'
import {
  Label,
  Input,
  Switch,
  RadioGroup,
  RadioGroupItem,
} from '@donkeywork/ui'

export function AgentSettingsProperties() {
  const agentIcon = useAgentBuilderStore((s) => s.agentIcon)
  const setAgentMetadata = useAgentBuilderStore((s) => s.setAgentMetadata)
  const agentName = useAgentBuilderStore((s) => s.agentName)
  const agentDescription = useAgentBuilderStore((s) => s.agentDescription)
  const lifecycle = useAgentBuilderStore((s) => s.lifecycle)
  const lingerSeconds = useAgentBuilderStore((s) => s.lingerSeconds)
  const timeoutSeconds = useAgentBuilderStore((s) => s.timeoutSeconds)
  const persistMessages = useAgentBuilderStore((s) => s.persistMessages)
  const connectToNavi = useAgentBuilderStore((s) => s.connectToNavi)
  const allowDelegation = useAgentBuilderStore((s) => s.allowDelegation)
  const setAgentSettings = useAgentBuilderStore((s) => s.setAgentSettings)
  const isReadOnly = useAgentBuilderStore((s) => s.isReadOnly)

  return (
    <div className="space-y-6">
      {/* Icon */}
      <div className="space-y-2">
        <Label htmlFor="agent-icon">Icon</Label>
        <Input
          id="agent-icon"
          value={agentIcon}
          onChange={(e) => setAgentMetadata(agentName, agentDescription, e.target.value)}
          placeholder="lucide icon name or image URL"
          disabled={isReadOnly}
        />
        <p className="text-xs text-muted-foreground">
          A lucide icon name (e.g. "brain", "search") or an image URL (png, svg, ico)
        </p>
      </div>

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

      {/* Connect to Navi */}
      <div className="flex items-center justify-between">
        <div>
          <Label>Connect to Navi</Label>
          <p className="text-xs text-muted-foreground">Make this agent available as a spawn target in Navi conversations</p>
        </div>
        <Switch
          checked={connectToNavi}
          onCheckedChange={(v) => setAgentSettings({ connectToNavi: v })}
          disabled={isReadOnly}
        />
      </div>

      {/* Allow Delegation */}
      <div className="flex items-center justify-between">
        <div>
          <Label>Allow Delegation</Label>
          <p className="text-xs text-muted-foreground">Let this agent spawn lightweight delegates for ad-hoc tasks</p>
        </div>
        <Switch
          checked={allowDelegation}
          onCheckedChange={(v) => setAgentSettings({ allowDelegation: v })}
          disabled={isReadOnly}
        />
      </div>
    </div>
  )
}
