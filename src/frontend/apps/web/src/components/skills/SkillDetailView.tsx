import { useState, useEffect, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { skills, type SkillFileNode, type WriteFileRequest } from '@donkeywork/api-client'
import { Button } from '@donkeywork/ui'
import { ArrowLeft, FileText, Download, Loader2 } from 'lucide-react'
import { SkillFileTree } from './SkillFileTree'
import { SkillFileEditor } from './SkillFileEditor'
import { SkillRenameDialog } from './SkillRenameDialog'
import { SkillNewItemDialog } from './SkillNewItemDialog'

interface SkillDetailViewProps {
  name: string
}

export function SkillDetailView({ name }: SkillDetailViewProps) {
  const [tree, setTree] = useState<SkillFileNode[]>([])
  const [treeLoading, setTreeLoading] = useState(true)
  const [selectedPath, setSelectedPath] = useState<string | null>(null)
  const [fileContent, setFileContent] = useState<string | null>(null)
  const [editedContent, setEditedContent] = useState<string | null>(null)
  const [fileLoading, setFileLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  // Rename dialog state
  const [renameTarget, setRenameTarget] = useState<{ path: string; isDirectory: boolean } | null>(null)
  const renameCurrentName = renameTarget?.path.split('/').pop() || ''

  // New item dialog state
  const [newItemTarget, setNewItemTarget] = useState<{ parentPath: string; mode: 'file' | 'folder' } | null>(null)
  const [downloading, setDownloading] = useState(false)

  const isDirty = editedContent !== null && editedContent !== fileContent

  const loadTree = useCallback(async () => {
    try {
      setTreeLoading(true)
      const data = await skills.getContents(name)
      setTree(data)
    } catch (err) {
      console.error('Failed to load file tree:', err)
    } finally {
      setTreeLoading(false)
    }
  }, [name])

  useEffect(() => {
    loadTree()
  }, [loadTree])

  const loadFile = useCallback(async (path: string) => {
    try {
      setFileLoading(true)
      const data = await skills.readFile(name, path)
      setFileContent(data.content)
      setEditedContent(null)
    } catch (err) {
      console.error('Failed to load file:', err)
      setFileContent(null)
      setEditedContent(null)
    } finally {
      setFileLoading(false)
    }
  }, [name])

  const handleSelectFile = useCallback((path: string) => {
    if (isDirty) {
      if (!confirm('You have unsaved changes. Discard them?')) {
        return
      }
    }
    setSelectedPath(path)
    setEditedContent(null)
    loadFile(path)
  }, [isDirty, loadFile])

  const handleSave = useCallback(async () => {
    if (!selectedPath || editedContent === null) return
    try {
      setSaving(true)
      const request: WriteFileRequest = { content: editedContent }
      await skills.writeFile(name, selectedPath, request)
      setFileContent(editedContent)
      setEditedContent(null)
    } catch (err) {
      console.error('Failed to save file:', err)
      alert('Failed to save file')
    } finally {
      setSaving(false)
    }
  }, [name, selectedPath, editedContent])

  const handleDiscard = useCallback(() => {
    setEditedContent(null)
  }, [])

  const handleRename = useCallback(async (newName: string) => {
    if (!renameTarget) return
    await skills.rename(name, renameTarget.path, { newName })
    // If the renamed item was selected, update selectedPath
    if (selectedPath && (selectedPath === renameTarget.path || selectedPath.startsWith(renameTarget.path + '/'))) {
      const pathParts = renameTarget.path.split('/')
      pathParts[pathParts.length - 1] = newName
      const newPath = pathParts.join('/')
      if (selectedPath === renameTarget.path) {
        setSelectedPath(newPath)
      } else {
        setSelectedPath(selectedPath.replace(renameTarget.path, newPath))
      }
    }
    await loadTree()
  }, [name, renameTarget, selectedPath, loadTree])

  const handleDelete = useCallback(async (path: string, isDirectory: boolean) => {
    const itemType = isDirectory ? 'folder' : 'file'
    if (!confirm(`Are you sure you want to delete this ${itemType}? This action cannot be undone.`)) {
      return
    }
    try {
      if (isDirectory) {
        await skills.deleteFolder(name, path)
      } else {
        await skills.deleteFile(name, path)
      }
      if (selectedPath && (selectedPath === path || selectedPath.startsWith(path + '/'))) {
        setSelectedPath(null)
        setFileContent(null)
        setEditedContent(null)
      }
      await loadTree()
    } catch (err) {
      console.error(`Failed to delete ${itemType}:`, err)
      alert(`Failed to delete ${itemType}`)
    }
  }, [name, selectedPath, loadTree])

  const handleDuplicate = useCallback(async (path: string) => {
    try {
      await skills.duplicateFile(name, path)
      await loadTree()
    } catch (err) {
      console.error('Failed to duplicate file:', err)
      alert('Failed to duplicate file')
    }
  }, [name, loadTree])

  const handleNewFile = useCallback(async (fileName: string) => {
    if (!newItemTarget) return
    const fullPath = newItemTarget.parentPath ? `${newItemTarget.parentPath}/${fileName}` : fileName
    await skills.writeFile(name, fullPath, { content: '' })
    await loadTree()
    setSelectedPath(fullPath)
    setFileContent('')
    setEditedContent(null)
  }, [name, newItemTarget, loadTree])

  const handleNewFolder = useCallback(async (folderName: string) => {
    if (!newItemTarget) return
    const fullPath = newItemTarget.parentPath ? `${newItemTarget.parentPath}/${folderName}` : folderName
    await skills.createFolder(name, fullPath)
    await loadTree()
  }, [name, newItemTarget, loadTree])

  return (
    <div className="flex flex-col h-[calc(100vh-8rem)]">
      <div className="flex items-center gap-3 pb-4 shrink-0">
        <Link to="/skills">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-1" />
            Back
          </Button>
        </Link>
        <h1 className="text-xl font-bold flex-1">{name}</h1>
        <Button
          variant="outline"
          size="sm"
          disabled={downloading}
          onClick={async () => {
            try {
              setDownloading(true)
              await skills.download(name)
            } catch (err) {
              console.error('Failed to download skill:', err)
            } finally {
              setDownloading(false)
            }
          }}
        >
          {downloading ? (
            <Loader2 className="h-4 w-4 mr-1 animate-spin" />
          ) : (
            <Download className="h-4 w-4 mr-1" />
          )}
          Download
        </Button>
      </div>

      <div className="flex flex-1 border border-border rounded-lg overflow-hidden min-h-0">
        {/* Sidebar - file tree */}
        <div className="w-64 border-r border-border shrink-0 overflow-hidden flex flex-col">
          <SkillFileTree
            tree={tree}
            loading={treeLoading}
            selectedPath={selectedPath}
            onSelect={handleSelectFile}
            onRename={(path, isDir) => setRenameTarget({ path, isDirectory: isDir })}
            onDelete={handleDelete}
            onDuplicate={handleDuplicate}
            onNewFile={(parentPath) => setNewItemTarget({ parentPath, mode: 'file' })}
            onNewFolder={(parentPath) => setNewItemTarget({ parentPath, mode: 'folder' })}
          />
        </div>

        {/* Main editor area */}
        <div className="flex-1 min-w-0 flex flex-col">
          {fileLoading ? (
            <div className="flex items-center justify-center h-full">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-muted border-t-foreground" />
            </div>
          ) : selectedPath && fileContent !== null ? (
            <SkillFileEditor
              path={selectedPath}
              content={editedContent ?? fileContent}
              onChange={setEditedContent}
              onSave={handleSave}
              onDiscard={handleDiscard}
              saving={saving}
              isDirty={isDirty}
            />
          ) : (
            <div className="flex flex-col items-center justify-center h-full text-muted-foreground">
              <FileText className="h-12 w-12 mb-3 opacity-30" />
              <p className="text-sm">Select a file to edit</p>
            </div>
          )}
        </div>
      </div>

      {/* Rename dialog */}
      <SkillRenameDialog
        open={renameTarget !== null}
        onOpenChange={(open) => { if (!open) setRenameTarget(null) }}
        currentName={renameCurrentName}
        onRename={handleRename}
      />

      {/* New item dialog */}
      <SkillNewItemDialog
        open={newItemTarget !== null}
        onOpenChange={(open) => { if (!open) setNewItemTarget(null) }}
        mode={newItemTarget?.mode ?? 'file'}
        onCreate={newItemTarget?.mode === 'folder' ? handleNewFolder : handleNewFile}
      />
    </div>
  )
}
