import { useState } from 'react'
import { Copy, Download, Check } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Button,
} from '@donkeywork/ui'
import { JsonViewer } from '@/components/ui/json-viewer'

interface ExportJsonDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  json: string
  filename: string
}

export function ExportJsonDialog({
  open,
  onOpenChange,
  json,
  filename
}: ExportJsonDialogProps) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(json)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (error) {
      console.error('Failed to copy:', error)
    }
  }

  const handleDownload = () => {
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }

  let parsedJson: unknown = null
  try {
    parsedJson = JSON.parse(json)
  } catch {
    parsedJson = { error: 'Invalid JSON', raw: json }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Export Agent</DialogTitle>
        </DialogHeader>
        <div className="flex-1 overflow-auto min-h-0 my-4">
          <JsonViewer
            data={parsedJson}
            collapsed={2}
            className="max-h-[50vh]"
          />
        </div>
        <DialogFooter className="flex gap-2 sm:gap-2">
          <Button
            variant="outline"
            onClick={handleCopy}
            className="flex items-center gap-2"
          >
            {copied ? (
              <>
                <Check className="h-4 w-4" />
                Copied
              </>
            ) : (
              <>
                <Copy className="h-4 w-4" />
                Copy
              </>
            )}
          </Button>
          <Button
            onClick={handleDownload}
            className="flex items-center gap-2"
          >
            <Download className="h-4 w-4" />
            Save File
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
