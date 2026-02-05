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
import { files, type StoredFileItem, type PaginatedResponse } from '@/lib/api'
import { ChevronLeft, ChevronRight } from 'lucide-react'

const PAGE_SIZE = 20

export function FilesPage() {
  const [allFiles, setAllFiles] = useState<StoredFileItem[]>([])
  const [loading, setLoading] = useState(true)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [downloadingId, setDownloadingId] = useState<string | null>(null)
  const [copyingId, setCopyingId] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [page, setPage] = useState(0)
  const [totalCount, setTotalCount] = useState(0)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const loadFiles = async () => {
    try {
      setLoading(true)
      const data: PaginatedResponse<StoredFileItem> = await files.list(page * PAGE_SIZE, PAGE_SIZE)
      setAllFiles(data.items)
      setTotalCount(data.totalCount)
    } catch (error) {
      console.error('Failed to load files:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadFiles()
  }, [page])

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this file? This action cannot be undone.')) {
      return
    }

    try {
      setDeletingId(id)
      await files.delete(id)
      setAllFiles(prev => prev.filter(f => f.id !== id))
      setTotalCount(prev => prev - 1)
    } catch (error) {
      console.error('Failed to delete file:', error)
      alert('Failed to delete file')
    } finally {
      setDeletingId(null)
    }
  }

  const handleDownload = async (file: StoredFileItem) => {
    try {
      setDownloadingId(file.id)
      const { blob, fileName } = await files.download(file.id)

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
      setDownloadingId(null)
    }
  }

  const handleCopyLink = async (id: string) => {
    try {
      setCopyingId(id)
      const result = await files.getPublicUrl(id, 60) // 1 hour expiry
      await navigator.clipboard.writeText(result.url)
      alert('Link copied to clipboard. Expires in 1 hour.')
    } catch (error) {
      console.error('Failed to get public URL:', error)
      alert('Failed to copy link')
    } finally {
      setCopyingId(null)
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

  const getFileTypeLabel = (contentType: string): string => {
    const typeMap: Record<string, string> = {
      'application/pdf': 'PDF',
      'image/png': 'PNG Image',
      'image/jpeg': 'JPEG Image',
      'image/gif': 'GIF Image',
      'image/webp': 'WebP Image',
      'text/plain': 'Text',
      'text/csv': 'CSV',
      'application/json': 'JSON',
      'application/zip': 'ZIP Archive',
      'application/x-tar': 'TAR Archive',
      'application/gzip': 'GZIP Archive',
    }
    return typeMap[contentType] || contentType.split('/')[1]?.toUpperCase() || 'File'
  }

  const totalPages = Math.ceil(totalCount / PAGE_SIZE)
  const canGoBack = page > 0
  const canGoForward = page < totalPages - 1

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
                <div key={file.id} className="rounded-lg border border-border bg-card p-4 space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="space-y-1 min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <File className="h-4 w-4 text-amber-500 shrink-0" />
                        <span className="text-sm font-medium truncate">{file.fileName}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Type: </span>
                        <span>{getFileTypeLabel(file.contentType)}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Size: </span>
                        <span>{formatFileSize(file.sizeBytes)}</span>
                      </div>
                      <div className="text-sm">
                        <span className="text-muted-foreground">Uploaded: </span>
                        <span>{formatDate(file.createdAt)}</span>
                      </div>
                    </div>
                    <div className="flex items-center gap-1 shrink-0">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDownload(file)}
                        disabled={downloadingId === file.id}
                      >
                        {downloadingId === file.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Download className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleCopyLink(file.id)}
                        disabled={copyingId === file.id}
                      >
                        {copyingId === file.id ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <LinkIcon className="h-4 w-4" />
                        )}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(file.id)}
                        disabled={deletingId === file.id}
                      >
                        {deletingId === file.id ? (
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
                    <TableHead>Uploaded</TableHead>
                    <TableHead className="w-[140px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {allFiles.map((file) => (
                    <TableRow key={file.id}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <File className="h-4 w-4 text-amber-500 shrink-0" />
                          <span className="font-medium truncate max-w-[300px]">{file.fileName}</span>
                        </div>
                      </TableCell>
                      <TableCell>{getFileTypeLabel(file.contentType)}</TableCell>
                      <TableCell>{formatFileSize(file.sizeBytes)}</TableCell>
                      <TableCell>{formatDate(file.createdAt)}</TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDownload(file)}
                            disabled={downloadingId === file.id}
                            title="Download"
                          >
                            {downloadingId === file.id ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <Download className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleCopyLink(file.id)}
                            disabled={copyingId === file.id}
                            title="Copy Link"
                          >
                            {copyingId === file.id ? (
                              <Loader2 className="h-4 w-4 animate-spin" />
                            ) : (
                              <LinkIcon className="h-4 w-4" />
                            )}
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(file.id)}
                            disabled={deletingId === file.id}
                            title="Delete"
                          >
                            {deletingId === file.id ? (
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

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between pt-4">
                <p className="text-sm text-muted-foreground">
                  Showing {page * PAGE_SIZE + 1}-{Math.min((page + 1) * PAGE_SIZE, totalCount)} of {totalCount}
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPage(p => p - 1)}
                    disabled={!canGoBack}
                  >
                    <ChevronLeft className="h-4 w-4 mr-1" />
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPage(p => p + 1)}
                    disabled={!canGoForward}
                  >
                    Next
                    <ChevronRight className="h-4 w-4 ml-1" />
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  )
}
