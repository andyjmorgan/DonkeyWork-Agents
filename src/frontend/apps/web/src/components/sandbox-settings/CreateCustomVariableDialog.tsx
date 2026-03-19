import { useState } from 'react'
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
import { sandboxCustomVariables } from '@donkeywork/api-client'

interface CreateCustomVariableDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated?: () => void
}

export function CreateCustomVariableDialog({
  open,
  onOpenChange,
  onCreated,
}: CreateCustomVariableDialogProps) {
  const [key, setKey] = useState('')
  const [value, setValue] = useState('')
  const [isSecret, setIsSecret] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const keyPattern = /^[A-Z0-9_]+$/

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    if (!keyPattern.test(key)) {
      setError('Key must contain only uppercase letters, numbers, and underscores.')
      return
    }

    setIsSubmitting(true)

    try {
      await sandboxCustomVariables.create({ key, value, isSecret })
      resetForm()
      onCreated?.()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create variable')
    } finally {
      setIsSubmitting(false)
    }
  }

  const resetForm = () => {
    setKey('')
    setValue('')
    setIsSecret(false)
    setError(null)
  }

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) resetForm()
    onOpenChange(newOpen)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>Add Custom Variable</DialogTitle>
          <DialogDescription>
            Define an environment variable that will be available in sandbox pods.
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
              <Label htmlFor="var-key">Key</Label>
              <Input
                id="var-key"
                placeholder="MY_API_KEY"
                value={key}
                onChange={(e) => setKey(e.target.value.toUpperCase().replace(/[^A-Z0-9_]/g, ''))}
                required
              />
              <p className="text-xs text-muted-foreground">
                Uppercase letters, numbers, and underscores only
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="var-value">Value</Label>
              <Input
                id="var-value"
                placeholder="Enter value..."
                type={isSecret ? 'password' : 'text'}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                required
              />
            </div>

            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="var-secret">Secret</Label>
                <p className="text-xs text-muted-foreground">
                  Secret values are masked in the UI
                </p>
              </div>
              <Switch
                id="var-secret"
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
            <Button type="submit" disabled={isSubmitting || !key || !value}>
              {isSubmitting ? 'Creating...' : 'Create Variable'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
