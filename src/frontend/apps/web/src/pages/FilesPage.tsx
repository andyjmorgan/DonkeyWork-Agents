import { useState, useEffect, useRef } from 'react'
import { Plus, Trash2, Download, Link as LinkIcon, File, Upload, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { files, type FileItem } from '@/lib/api'

export function FilesPage() {
  const [allFiles, setAllFiles] = useState<FileItem[]>([])
  const [loading, setLoading] = useState(true)
  const [deletingName, setDeletingName] = useState<string | null>(null)
  const [downloadingName, setDownloadingName] = useState<string | null>(null)
  const [copyingName, setCopyingName] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const loadFiles = async () => {
    try {
      setLoading(true)
      const data = await files.list()
      setAllFiles(data)
    } catch (error) {
      console.error('Failed to load files:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadFiles()
  }, [])

  const handleDelete = async (fileName: string) => {
    if (!confirm('Are you sure you want to delete this file? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingName(fileName)
      await files.delete(fileName)
      setAllFiles(prev => prev.filter(f => f.fileName !== fileName))
    } catch (error) {
      console.error('Failed to delete file:', error)
      alert('Failed to delete file')
    } finally {
      setDeletingName(null)
    }
  }

  const handleDownload = async (file: FileItem) => {
    try {
      setDownloadingName(file.fileName)
      const { blob, fileName } = await files.download(file.fileName)

      // Create download link
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

  const handleCopyLink = async (fileName: string) => {
    try {
      setCopyingName(fileName)
      const result = await files.getPublicUrl(fileName, 60) // 1 hour expiry
      await navigator.clipboard.writeText(result.url)
      alert('Link copied to clipboard. Expires in 1 hour.')
    } catch (error) {
      console.error('Failed to get public URL:', error)
      alert('Failed to copy link')
    } finally {
      setCopyingName(null)
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
      // Reload to get updated list
      await loadFiles()
    } catch (error) {
      console.error('Failed to upload file:', error)
      alert('Failed to upload file')
    } finally {
      setUploading(false)
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

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
          <h2 className="text-lg font-semibold">All Files</h2>
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
        ) : allFiles.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border p-12 text-center">
            <div className="rounded-full bg-muted p-4">
              <File className="h-8 w-8 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-semibold">No files yet</h3>
            <p className="mt-2 text-sm text-muted-foreground max-w-sm">
              Upload your first file to get started
            </p>
            <Button className="mt-4" onClick={() => fileInputRef.current?.click()} disabled={uploading}>
              <Plus className="h-4 w-4 mr-2" />
              Upload File
            </Button>
          </div>
        ) : (
          <>
            {/* Mobile view - card layout */}
            <div className="space-y-3 md:hidden">
              {allFiles.map((file) => (
                <div key={file.fileName} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-1 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <File className="h-4 w-4 text-amber-500 shrink-0" />
                        <span className="text-sm font-medium truncate">{file.fileName}</span>
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
                        onClick={() => handleCopyLink(file.fileName)}
                        disabled={copyingName === file.fileName}
                      >
                        {copyingName === file.fileName ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <LinkIcon className="h-4 w-4" />
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
                    <TableHead className="w-[140px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {allFiles.map((file) => (
                    <TableRow key={file.fileName}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <File className="h-4 w-4 text-amber-500 shrink-0" />
                          <span className="font-medium truncate max-w-[300px]">{file.fileName}</span>
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
                            onClick={() => handleCopyLink(file.fileName)}
                            disabled={copyingName === file.fileName}
                            title="Copy Link"
                          >
                            {copyingName === file.fileName ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <LinkIcon className="h-4 w-4" />
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
    </div>
  )
}
