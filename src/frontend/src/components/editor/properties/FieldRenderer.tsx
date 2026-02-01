import { useState, useEffect } from 'react'
import type { NodeFieldSchema } from '@/lib/api'
import { credentials, type CredentialSummary } from '@/lib/api'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Slider } from '@/components/ui/slider'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Button } from '@/components/ui/button'
import { Plus, Trash2 } from 'lucide-react'
import { KeyValueEditor } from './KeyValueEditor'
import { ScribanEditor } from './ScribanEditor'
import Editor from '@monaco-editor/react'
import { Label } from '@/components/ui/label'

interface FieldRendererProps {
  field: NodeFieldSchema
  value: unknown
  onChange: (value: unknown) => void
  predecessors?: Array<{ nodeId: string; nodeName: string; nodeType: string }>
  credentialProvider?: string
}

export function FieldRenderer({
  field,
  value,
  onChange,
  predecessors = [],
  credentialProvider,
}: FieldRendererProps) {
  const [availableCredentials, setAvailableCredentials] = useState<CredentialSummary[]>([])

  // Load credentials if this is a Credential field
  useEffect(() => {
    if (field.controlType === 'Credential') {
      credentials.list().then(setAvailableCredentials).catch(console.error)
    }
  }, [field.controlType])

  // Filter credentials by provider if specified
  const filteredCredentials = credentialProvider
    ? availableCredentials.filter(c => c.provider === credentialProvider)
    : availableCredentials

  const renderControl = () => {
    switch (field.controlType) {
      case 'Text':
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

      case 'Slider':
        return (
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-sm text-muted-foreground">
                {field.min ?? 0}
              </span>
              <span className="text-sm font-medium">
                {value !== undefined && value !== null ? String(value) : String(field.default ?? '')}
              </span>
              <span className="text-sm text-muted-foreground">
                {field.max ?? 100}
              </span>
            </div>
            <Slider
              value={[Number(value ?? field.default ?? field.min ?? 0)]}
              onValueChange={([val]) => onChange(val)}
              min={field.min ?? 0}
              max={field.max ?? 100}
              step={field.step ?? 1}
            />
          </div>
        )

      case 'Select':
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
          <div className="border rounded-md overflow-hidden">
            <Editor
              height="200px"
              defaultLanguage="plaintext"
              value={String(value ?? '')}
              onChange={(val) => onChange(val ?? '')}
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
          <div className="border rounded-md overflow-hidden">
            <Editor
              height="200px"
              defaultLanguage="json"
              value={typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)}
              onChange={(val) => {
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
            value={value as any ?? null}
            onChange={(v) => onChange(v)}
          />
        )

      case 'Credential':
        return (
          <Select
            value={String(value ?? '')}
            onValueChange={onChange}
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
              {filteredCredentials.length === 0 && (
                <div className="px-2 py-1.5 text-sm text-muted-foreground">
                  No credentials available
                </div>
              )}
            </SelectContent>
          </Select>
        )

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
        {field.supportsVariables && (
          <span className="text-xs text-muted-foreground">Supports {'{{variables}}'}</span>
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
