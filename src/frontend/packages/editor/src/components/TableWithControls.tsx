import { NodeViewContent, NodeViewWrapper } from '@tiptap/react'
import type { NodeViewProps } from '@tiptap/react'
import { useEffect, useRef, useState } from 'react'
import { GripVertical, GripHorizontal, Plus, Trash2 } from 'lucide-react'
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@donkeywork/ui'

export function TableWithControls({ editor, getPos, node }: NodeViewProps) {
  const [hoveredRow, setHoveredRow] = useState<number | null>(null)
  const [hoveredCol, setHoveredCol] = useState<number | null>(null)
  const [rowDropdownOpen, setRowDropdownOpen] = useState(false)
  const [colDropdownOpen, setColDropdownOpen] = useState(false)
  const tableRef = useRef<HTMLDivElement>(null)
  const clearTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    const wrapperElement = tableRef.current
    const tableElement = wrapperElement?.querySelector('table')
    if (!tableElement || !wrapperElement) return

    const handleMouseMove = (e: MouseEvent) => {
      const target = e.target as HTMLElement

      if (clearTimeoutRef.current) {
        clearTimeout(clearTimeoutRef.current)
        clearTimeoutRef.current = null
      }

      // Check if hovering over control buttons or dropdown menu - keep state
      const isOverControl = target.closest('.row-control, .column-control, [role="menu"], [data-radix-popper-content-wrapper]')
      if (isOverControl) {
        return
      }

      // Only update state if hovering over table cells
      const cell = target.closest('td, th')
      if (cell) {
        const row = cell.parentElement
        const rows = Array.from(tableElement.querySelectorAll('tr'))
        const cells = Array.from(row?.querySelectorAll('td, th') || [])

        if (row) {
          setHoveredRow(rows.indexOf(row as HTMLTableRowElement))
        }
        setHoveredCol(cells.indexOf(cell as HTMLTableCellElement))
      }
      // Don't clear state here - only clear on mouseleave from wrapper
    }

    const handleMouseLeave = () => {
      // Don't clear state if any dropdown is open
      if (rowDropdownOpen || colDropdownOpen) return

      // Delay clearing to allow moving to controls
      clearTimeoutRef.current = setTimeout(() => {
        setHoveredRow(null)
        setHoveredCol(null)
      }, 150)
    }

    wrapperElement.addEventListener('mousemove', handleMouseMove)
    wrapperElement.addEventListener('mouseleave', handleMouseLeave)

    return () => {
      wrapperElement.removeEventListener('mousemove', handleMouseMove)
      wrapperElement.removeEventListener('mouseleave', handleMouseLeave)
      if (clearTimeoutRef.current) {
        clearTimeout(clearTimeoutRef.current)
      }
    }
  }, [rowDropdownOpen, colDropdownOpen])

  const getCellPosition = (rowIndex: number, colIndex: number) => {
    if (typeof getPos !== 'function') return null

    const pos = getPos()
    if (pos === undefined) return null
    let currentPos = pos + 1 // Start inside table

    // Navigate to the specific row
    for (let i = 0; i <= rowIndex; i++) {
      if (i > 0) {
        const prevRow = node.child(i - 1)
        currentPos += prevRow.nodeSize
      }
    }

    // Navigate to the specific cell in the row
    const row = node.child(rowIndex)
    currentPos += 1 // Enter row

    for (let j = 0; j < colIndex; j++) {
      const cell = row.child(j)
      currentPos += cell.nodeSize
    }

    return currentPos
  }

  const handleAddRowAbove = (rowIndex: number) => {
    const cellPos = getCellPosition(rowIndex, 0)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).addRowBefore().run()
  }

  const handleAddRowBelow = (rowIndex: number) => {
    const cellPos = getCellPosition(rowIndex, 0)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).addRowAfter().run()
  }

  const handleDeleteRow = (rowIndex: number) => {
    if (node.childCount <= 1) return // Don't delete last row

    const cellPos = getCellPosition(rowIndex, 0)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).deleteRow().run()
  }

  const handleAddColumnLeft = (colIndex: number) => {
    const cellPos = getCellPosition(0, colIndex)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).addColumnBefore().run()
  }

  const handleAddColumnRight = (colIndex: number) => {
    const cellPos = getCellPosition(0, colIndex)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).addColumnAfter().run()
  }

  const handleDeleteColumn = (colIndex: number) => {
    const firstRow = node.child(0)
    if (firstRow.childCount <= 1) return // Don't delete last column

    const cellPos = getCellPosition(0, colIndex)
    if (cellPos === null) return

    editor.chain().focus().setTextSelection(cellPos).deleteColumn().run()
  }

  return (
    <NodeViewWrapper className="table-wrapper-enhanced" ref={tableRef}>
      <div className="table-content-wrapper">
        <NodeViewContent />

        {/* Row controls */}
        {hoveredRow !== null && (
          <div
            className="row-control"
            style={{
              top: `${(hoveredRow + 0.5) * 100 / (node.childCount || 1)}%`,
            }}
            contentEditable={false}
          >
            <DropdownMenu onOpenChange={setRowDropdownOpen}>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0 hover:bg-accent"
                >
                  <GripVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start">
                <DropdownMenuItem onClick={() => handleAddRowAbove(hoveredRow)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add row above
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleAddRowBelow(hoveredRow)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add row below
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => handleDeleteRow(hoveredRow)}
                  disabled={node.childCount <= 1}
                  className="text-destructive focus:text-destructive"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete row
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        )}

        {/* Column controls */}
        {hoveredCol !== null && node.childCount > 0 && (
          <div
            className="column-control"
            style={{
              left: `${(hoveredCol + 0.5) * 100 / (node.child(0).childCount || 1)}%`,
            }}
            contentEditable={false}
          >
            <DropdownMenu onOpenChange={setColDropdownOpen}>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-6 w-6 p-0 hover:bg-accent"
                >
                  <GripHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="center">
                <DropdownMenuItem onClick={() => handleAddColumnLeft(hoveredCol)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add column left
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleAddColumnRight(hoveredCol)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add column right
                </DropdownMenuItem>
                <DropdownMenuItem
                  onClick={() => handleDeleteColumn(hoveredCol)}
                  disabled={node.child(0).childCount <= 1}
                  className="text-destructive focus:text-destructive"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete column
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        )}
      </div>
    </NodeViewWrapper>
  )
}
