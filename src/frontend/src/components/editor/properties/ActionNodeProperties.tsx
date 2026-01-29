import { useState } from 'react'
import { useEditorStore, type ActionNodeConfig } from '@/store/editor'
import { useActions } from '@/hooks/useActions'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import { FormField } from '@/components/ui/form-field'
import { Pencil, Check } from 'lucide-react'
import type { ParameterSchema } from '@/types/actions'
import { KeyValueEditor, type KeyValueCollection } from './KeyValueEditor'
import { ScribanEditor } from './ScribanEditor'

interface ActionNodePropertiesProps {
  nodeId: string
}

export function ActionNodeProperties({ nodeId }: ActionNodePropertiesProps) {
  const node = useEditorStore((state) => state.nodes.find((n) => n.id === nodeId))
  const config = useEditorStore((state) => state.nodeConfigurations[nodeId]) as ActionNodeConfig
  const updateNodeConfig = useEditorStore((state) => state.updateNodeConfig)
  const updateNodeData = useEditorStore((state) => state.updateNodeData)
  const { getAction } = useActions()

  const [isEditingName, setIsEditingName] = useState(false)
  const [editedName, setEditedName] = useState('')

  if (!node || node.type !== 'action') {
    return <div className="p-4 text-sm text-muted-foreground">Invalid node</div>
  }

  if (!config) {
    return <div className="p-4 text-sm text-muted-foreground">No configuration found</div>
  }

  const actionType = config.actionType
  if (!actionType) {
    return <div className="p-4 text-sm text-muted-foreground">No action type specified</div>
  }

  const actionSchema = getAction(actionType)
  if (!actionSchema) {
    return <div className="p-4 text-sm text-muted-foreground">Action schema not found: {actionType}</div>
  }

  const handleStartEdit = () => {
    setEditedName(config.name)
    setIsEditingName(true)
  }

  const handleSaveName = () => {
    if (editedName.trim()) {
      updateNodeConfig(nodeId, { name: editedName.trim() })
    }
    setIsEditingName(false)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSaveName()
    } else if (e.key === 'Escape') {
      setIsEditingName(false)
    }
  }

  const handleParameterChange = (paramName: string, value: any) => {
    // Update the config (source of truth)
    updateNodeConfig(nodeId, {
      parameters: {
        ...(config.parameters || {}),
        [paramName]: value
      }
    })
    // Also update node data for display consistency
    updateNodeData(nodeId, {
      ...node.data,
      parameters: {
        ...(config.parameters || {}),
        [paramName]: value
      }
    })
  }

  const getParameterValue = (paramName: string, defaultValue?: string) => {
    return config.parameters?.[paramName] ?? defaultValue ?? ''
  }

  const renderParameter = (param: ParameterSchema) => {
    const value = getParameterValue(param.name, param.defaultValue)
    const label = param.required ? `${param.displayName} *` : param.displayName

    // Dropdown (enum)
    if (param.controlType === 'dropdown' && param.options) {
      return (
        <FormField
          key={param.name}
          label={label}
          description={param.description}
          htmlFor={param.name}
        >
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
        </FormField>
      )
    }

    // Slider
    if (param.controlType === 'slider' && param.validation) {
      const min = param.validation.min ?? 0
      const max = param.validation.max ?? 100
      const step = param.validation.step ?? 1
      const numValue = typeof value === 'number' ? value : parseInt(value) || min

      return (
        <FormField
          key={param.name}
          label={`${label}: ${numValue}`}
          description={param.description}
          htmlFor={param.name}
        >
          <Slider
            id={param.name}
            min={min}
            max={max}
            step={step}
            value={[numValue]}
            onValueChange={([v]) => handleParameterChange(param.name, v)}
          />
          <div className="flex justify-between text-xs text-muted-foreground mt-1">
            <span>{min}</span>
            <span>{max}</span>
          </div>
        </FormField>
      )
    }

    // Textarea
    if (param.controlType === 'textarea') {
      return (
        <FormField
          key={param.name}
          label={label}
          description={param.description}
          htmlFor={param.name}
        >
          <Textarea
            id={param.name}
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={param.displayName}
            rows={4}
          />
        </FormField>
      )
    }

    // Code editor using Scriban (with variable autocomplete)
    if (param.controlType === 'code') {
      const description = param.supportsVariables
        ? `${param.description || ''} Type {{ for autocomplete.`.trim()
        : param.description
      return (
        <FormField
          key={param.name}
          label={label}
          description={description}
          htmlFor={param.name}
        >
          <ScribanEditor
            nodeId={nodeId}
            value={value}
            onChange={(v) => handleParameterChange(param.name, v)}
            height="200px"
          />
        </FormField>
      )
    }

    // Key-Value List
    if (param.controlType === 'keyValueList') {
      const kvValue = (value as KeyValueCollection | null) ?? {
        useVariable: false,
        variable: '',
        items: []
      }
      return (
        <KeyValueEditor
          key={param.name}
          id={param.name}
          label={label}
          description={param.description ?? undefined}
          required={param.required}
          value={kvValue}
          onChange={(v) => handleParameterChange(param.name, v)}
        />
      )
    }

    // Checkbox
    if (param.controlType === 'checkbox') {
      const checked = value === true || value === 'true'
      return (
        <FormField
          key={param.name}
          label={label}
          description={param.description}
          htmlFor={param.name}
        >
          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id={param.name}
              checked={checked}
              onChange={(e) => handleParameterChange(param.name, e.target.checked)}
              className="h-4 w-4 rounded border-border"
            />
            <Label htmlFor={param.name} className="font-normal cursor-pointer text-sm">
              Enabled
            </Label>
          </div>
        </FormField>
      )
    }

    // Number input
    if (param.controlType === 'number' || param.type === 'number') {
      return (
        <FormField
          key={param.name}
          label={label}
          description={param.description}
          htmlFor={param.name}
        >
          <Input
            id={param.name}
            type="number"
            value={value}
            onChange={(e) => handleParameterChange(param.name, e.target.value)}
            placeholder={param.displayName}
            min={param.validation?.min}
            max={param.validation?.max}
            step={param.validation?.step}
          />
        </FormField>
      )
    }

    // Default: text input
    const textDescription = param.supportsVariables
      ? `${param.description || ''} Use {{expressions}} for variables.`.trim()
      : param.description
    return (
      <FormField
        key={param.name}
        label={label}
        description={textDescription}
        htmlFor={param.name}
      >
        <Input
          id={param.name}
          type="text"
          value={value}
          onChange={(e) => handleParameterChange(param.name, e.target.value)}
          placeholder={param.displayName}
          maxLength={param.validation?.maxLength}
        />
      </FormField>
    )
  }

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto p-4">
      {/* Header with editable name */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          {isEditingName ? (
            <>
              <input
                type="text"
                value={editedName}
                onChange={(e) => setEditedName(e.target.value)}
                onKeyDown={handleKeyDown}
                onBlur={handleSaveName}
                autoFocus
                className="flex-1 bg-transparent text-sm font-semibold outline-none border-b border-primary"
              />
              <button
                onClick={handleSaveName}
                className="p-1 hover:bg-muted rounded"
              >
                <Check className="h-3.5 w-3.5" />
              </button>
            </>
          ) : (
            <>
              <h3 className="text-sm font-semibold">{config.name}</h3>
              <button
                onClick={handleStartEdit}
                className="p-1 hover:bg-muted rounded opacity-50 hover:opacity-100 transition-opacity"
              >
                <Pencil className="h-3 w-3" />
              </button>
            </>
          )}
        </div>
        <p className="text-xs text-muted-foreground">
          {actionSchema.displayName}{actionSchema.description ? ` - ${actionSchema.description}` : ''}
        </p>
      </div>

      {/* Parameters */}
      {actionSchema.parameters.length > 0 && (
        <div className="space-y-4">
          {actionSchema.parameters.map(renderParameter)}
        </div>
      )}
    </div>
  )
}
