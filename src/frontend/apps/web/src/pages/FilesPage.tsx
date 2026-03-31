import { useState, useEffect, useRef, useCallback } from 'react'
import { Trash2, Download, File, Folder, Upload, Loader2, ChevronRight } from 'lucide-react'
import {
  Button,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@donkeywork/ui'
import { files, type FileItem } from '@donkeywork/api-client'
import { FilePreviewPanel } from '../components/files/FilePreviewPanel'

export function FilesPage() {
  const [fileList, setFileList] = useState<FileItem[]>([])
  const [folders, setFolders] = useState<string[]>([])
  const [currentPrefix, setCurrentPrefix] = useState('')
  const [loading, setLoading] = useState(true)
  const [deletingName, setDeletingName] = useState<string | null>(null)
  const [deletingFolder, setDeletingFolder] = useState<string | null>(null)
  const [downloadingName, setDownloadingName] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [selectedFile, setSelectedFile] = useState<FileItem | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const loadFiles = useCallback(async () => {
    try {
      setLoading(true)
      const data = await files.list(currentPrefix || undefined)
      setFileList(data.files)
      setFolders(data.folders)
    } catch (error) {
      console.error('Failed to load files:', error)
    } finally {
      setLoading(false)
    }
  }, [currentPrefix])

  useEffect(() => {
    loadFiles()
  }, [loadFiles])

  const handleDelete = async (fileName: string) => {
    if (!confirm('Are you sure you want to delete this file? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingName(fileName)
      const deleteKey = currentPrefix ? `${currentPrefix}${fileName}` : fileName
      await files.delete(deleteKey)
      setFileList(prev => prev.filter(f => f.fileName !== fileName))
    } catch (error) {
      console.error('Failed to delete file:', error)
      alert('Failed to delete file')
    } finally {
      setDeletingName(null)
    }
  }

  const handleDeleteFolder = async (folderName: string) => {
    if (!confirm(`Are you sure you want to delete the folder "${folderName}" and all its contents? This action cannot be undone.`)) {
      return
    }

    try {
      setDeletingFolder(folderName)
      const folderPrefix = currentPrefix ? `${currentPrefix}${folderName}` : folderName
      await files.deleteFolder(folderPrefix)
      setFolders(prev => prev.filter(f => f !== folderName))
    } catch (error) {
      console.error('Failed to delete folder:', error)
      alert('Failed to delete folder')
    } finally {
      setDeletingFolder(null)
    }
  }

  const handleDownload = async (file: FileItem) => {
    try {
      setDownloadingName(file.fileName)
      const downloadKey = currentPrefix ? `${currentPrefix}${file.fileName}` : file.fileName
      const { blob, fileName } = await files.download(downloadKey)

      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to download file:', error)
      alert('Failed to download file')
    } finally {
      setDownloadingName(null)
    }
  }

  const handleUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = event.target.files
    if (!selectedFiles || selectedFiles.length === 0) return

    try {
      setUploading(true)
      for (const file of Array.from(selectedFiles)) {
        await files.upload(file)
      }
      await loadFiles()
    } catch (error) {
      console.error('Failed to upload file:', error)
      alert('Failed to upload file')
    } finally {
      setUploading(false)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  const navigateToFolder = (folderName: string) => {
    setCurrentPrefix(prev => prev + folderName + '/')
  }

  const navigateToRoot = () => {
    setCurrentPrefix('')
  }

  const navigateToBreadcrumb = (index: number) => {
    const segments = currentPrefix.split('/').filter(Boolean)
    setCurrentPrefix(segments.slice(0, index + 1).join('/') + '/')
  }

  const breadcrumbSegments = currentPrefix.split('/').filter(Boolean)

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
  }

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    })
  }

  const getFileTypeLabel = (fileName: string): string => {
    const ext = fileName.split('.').pop()?.toLowerCase()
    const typeMap: Record<string, string> = {
      'pdf': 'PDF',
      'png': 'PNG Image',
      'jpg': 'JPEG Image',
      'jpeg': 'JPEG Image',
      'gif': 'GIF Image',
      'webp': 'WebP Image',
      'txt': 'Text',
      'csv': 'CSV',
      'json': 'JSON',
      'zip': 'ZIP Archive',
      'tar': 'TAR Archive',
      'gz': 'GZIP Archive',
    }
    return typeMap[ext || ''] || ext?.toUpperCase() || 'File'
  }

  const isEmpty = folders.length === 0 && fileList.length === 0

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Files</h1>
          <p className="text-muted-foreground">
            Manage your uploaded files
          </p>
        </div>
      </div>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1 text-sm">
            <button
              onClick={navigateToRoot}
              className={`hover:text-foreground transition-colors ${currentPrefix ? 'text-muted-foreground hover:underline' : 'text-foreground font-semibold'}`}
            >
              Files
            </button>
            {breadcrumbSegments.map((segment, index) => (
              <span key={index} className="flex items-center gap-1">
                <ChevronRight className="h-3 w-3 text-muted-foreground" />
                <button
                  onClick={() => index < breadcrumbSegments.length - 1 ? navigateToBreadcrumb(index) : undefined}
                  className={`hover:text-foreground transition-colors ${index < breadcrumbSegments.length - 1 ? 'text-muted-foreground hover:underline' : 'text-foreground font-semibold'}`}
                >
                  {segment}
                </button>
              </span>
            ))}
          </div>
          <div>
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              onChange={handleUpload}
              multiple
            />
            <Button onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              {uploading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Uploading...
                </>
              ) : (
                <>
                  <Upload className="h-4 w-4 mr-2" />
                  Upload File
                </>
              )}
            </Button>
          </div>
        </div>

        {loading ? (
          <div className="flex items-center justify-center rounded-lg border border-border p-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground mr-2" />
            <p className="text-sm text-muted-foreground">Loading files...</p>
          </div>
        ) : isEmpty && !currentPrefix ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <File className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No files yet</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Upload your first file to get started
            </p>
            <Button className="mt-4" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              <Upload className="h-4 w-4 mr-2" />
              Upload File
            </Button>
          </div>
        ) : isEmpty && currentPrefix ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <Folder className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">Empty folder</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              This folder has no files or subfolders
            </p>
          </div>
        ) : (
          <>
            {/* Mobile view - card layout */}
            <div className="space-y-3 md:hidden">
              {folders.map((folder) => (
                <div
                  key={`folder-${folder}`}
                  className="rounded-lg border border-border bg-card p-4 hover:bg-accent/50 transition-colors"
                >
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => navigateToFolder(folder)}
                      className="flex items-center gap-2 flex-1 min-w-0 text-left"
                    >
                      <Folder className="h-4 w-4 text-blue-500 shrink-0" />
                      <span className="text-sm font-medium truncate">{folder}</span>
                      <ChevronRight className="h-4 w-4 text-muted-foreground ml-auto" />
                    </button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleDeleteFolder(folder)}
                      disabled={deletingFolder === folder}
                    >
                      {deletingFolder === folder ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Trash2 className="h-4 w-4 text-red-500" />
                      )}
                    </Button>
                  </div>
                </div>
              ))}
              {fileList.map((file) => (
                <div key={file.fileName} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-1 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <File className="h-4 w-4 text-amber-500 shrink-0" />
                        <button
                          className="text-sm font-medium truncate text-left hover:text-accent-foreground transition-colors cursor-pointer"
                          onClick={() => setSelectedFile(file)}
                        >
                          {file.fileName}
                        </button>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Type: </span>
                        <span>{getFileTypeLabel(file.fileName)}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Size: </span>
                        <span>{formatFileSize(file.sizeBytes)}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Modified: </span>
                        <span>{formatDate(file.lastModified)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDownload(file)}
                        disabled={downloadingName === file.fileName}
                      >
                        {downloadingName === file.fileName ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Download className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(file.fileName)}
                        disabled={deletingName === file.fileName}
                      >
                        {deletingName === file.fileName ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Trash2 className="h-4 w-4 text-red-500" />
                        )}
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Desktop view - table layout */}
            <div className="hidden md:block rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Type</TableHead>
                    <TableHead>Size</TableHead>
                    <TableHead>Modified</TableHead>
                    <TableHead className="w-[100px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {folders.map((folder) => (
                    <TableRow
                      key={`folder-${folder}`}
                      className="cursor-pointer hover:bg-accent/50"
                      onClick={() => navigateToFolder(folder)}
                    >
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Folder className="h-4 w-4 text-blue-500 shrink-0" />
                          <span className="font-medium">{folder}</span>
                        </div>
                      </TableCell>
                      <TableCell className="text-muted-foreground">Folder</TableCell>
                      <TableCell className="text-muted-foreground">-</TableCell>
                      <TableCell className="text-muted-foreground">-</TableCell>
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={(e) => { e.stopPropagation(); handleDeleteFolder(folder) }}
                          disabled={deletingFolder === folder}
                          title="Delete folder"
                        >
                          {deletingFolder === folder ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Trash2 className="h-4 w-4 text-red-500" />
                          )}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {fileList.map((file) => (
                    <TableRow key={file.fileName}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <File className="h-4 w-4 text-amber-500 shrink-0" />
                          <button
                            className="font-medium truncate max-w-[300px] text-left hover:text-accent-foreground transition-colors cursor-pointer"
                            onClick={() => setSelectedFile(file)}
                          >
                            {file.fileName}
                          </button>
                        </div>
                      </TableCell>
                      <TableCell>{getFileTypeLabel(file.fileName)}</TableCell>
                      <TableCell>{formatFileSize(file.sizeBytes)}</TableCell>
                      <TableCell>{formatDate(file.lastModified)}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDownload(file)}
                            disabled={downloadingName === file.fileName}
                            title="Download"
                          >
                            {downloadingName === file.fileName ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Download className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(file.fileName)}
                            disabled={deletingName === file.fileName}
                            title="Delete"
                          >
                            {deletingName === file.fileName ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Trash2 className="h-4 w-4 text-red-500" />
                            )}
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </>
        )}
      </section>

      <FilePreviewPanel
        file={selectedFile}
        currentPrefix={currentPrefix}
        open={selectedFile !== null}
        onClose={() => setSelectedFile(null)}
      />
    </div>
  )
}
