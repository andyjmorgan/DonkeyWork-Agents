import { useState, useMemo, useId } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Minus, Plus } from 'lucide-react'

interface TableDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onInsertTable: (rows: number, cols: number, markdown: string) => void
}

const MIN_ROWS = 2
const MAX_ROWS = 20
const MIN_COLS = 2
const MAX_COLS = 10
const DEFAULT_ROWS = 3
const DEFAULT_COLS = 3

// Generate initial headers array
function generateInitialHeaders(cols: number): string[] {
  return Array.from({ length: cols }, (_, i) => `Column ${i + 1}`)
}

// Inner component that resets when key changes
function TableDialogContent({
  onInsertTable,
  onClose,
}: {
  onInsertTable: (rows: number, cols: number, markdown: string) => void
  onClose: () => void
}) {
  const [rows, setRows] = useState(DEFAULT_ROWS)
  const [cols, setCols] = useState(DEFAULT_COLS)
  const [headers, setHeaders] = useState<string[]>(() => generateInitialHeaders(DEFAULT_COLS))

  const updateHeader = (index: number, value: string) => {
    setHeaders(prev => {
      const newHeaders = [...prev]
      newHeaders[index] = value
      return newHeaders
    })
  }

  // Handle cols change - update headers array
  const handleColsChange = (newCols: number) => {
    setCols(newCols)
    setHeaders(prev => {
      const newHeaders = [...prev]
      while (newHeaders.length < newCols) {
        newHeaders.push(`Column ${newHeaders.length + 1}`)
      }
      return newHeaders.slice(0, newCols)
    })
  }

  // Generate markdown table
  const markdown = useMemo(() => {
    const headerRow = '| ' + headers.map(h => h || 'Column').join(' | ') + ' |'
    const separatorRow = '| ' + headers.map(() => '---').join(' | ') + ' |'
    const dataRows = Array(rows - 1)
      .fill(null)
      .map(() => '| ' + Array(cols).fill('').join(' | ') + ' |')

    return [headerRow, separatorRow, ...dataRows].join('\n')
  }, [rows, cols, headers])

  const handleInsert = () => {
    onInsertTable(rows, cols, markdown)
    onClose()
  }

  const adjustRows = (delta: number) => {
    setRows(prev => Math.min(MAX_ROWS, Math.max(MIN_ROWS, prev + delta)))
  }

  const adjustCols = (delta: number) => {
    handleColsChange(Math.min(MAX_COLS, Math.max(MIN_COLS, cols + delta)))
  }

  return (
    <>
      <DialogHeader className="px-6 pt-6 pb-4">
        <DialogTitle className="text-xl">Insert Table</DialogTitle>
        <DialogDescription className="sr-only">
          Create and insert a markdown table with live preview
        </DialogDescription>
      </DialogHeader>

      <div className="grid grid-cols-2 gap-6 px-6 pb-6 flex-1 overflow-hidden">
        {/* Left: Configuration */}
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-3">
            <span className="text-sm font-medium">Table Size</span>

            {/* Rows control */}
            <div className="flex items-center gap-3">
              <Label className="w-16 text-sm text-muted-foreground">Rows</Label>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => adjustRows(-1)}
                  disabled={rows <= MIN_ROWS}
                >
                  <Minus className="h-4 w-4" />
                </Button>
                <Input
                  type="number"
                  min={MIN_ROWS}
                  max={MAX_ROWS}
                  value={rows}
                  onChange={(e) => setRows(Math.min(MAX_ROWS, Math.max(MIN_ROWS, parseInt(e.target.value) || MIN_ROWS)))}
                  className="w-16 text-center"
                />
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => adjustRows(1)}
                  disabled={rows >= MAX_ROWS}
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {/* Columns control */}
            <div className="flex items-center gap-3">
              <Label className="w-16 text-sm text-muted-foreground">Columns</Label>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => adjustCols(-1)}
                  disabled={cols <= MIN_COLS}
                >
                  <Minus className="h-4 w-4" />
                </Button>
                <Input
                  type="number"
                  min={MIN_COLS}
                  max={MAX_COLS}
                  value={cols}
                  onChange={(e) => handleColsChange(Math.min(MAX_COLS, Math.max(MIN_COLS, parseInt(e.target.value) || MIN_COLS)))}
                  className="w-16 text-center"
                />
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => adjustCols(1)}
                  disabled={cols >= MAX_COLS}
                >
                  <Plus className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>

          {/* Header names */}
          <div className="flex flex-col gap-3">
            <span className="text-sm font-medium">Column Headers</span>
            <div className="flex flex-col gap-2 max-h-[200px] overflow-y-auto pr-2">
              {headers.map((header, index) => (
                <Input
                  key={index}
                  value={header}
                  onChange={(e) => updateHeader(index, e.target.value)}
                  placeholder={`Column ${index + 1}`}
                  className="text-sm"
                />
              ))}
            </div>
          </div>

          {/* Markdown output */}
          <div className="flex flex-col gap-2 flex-1">
            <span className="text-sm font-medium">Markdown</span>
            <pre className="flex-1 p-3 rounded-md bg-muted/50 border text-xs font-mono overflow-auto whitespace-pre">
              {markdown}
            </pre>
          </div>
        </div>

        {/* Right: Preview */}
        <div className="flex flex-col gap-3">
          <span className="text-sm font-medium">Preview</span>
          <div className="border rounded-md bg-background/50 flex-1 overflow-auto p-4">
            <table className="w-full border-collapse">
              <thead>
                <tr>
                  {headers.map((header, index) => (
                    <th
                      key={index}
                      className="border border-border px-3 py-2 text-left text-sm font-medium bg-muted/50"
                    >
                      {header || `Column ${index + 1}`}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {Array(rows - 1)
                  .fill(null)
                  .map((_, rowIndex) => (
                    <tr key={rowIndex}>
                      {Array(cols)
                        .fill(null)
                        .map((_, colIndex) => (
                          <td
                            key={colIndex}
                            className="border border-border px-3 py-2 text-sm text-muted-foreground"
                          >
                            &nbsp;
                          </td>
                        ))}
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <DialogFooter className="px-6 pb-6 pt-2 gap-2">
        <Button variant="ghost" onClick={onClose}>
          Cancel
        </Button>
        <Button onClick={handleInsert}>Insert Table</Button>
      </DialogFooter>
    </>
  )
}

export function TableDialog({ open, onOpenChange, onInsertTable }: TableDialogProps) {
  // Use unique key each time dialog opens to reset state
  const dialogKey = useId()

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-4xl max-h-[85vh] p-0 gap-0">
        {/* Use key to reset internal state when dialog opens */}
        <TableDialogContent
          key={open ? dialogKey : 'closed'}
          onInsertTable={onInsertTable}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  )
}
