import type { ReactNode } from 'react'
import { Settings, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useEditorStore } from '@/store/editor'

interface BaseNodeProps {
  id: string
  selected?: boolean
  borderColor: string
  children: ReactNode
  canDelete?: boolean
}

export function BaseNode({
  id,
  selected,
  borderColor,
  children,
  canDelete = true
}: BaseNodeProps) {
  const selectNode = useEditorStore((state) => state.selectNode)
  const removeNode = useEditorStore((state) => state.removeNode)

  const handleConfigClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    selectNode(id)
  }

  const handleDeleteClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    if (confirm('Are you sure you want to delete this node?')) {
      removeNode(id)
    }
  }

  return (
    <div
      className={cn(
        'px-4 py-3 rounded-2xl border-2 bg-card shadow-lg min-w-[180px] transition-all relative group',
        borderColor.replace('border-', 'border-').replace('-500', '-500/50'),
        selected && `ring-2 ${borderColor.replace('border-', 'ring-')} ring-offset-2 ring-offset-background`
      )}
    >
      {/* Action buttons */}
      <div className="absolute -top-2 -right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-10">
        <button
          onClick={handleConfigClick}
          className="h-6 w-6 rounded-full bg-card border border-border shadow-sm flex items-center justify-center hover:bg-accent"
        >
          <Settings className="h-3.5 w-3.5" />
        </button>
        {canDelete && (
          <button
            onClick={handleDeleteClick}
            className="h-6 w-6 rounded-full bg-card border border-border shadow-sm flex items-center justify-center hover:bg-accent hover:text-red-500"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {children}
    </div>
  )
}
