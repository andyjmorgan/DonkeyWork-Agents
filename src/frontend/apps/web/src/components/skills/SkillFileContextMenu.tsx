import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@donkeywork/ui'
import { Copy, Edit, FilePlus, FolderPlus, Trash2 } from 'lucide-react'

interface SkillFileContextMenuProps {
  isDirectory: boolean
  children: React.ReactNode
  onRename: () => void
  onDelete: () => void
  onDuplicate?: () => void
  onNewFile?: () => void
  onNewFolder?: () => void
}

export function SkillFileContextMenu({
  isDirectory,
  children,
  onRename,
  onDelete,
  onDuplicate,
  onNewFile,
  onNewFolder,
}: SkillFileContextMenuProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {children}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {isDirectory && (
          <>
            <DropdownMenuItem onClick={onNewFile}>
              <FilePlus className="h-4 w-4" />
              New File
            </DropdownMenuItem>
            <DropdownMenuItem onClick={onNewFolder}>
              <FolderPlus className="h-4 w-4" />
              New Folder
            </DropdownMenuItem>
            <DropdownMenuSeparator />
          </>
        )}
        <DropdownMenuItem onClick={onRename}>
          <Edit className="h-4 w-4" />
          Rename
        </DropdownMenuItem>
        {!isDirectory && onDuplicate && (
          <DropdownMenuItem onClick={onDuplicate}>
            <Copy className="h-4 w-4" />
            Duplicate
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={onDelete} className="text-red-500 focus:text-red-500">
          <Trash2 className="h-4 w-4" />
          Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
