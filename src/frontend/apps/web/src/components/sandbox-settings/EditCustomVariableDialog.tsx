import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Input,
  Label,
  Switch,
} from '@donkeywork/ui'
import { sandboxCustomVariables, type SandboxCustomVariable } from '@donkeywork/api-client'

interface EditCustomVariableDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  variable: SandboxCustomVariable
  onUpdated?: () => void
}

export function EditCustomVariableDialog({
  open,
  onOpenChange,
  variable,
  onUpdated,
}: EditCustomVariableDialogProps) {
  const [value, setValue] = useState('')
  const [isSecret, setIsSecret] = useState(variable.isSecret)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setValue(variable.isSecret ? '' : variable.value)
    setIsSecret(variable.isSecret)
    setError(null)
  }, [variable])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await sandboxCustomVariables.update(variable.id, {
        value: value || undefined,
        isSecret,
      })
      onUpdated?.()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update variable')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) setError(null)
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Edit Custom Variable</DialogTitle>
          <DialogDescription>
            Update the value for <span className="font-medium">{variable.key}</span>
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="space-y-4 py-4">
            {error && (
              <div className="rounded-md bg-red-500/10 border border-red-500/20 p-3 text-sm text-red-500">
                {error}
              </div>
            )}

            <div className="space-y-2">
              <Label>Key</Label>
              <div className="rounded-md border border-input bg-muted/50 px-3 py-2 text-sm">
                {variable.key}
              </div>
              <p className="text-xs text-muted-foreground">
                Key cannot be changed. Delete and recreate to use a different key.
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="edit-var-value">Value</Label>
              <Input
                id="edit-var-value"
                placeholder={variable.isSecret ? 'Enter new value to replace...' : 'Enter value...'}
                type={isSecret ? 'password' : 'text'}
                value={value}
                onChange={(e) => setValue(e.target.value)}
              />
              {variable.isSecret && (
                <p className="text-xs text-muted-foreground">
                  Leave empty to keep the current value
                </p>
              )}
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="edit-var-secret">Secret</Label>
                <p className="text-xs text-muted-foreground">
                  Secret values are masked in the UI
                </p>
              </div>
              <Switch
                id="edit-var-secret"
                checked={isSecret}
                onCheckedChange={setIsSecret}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Saving...' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
