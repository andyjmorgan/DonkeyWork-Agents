import type { SkillFileNode } from '@donkeywork/api-client'
import { Button } from '@donkeywork/ui'
import { FilePlus, FolderPlus, Loader2 } from 'lucide-react'
import { SkillFileTreeNode } from './SkillFileTreeNode'

interface SkillFileTreeProps {
  tree: SkillFileNode[]
  loading: boolean
  selectedPath: string | null
  onSelect: (path: string) => void
  onRename: (path: string, isDirectory: boolean) => void
  onDelete: (path: string, isDirectory: boolean) => void
  onDuplicate: (path: string) => void
  onNewFile: (parentPath: string) => void
  onNewFolder: (parentPath: string) => void
}

export function SkillFileTree({
  tree,
  loading,
  selectedPath,
  onSelect,
  onRename,
  onDelete,
  onDuplicate,
  onNewFile,
  onNewFolder,
}: SkillFileTreeProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center p-8">
        <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center gap-1 px-2 py-2 border-b border-border">
        <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider flex-1">Files</span>
        <Button
          variant="ghost"
          size="sm"
          className="h-6 w-6 p-0"
          title="New File"
          onClick={() => onNewFile('')}
        >
          <FilePlus className="h-3.5 w-3.5" />
        </Button>
        <Button
          variant="ghost"
          size="sm"
          className="h-6 w-6 p-0"
          title="New Folder"
          onClick={() => onNewFolder('')}
        >
          <FolderPlus className="h-3.5 w-3.5" />
        </Button>
      </div>
      <div className="flex-1 overflow-y-auto py-1">
        {tree.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-muted-foreground">
            No files yet
          </div>
        ) : (
          tree.map((node) => (
            <SkillFileTreeNode
              key={node.name}
              node={node}
              depth={0}
              parentPath=""
              selectedPath={selectedPath}
              onSelect={onSelect}
              onRename={onRename}
              onDelete={onDelete}
              onDuplicate={onDuplicate}
              onNewFile={onNewFile}
              onNewFolder={onNewFolder}
            />
          ))
        )}
      </div>
    </div>
  )
}
