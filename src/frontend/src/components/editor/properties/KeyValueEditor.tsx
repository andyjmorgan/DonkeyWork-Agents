import { useCallback } from 'react'
import { Plus, Trash2, Variable, List } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export interface KeyValueItem {
  key: string
  value: string
}

export interface KeyValueCollection {
  useVariable: boolean
  variable?: string
  items: KeyValueItem[]
}

interface KeyValueEditorProps {
  id: string
  label: string
  description?: string
  required?: boolean
  value: KeyValueCollection | null
  onChange: (value: KeyValueCollection) => void
}

export function KeyValueEditor({
  id,
  label,
  description,
  required,
  value,
  onChange
}: KeyValueEditorProps) {
  // Ensure collection has all required properties with defaults
  const collection: KeyValueCollection = {
    useVariable: value?.useVariable ?? false,
    variable: value?.variable ?? '',
    items: value?.items ?? []
  }

  const handleModeChange = useCallback((useVariable: boolean) => {
    onChange({
      ...collection,
      useVariable
    })
  }, [collection, onChange])

  const handleVariableChange = useCallback((variable: string) => {
    onChange({
      ...collection,
      variable
    })
  }, [collection, onChange])

  const handleAddItem = useCallback(() => {
    onChange({
      ...collection,
      items: [...collection.items, { key: '', value: '' }]
    })
  }, [collection, onChange])

  const handleRemoveItem = useCallback((index: number) => {
    const newItems = collection.items.filter((_, i) => i !== index)
    onChange({
      ...collection,
      items: newItems
    })
  }, [collection, onChange])

  const handleItemChange = useCallback((index: number, field: 'key' | 'value', newValue: string) => {
    const newItems = collection.items.map((item, i) =>
      i === index ? { ...item, [field]: newValue } : item
    )
    onChange({
      ...collection,
      items: newItems
    })
  }, [collection, onChange])

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <Label htmlFor={id}>
          {label}
          {required && <span className="text-destructive ml-1">*</span>}
        </Label>
        <div className="flex items-center gap-1">
          <Button
            type="button"
            variant={!collection.useVariable ? "default" : "outline"}
            size="sm"
            onClick={() => handleModeChange(false)}
            className="h-7 px-2"
          >
            <List className="h-3 w-3 mr-1" />
            Manual
          </Button>
          <Button
            type="button"
            variant={collection.useVariable ? "default" : "outline"}
            size="sm"
            onClick={() => handleModeChange(true)}
            className="h-7 px-2"
          >
            <Variable className="h-3 w-3 mr-1" />
            Variable
          </Button>
        </div>
      </div>

      {collection.useVariable ? (
        <div className="space-y-2">
          <Input
            id={id}
            value={collection.variable ?? ''}
            onChange={(e) => handleVariableChange(e.target.value)}
            placeholder="{{headers}} or {{myVariable}}"
            className="font-mono text-sm bg-muted/50 border-border"
          />
        </div>
      ) : (
        <div className="space-y-2">
          {collection.items.length === 0 ? (
            <div className="rounded-md border border-dashed border-border bg-muted/30 p-4 text-center">
              <p className="text-sm text-muted-foreground">
                No items added yet
              </p>
            </div>
          ) : (
            <div className="space-y-2 rounded-md border border-border bg-muted/30 p-3">
              {collection.items.map((item, index) => (
                <div key={index} className="flex items-center gap-2">
                  <Input
                    value={item.key}
                    onChange={(e) => handleItemChange(index, 'key', e.target.value)}
                    placeholder="Key"
                    className="flex-1 bg-background border-border"
                  />
                  <Input
                    value={item.value}
                    onChange={(e) => handleItemChange(index, 'value', e.target.value)}
                    placeholder="Value"
                    className="flex-1 bg-background border-border"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => handleRemoveItem(index)}
                    className="shrink-0 text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              ))}
            </div>
          )}
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddItem}
            className="w-full"
          >
            <Plus className="h-4 w-4 mr-2" />
            Add Item
          </Button>
        </div>
      )}

      {description && (
        <p className="text-xs text-muted-foreground">{description}</p>
      )}

      <p className="text-xs text-blue-500">
        {collection.useVariable
          ? 'Use a variable expression that resolves to a key-value dictionary'
          : 'Keys and values support variable expressions like {{variable}}'}
      </p>
    </div>
  )
}
