import { useState, useEffect, useCallback } from 'react'
import type { NodeFieldSchema } from '@donkeywork/api-client'
import { credentials, type CredentialSummary, audioCollections, type AudioCollection } from '@donkeywork/api-client'
import {
  Input,
  Textarea,
  Slider,
  Switch,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Button,
  Label,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@donkeywork/ui'
import { Plus, Trash2, Variable } from 'lucide-react'
import { KeyValueEditor, type KeyValueCollection } from './KeyValueEditor'
import { ScribanEditor } from './ScribanEditor'
import Editor from '@monaco-editor/react'
import { CreateCredentialDialog } from '@/components/credentials/CreateCredentialDialog'
import { AudioCollectionFormDialog } from '@/components/audio/AudioCollectionFormDialog'

// Variable mode toggle button - allows switching between native control and ScribanEditor
function VariableModeButton({ isActive, onClick }: { isActive: boolean; onClick: () => void }) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <button
            type="button"
            onClick={onClick}
            className={`p-1 rounded transition-colors ${
              isActive
                ? 'bg-accent text-accent-foreground'
                : 'text-muted-foreground hover:text-foreground hover:bg-muted'
            }`}
          >
            <Variable className="h-3.5 w-3.5" />
          </button>
        </TooltipTrigger>
        <TooltipContent side="left">
          <p>{isActive ? 'Switch to value mode' : 'Switch to variable mode'}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

interface FieldRendererProps {
  field: NodeFieldSchema
  value: unknown
  onChange: (value: unknown) => void
  predecessors?: Array<{ nodeId: string; nodeName: string; nodeType: string }>
  credentialProvider?: string
  /** If true, render the field as read-only */
  readOnly?: boolean
  /** Model ID for checking supportedBy field visibility */
  modelId?: string
  /** Max output tokens for the selected model (for dynamic slider max) */
  modelMaxOutputTokens?: number
}

export function FieldRenderer({
  field,
  value,
  onChange,
  predecessors = [],
  credentialProvider,
  readOnly = false,
  modelId,
  modelMaxOutputTokens,
}: FieldRendererProps) {
  const [availableCredentials, setAvailableCredentials] = useState<CredentialSummary[]>([])
  const [isCreateCredentialOpen, setIsCreateCredentialOpen] = useState(false)
  const [availableAudioCollections, setAvailableAudioCollections] = useState<AudioCollection[]>([])
  const [isCreateAudioCollectionOpen, setIsCreateAudioCollectionOpen] = useState(false)
  // Track if field is in variable mode (showing ScribanEditor instead of native control)
  const [isVariableMode, setIsVariableMode] = useState(false)

  const hasVariableExpression = typeof value === 'string' && value.includes('{{')

  // Determine if we should show variable mode (either explicitly toggled or value has variables)
  const showAsVariable = isVariableMode || hasVariableExpression

  // Control types that need the variable mode toggle (native controls that don't support variables directly)
  const needsVariableToggle = field.supportsVariables && ['Slider', 'Toggle', 'Number', 'Select', 'AudioCollection'].includes(field.controlType)

  const isReadOnly = readOnly || field.immutable === true

  // All hooks MUST be called before any conditional returns
  const loadCredentials = useCallback(() => {
    credentials.list().then(setAvailableCredentials).catch(console.error)
  }, [])

  const loadAudioCollections = useCallback(() => {
    audioCollections.list(0, 100).then((r) => setAvailableAudioCollections(r.items)).catch(console.error)
  }, [])

  useEffect(() => {
    if (field.controlType === 'Credential') {
      loadCredentials()
    }
    if (field.controlType === 'AudioCollection') {
      loadAudioCollections()
    }
  }, [field.controlType, loadCredentials, loadAudioCollections])

  if (field.supportedBy && field.supportedBy.length > 0 && modelId) {
    if (!field.supportedBy.includes(modelId)) {
      return null
    }
  }

  const filteredCredentials = credentialProvider
    ? availableCredentials.filter(c => c.provider === credentialProvider)
    : availableCredentials

  const renderControl = () => {
    // If field is immutable/read-only, render as static text
    if (isReadOnly) {
      return (
        <div className="rounded-xl border border-input bg-muted/50 px-3 py-2.5 text-sm text-muted-foreground">
          {value !== undefined && value !== null ? String(value) : (field.default !== undefined ? String(field.default) : 'Not set')}
        </div>
      )
    }

    switch (field.controlType) {
      case 'Text':
        // Use ScribanEditor for Text fields that support variables
        if (field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={field.placeholder}
              predecessors={predecessors}
              height="60px"
            />
          )
        }
        return (
          <Input
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
          />
        )

      case 'TextArea':
        if (field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={field.placeholder}
              predecessors={predecessors}
            />
          )
        }
        return (
          <Textarea
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
            rows={4}
          />
        )

      case 'TextAreaList':
        return (
          <TextAreaListControl
            value={Array.isArray(value) ? value : []}
            onChange={onChange}
            placeholder={field.placeholder}
            supportsVariables={field.supportsVariables}
            predecessors={predecessors}
          />
        )

      case 'Number':
        if (showAsVariable && field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={`Enter value or variable, e.g. {{Input.${field.name}}}`}
              predecessors={predecessors}
              height="60px"
            />
          )
        }
        return (
          <Input
            type="number"
            value={value !== undefined && value !== null ? String(value) : ''}
            onChange={(e) => {
              const val = e.target.value
              onChange(val === '' ? undefined : Number(val))
            }}
            min={field.min}
            max={field.max}
            step={field.step}
            placeholder={field.placeholder}
          />
        )

      case 'Slider': {
        if (showAsVariable && field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={`Enter value or variable, e.g. {{Input.${field.name}}}`}
              predecessors={predecessors}
              height="60px"
            />
          )
        }

        // For maxOutputTokens, use model-specific max if available
        const isMaxOutputTokens = field.name === 'maxOutputTokens'
        const sliderMax = isMaxOutputTokens && modelMaxOutputTokens
          ? modelMaxOutputTokens
          : (field.max ?? 100)
        const sliderMin = field.min ?? (isMaxOutputTokens ? 1 : 0)
        const sliderStep = field.step ?? (isMaxOutputTokens ? 256 : 1)
        const sliderDefault = field.default ?? (isMaxOutputTokens ? 4096 : 0)
        const currentValue = Number(value ?? sliderDefault)

        const displayValue = isMaxOutputTokens
          ? currentValue.toLocaleString()
          : (Number.isInteger(currentValue) ? currentValue.toString() : currentValue.toFixed(2))

        return (
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground tabular-nums w-12 text-right">
                {sliderMin.toLocaleString()}
              </span>
              <Slider
                value={[currentValue]}
                onValueChange={([val]) => onChange(val)}
                min={sliderMin}
                max={sliderMax}
                step={sliderStep}
                className="flex-1"
              />
              <span className="text-xs text-muted-foreground tabular-nums w-14">
                {sliderMax.toLocaleString()}
              </span>
            </div>
            <div className="text-center">
              <span className="text-sm font-medium tabular-nums">
                {displayValue}
              </span>
            </div>
          </div>
        )
      }

      case 'Select':
        if (showAsVariable && field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={`Enter value or variable, e.g. {{Input.${field.name}}}`}
              predecessors={predecessors}
              height="60px"
            />
          )
        }
        return (
          <Select
            value={String(value ?? '')}
            onValueChange={onChange}
          >
            <SelectTrigger>
              <SelectValue placeholder={field.placeholder || `Select ${field.label}`} />
            </SelectTrigger>
            <SelectContent>
              {field.options?.map((option) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )

      case 'Toggle':
        if (showAsVariable && field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={`Enter value or variable, e.g. {{Input.${field.name}}}`}
              predecessors={predecessors}
              height="60px"
            />
          )
        }
        return (
          <Switch
            checked={Boolean(value)}
            onCheckedChange={onChange}
          />
        )

      case 'Code':
        if (field.supportsVariables) {
          return (
            <ScribanEditor
              value={String(value ?? '')}
              onChange={onChange}
              placeholder={field.placeholder}
              predecessors={predecessors}
              className="min-h-[200px]"
            />
          )
        }
        return (
          <div
            className="border rounded-md overflow-hidden"
            onKeyDown={(e) => e.stopPropagation()}
            onKeyDownCapture={(e) => e.stopPropagation()}
          >
            <Editor
              height="200px"
              defaultLanguage="plaintext"
              value={String(value ?? '')}
              onChange={(val: string | undefined) => onChange(val ?? '')}
              theme="vs-dark"
              options={{
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 13,
                lineNumbers: 'off',
                wordWrap: 'on',
              }}
            />
          </div>
        )

      case 'Json':
        return (
          <div
            className="border rounded-md overflow-hidden"
            onKeyDown={(e) => e.stopPropagation()}
            onKeyDownCapture={(e) => e.stopPropagation()}
          >
            <Editor
              height="200px"
              defaultLanguage="json"
              value={typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)}
              onChange={(val: string | undefined) => {
                try {
                  onChange(JSON.parse(val ?? '{}'))
                } catch {
                  // Keep as string if invalid JSON
                  onChange(val)
                }
              }}
              theme="vs-dark"
              options={{
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 13,
                lineNumbers: 'on',
                wordWrap: 'on',
                formatOnPaste: true,
                formatOnType: true,
              }}
            />
          </div>
        )

      case 'KeyValueList':
        return (
          <KeyValueEditor
            id={field.name}
            label={field.label}
            description={field.description}
            required={field.required}
            value={value as KeyValueCollection | null ?? null}
            onChange={(v) => onChange(v)}
          />
        )

      case 'Credential':
        return (
          <>
            <Select
              value={String(value ?? '')}
              onValueChange={(v) => {
                if (v === '__create_new__') {
                  setIsCreateCredentialOpen(true)
                } else {
                  onChange(v)
                }
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select credential" />
              </SelectTrigger>
              <SelectContent>
                {filteredCredentials.map((cred) => (
                  <SelectItem key={cred.id} value={cred.id}>
                    {cred.name} ({cred.provider})
                  </SelectItem>
                ))}
                <SelectItem value="__create_new__" className="text-blue-500">
                  <div className="flex items-center gap-2">
                    <Plus className="h-3.5 w-3.5" />
                    <span>Add New Credential</span>
                  </div>
                </SelectItem>
              </SelectContent>
            </Select>
            <CreateCredentialDialog
              open={isCreateCredentialOpen}
              onOpenChange={setIsCreateCredentialOpen}
              onCreated={(credentialId) => {
                loadCredentials()
                onChange(credentialId)
              }}
              defaultProvider={credentialProvider as "OpenAi" | "Anthropic" | "Google" | "Azure" | undefined}
            />
          </>
        )

      case 'AudioCollection': {
        const selected = availableAudioCollections.find((c) => c.id === value)
        return (
          <>
            <Select
              value={selected ? String(value) : ''}
              onValueChange={(v) => {
                if (v === '__create_new__') {
                  setIsCreateAudioCollectionOpen(true)
                } else if (v === '__unfiled__') {
                  onChange('')
                } else {
                  onChange(v)
                }
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder={
                  typeof value === 'string' && value && !selected
                    ? value
                    : 'Unfiled (pick or create a collection)'
                } />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__unfiled__">
                  <span className="text-muted-foreground">Unfiled</span>
                </SelectItem>
                {availableAudioCollections.map((c) => (
                  <SelectItem key={c.id} value={c.id}>
                    {c.name}
                  </SelectItem>
                ))}
                <SelectItem value="__create_new__" className="text-blue-500">
                  <div className="flex items-center gap-2">
                    <Plus className="h-3.5 w-3.5" />
                    <span>New Collection…</span>
                  </div>
                </SelectItem>
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground mt-1">
              Or switch to variable mode and enter a name — the collection is created on first run if it doesn't exist.
            </p>
            <AudioCollectionFormDialog
              open={isCreateAudioCollectionOpen}
              mode="create"
              onClose={() => setIsCreateAudioCollectionOpen(false)}
              onSaved={(collection) => {
                setIsCreateAudioCollectionOpen(false)
                loadAudioCollections()
                onChange(collection.id)
              }}
            />
          </>
        )
      }

      default:
        return (
          <Input
            value={String(value ?? '')}
            onChange={(e) => onChange(e.target.value)}
            placeholder={field.placeholder}
          />
        )
    }
  }

  // KeyValueList renders its own label and description
  if (field.controlType === 'KeyValueList') {
    return renderControl()
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label className="text-sm font-medium">
          {field.label}
          {field.required && <span className="text-destructive ml-1">*</span>}
        </Label>
        {needsVariableToggle && (
          <VariableModeButton
            isActive={showAsVariable}
            onClick={() => setIsVariableMode(!isVariableMode)}
          />
        )}
      </div>
      {field.description && (
        <p className="text-xs text-muted-foreground">{field.description}</p>
      )}
      {renderControl()}
    </div>
  )
}

// TextAreaList component for arrays of text (SystemPrompts[], UserMessages[])
interface TextAreaListControlProps {
  value: string[]
  onChange: (value: string[]) => void
  placeholder?: string
  supportsVariables: boolean
  predecessors: Array<{ nodeId: string; nodeName: string; nodeType: string }>
}

function TextAreaListControl({
  value,
  onChange,
  placeholder,
  supportsVariables,
  predecessors,
}: TextAreaListControlProps) {
  const addItem = () => {
    onChange([...value, ''])
  }

  const removeItem = (index: number) => {
    onChange(value.filter((_, i) => i !== index))
  }

  const updateItem = (index: number, newValue: string) => {
    const updated = [...value]
    updated[index] = newValue
    onChange(updated)
  }

  return (
    <div className="space-y-2">
      {value.map((item, index) => (
        <div key={index} className="flex gap-2">
          <div className="flex-1">
            {supportsVariables ? (
              <ScribanEditor
                value={item}
                onChange={(val) => updateItem(index, String(val))}
                placeholder={placeholder}
                predecessors={predecessors}
              />
            ) : (
              <Textarea
                value={item}
                onChange={(e) => updateItem(index, e.target.value)}
                placeholder={placeholder}
                rows={3}
              />
            )}
          </div>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => removeItem(index)}
            className="shrink-0"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ))}
      <Button
        variant="outline"
        size="sm"
        onClick={addItem}
        className="w-full"
      >
        <Plus className="h-4 w-4 mr-2" />
        Add Item
      </Button>
    </div>
  )
}
