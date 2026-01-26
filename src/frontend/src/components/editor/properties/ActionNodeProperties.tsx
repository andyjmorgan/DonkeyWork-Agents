import { useEditorStore } from '@/store/editor'
import { useActions } from '@/hooks/useActions'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import type { ParameterSchema } from '@/types/actions'

interface ActionNodePropertiesProps {
  nodeId: string
}

export function ActionNodeProperties({ nodeId }: ActionNodePropertiesProps) {
  const node = useEditorStore((state) => state.nodes.find((n) => n.id === nodeId))
  const updateNodeData = useEditorStore((state) => state.updateNodeData)
  const { getAction } = useActions()

  if (!node || node.type !== 'action') {
    return <div className="p-4 text-sm text-muted-foreground">Invalid node</div>
  }

  const actionType = node.data?.actionType
  if (!actionType) {
    return <div className="p-4 text-sm text-muted-foreground">No action type specified</div>
  }

  const actionSchema = getAction(actionType)
  if (!actionSchema) {
    return <div className="p-4 text-sm text-muted-foreground">Action schema not found: {actionType}</div>
  }

  const handleParameterChange = (paramName: string, value: any) => {
    updateNodeData(nodeId, {
      ...node.data,
      parameters: {
        ...(node.data?.parameters || {}),
        [paramName]: value
      }
    })
  }

  const getParameterValue = (paramName: string, defaultValue?: string) => {
    return node.data?.parameters?.[paramName] ?? defaultValue ?? ''
  }

  const renderParameter = (param: ParameterSchema) => {
    const value = getParameterValue(param.name, param.defaultValue)

    // Dropdown (enum)
    if (param.controlType === 'dropdown' && param.options) {
      return (
        <div key={param.name} className="space-y-2">
          <Label htmlFor={param.name}>
            {param.displayName}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">{param.description}</p>
          )}
          <Select
            value={value}
            onValueChange={(v) => handleParameterChange(param.name, v)}
          >
            <SelectTrigger id={param.name}>
              <SelectValue placeholder={`Select ${param.displayName.toLowerCase()}`} />
            </SelectTrigger>
            <SelectContent>
              {param.options.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )
    }

    // Slider
    if (param.controlType === 'slider' && param.validation) {
      const min = param.validation.min ?? 0
      const max = param.validation.max ?? 100
      const step = param.validation.step ?? 1
      const numValue = typeof value === 'number' ? value : parseInt(value) || min

      return (
        <div key={param.name} className="space-y-2">
          <Label htmlFor={param.name}>
            {param.displayName}: {numValue}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">{param.description}</p>
          )}
          <Slider
            id={param.name}
            min={min}
            max={max}
            step={step}
            value={[numValue]}
            onValueChange={([v]) => handleParameterChange(param.name, v)}
            className="mt-2"
          />
          <div className="flex justify-between text-xs text-muted-foreground">
            <span>{min}</span>
            <span>{max}</span>
          </div>
        </div>
      )
    }

    // Textarea
    if (param.controlType === 'textarea') {
      return (
        <div key={param.name} className="space-y-2">
          <Label htmlFor={param.name}>
            {param.displayName}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">{param.description}</p>
          )}
          <Textarea
            id={param.name}
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={param.description || param.displayName}
            rows={4}
          />
        </div>
      )
    }

    // Code editor (for now, just use textarea with monospace font)
    if (param.controlType === 'code') {
      return (
        <div key={param.name} className="space-y-2">
          <Label htmlFor={param.name}>
            {param.displayName}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">{param.description}</p>
          )}
          <Textarea
            id={param.name}
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={param.description || param.displayName}
            rows={8}
            className="font-mono text-sm"
          />
        </div>
      )
    }

    // Checkbox
    if (param.controlType === 'checkbox') {
      const checked = value === true || value === 'true'
      return (
        <div key={param.name} className="flex items-center space-x-2">
          <input
            type="checkbox"
            id={param.name}
            checked={checked}
            onChange={(e) => handleParameterChange(param.name, e.target.checked)}
            className="h-4 w-4 rounded border-border"
          />
          <Label htmlFor={param.name} className="font-normal cursor-pointer">
            {param.displayName}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">({param.description})</p>
          )}
        </div>
      )
    }

    // Number input
    if (param.controlType === 'number' || param.type === 'number') {
      return (
        <div key={param.name} className="space-y-2">
          <Label htmlFor={param.name}>
            {param.displayName}
            {param.required && <span className="text-destructive">*</span>}
          </Label>
          {param.description && (
            <p className="text-xs text-muted-foreground">{param.description}</p>
          )}
          <Input
            id={param.name}
            type="number"
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={param.description || param.displayName}
            min={param.validation?.min}
            max={param.validation?.max}
            step={param.validation?.step}
          />
        </div>
      )
    }

    // Default: text input
    return (
      <div key={param.name} className="space-y-2">
        <Label htmlFor={param.name}>
          {param.displayName}
          {param.required && <span className="text-destructive">*</span>}
        </Label>
        {param.description && (
          <p className="text-xs text-muted-foreground">{param.description}</p>
        )}
        <Input
          id={param.name}
          type="text"
          value={value}
          onChange={(e) => handleParameterChange(param.name, e.target.value)}
          placeholder={param.description || param.displayName}
          maxLength={param.validation?.maxLength}
        />
        {param.supportsVariables && (
          <p className="text-xs text-blue-500">
            ✨ Supports variables: Use {'{{'} expressions {'}}'}
          </p>
        )}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Action info */}
      <div className="space-y-2">
        <h3 className="text-lg font-semibold">{actionSchema.displayName}</h3>
        {actionSchema.description && (
          <p className="text-sm text-muted-foreground">{actionSchema.description}</p>
        )}
      </div>

      {/* Parameters */}
      <div className="space-y-4">
        {actionSchema.parameters.map(renderParameter)}
      </div>
    </div>
  )
}
