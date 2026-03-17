import { useState } from 'react'
import type { SkillFileNode } from '@donkeywork/api-client'
import { Button } from '@donkeywork/ui'
import { ChevronRight, FileText, Folder, FolderOpen, MoreHorizontal } from 'lucide-react'
import { cn } from '@/lib/utils'
import { SkillFileContextMenu } from './SkillFileContextMenu'

interface SkillFileTreeNodeProps {
  node: SkillFileNode
  depth: number
  parentPath: string
  selectedPath: string | null
  onSelect: (path: string) => void
  onRename: (path: string, isDirectory: boolean) => void
  onDelete: (path: string, isDirectory: boolean) => void
  onDuplicate: (path: string) => void
  onNewFile: (parentPath: string) => void
  onNewFolder: (parentPath: string) => void
}

export function SkillFileTreeNode({
  node,
  depth,
  parentPath,
  selectedPath,
  onSelect,
  onRename,
  onDelete,
  onDuplicate,
  onNewFile,
  onNewFolder,
}: SkillFileTreeNodeProps) {
  const [expanded, setExpanded] = useState(false)
  const fullPath = parentPath ? `${parentPath}/${node.name}` : node.name
  const isSelected = selectedPath === fullPath

  if (!node.isDirectory) {
    return (
      <div
        className={cn(
          'group flex items-center gap-1 py-1 px-2 text-sm rounded cursor-pointer hover:bg-muted/50',
          isSelected && 'bg-accent text-accent-foreground'
        )}
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
        onClick={() => onSelect(fullPath)}
      >
        <FileText className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        <span className="truncate flex-1">{node.name}</span>
        <SkillFileContextMenu
          isDirectory={false}
          onRename={() => onRename(fullPath, false)}
          onDelete={() => onDelete(fullPath, false)}
          onDuplicate={() => onDuplicate(fullPath)}
        >
          <Button
            variant="ghost"
            size="sm"
            className="h-5 w-5 p-0 opacity-0 group-hover:opacity-100"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreHorizontal className="h-3.5 w-3.5" />
          </Button>
        </SkillFileContextMenu>
      </div>
    )
  }

  return (
    <div>
      <div
        className={cn(
          'group flex items-center gap-1 py-1 px-2 text-sm rounded cursor-pointer hover:bg-muted/50',
          isSelected && 'bg-accent text-accent-foreground'
        )}
        style={{ paddingLeft: `${depth * 16 + 8}px` }}
        onClick={() => setExpanded(!expanded)}
      >
        <ChevronRight className={cn('h-3.5 w-3.5 shrink-0 transition-transform', expanded && 'rotate-90')} />
        {expanded ? (
          <FolderOpen className="h-3.5 w-3.5 text-amber-500 shrink-0" />
        ) : (
          <Folder className="h-3.5 w-3.5 text-amber-500 shrink-0" />
        )}
        <span className="truncate flex-1">{node.name}</span>
        <SkillFileContextMenu
          isDirectory={true}
          onRename={() => onRename(fullPath, true)}
          onDelete={() => onDelete(fullPath, true)}
          onNewFile={() => onNewFile(fullPath)}
          onNewFolder={() => onNewFolder(fullPath)}
        >
          <Button
            variant="ghost"
            size="sm"
            className="h-5 w-5 p-0 opacity-0 group-hover:opacity-100"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreHorizontal className="h-3.5 w-3.5" />
          </Button>
        </SkillFileContextMenu>
      </div>
      {expanded && node.children?.map((child) => (
        <SkillFileTreeNode
          key={child.name}
          node={child}
          depth={depth + 1}
          parentPath={fullPath}
          selectedPath={selectedPath}
          onSelect={onSelect}
          onRename={onRename}
          onDelete={onDelete}
          onDuplicate={onDuplicate}
          onNewFile={onNewFile}
          onNewFolder={onNewFolder}
        />
      ))}
    </div>
  )
}
