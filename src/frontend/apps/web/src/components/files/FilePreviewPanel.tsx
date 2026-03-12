import { useState, useEffect } from 'react'
import { Download, Loader2, FileText, Eye } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  Button,
  Badge,
} from '@donkeywork/ui'
import { MarkdownViewer } from '@donkeywork/editor'
import DocViewer, { DocViewerRenderers } from '@iamjariwala/react-doc-viewer'
import { files, type FileItem } from '@donkeywork/api-client'
import { JsonViewer } from '../ui/json-viewer'

interface FilePreviewPanelProps {
  file: FileItem | null
  currentPrefix: string
  open: boolean
  onClose: () => void
}

type PreviewType = 'markdown' | 'json' | 'text' | 'document' | 'unsupported'

const TEXT_EXTENSIONS = ['txt', 'log', 'xml', 'yaml', 'yml']
const JSON_EXTENSIONS = ['json']
const MARKDOWN_EXTENSIONS = ['md']
const DOCUMENT_EXTENSIONS = [
  'pdf', 'docx', 'xlsx', 'pptx', 'csv',
  'png', 'jpg', 'jpeg', 'gif', 'webp',
]

function getExtension(fileName: string): string {
  return fileName.split('.').pop()?.toLowerCase() || ''
}

function getPreviewType(fileName: string): PreviewType {
  const ext = getExtension(fileName)
  if (MARKDOWN_EXTENSIONS.includes(ext)) return 'markdown'
  if (JSON_EXTENSIONS.includes(ext)) return 'json'
  if (TEXT_EXTENSIONS.includes(ext)) return 'text'
  if (DOCUMENT_EXTENSIONS.includes(ext)) return 'document'
  return 'unsupported'
}

function getTypeLabel(fileName: string): string {
  const ext = getExtension(fileName)
  const labels: Record<string, string> = {
    md: 'Markdown', txt: 'Text', json: 'JSON', log: 'Log',
    xml: 'XML', yaml: 'YAML', yml: 'YAML', pdf: 'PDF',
    docx: 'Word', xlsx: 'Excel', pptx: 'PowerPoint', csv: 'CSV',
    png: 'PNG', jpg: 'JPEG', jpeg: 'JPEG', gif: 'GIF', webp: 'WebP',
  }
  return labels[ext] || ext.toUpperCase() || 'File'
}

export function FilePreviewPanel({ file, currentPrefix, open, onClose }: FilePreviewPanelProps) {
  const [textContent, setTextContent] = useState<string | null>(null)
  const [docUrl, setDocUrl] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!file || !open) {
      setTextContent(null)
      setDocUrl(null)
      setError(null)
      return
    }

    const previewType = getPreviewType(file.fileName)
    if (previewType === 'unsupported') return

    const fileKey = currentPrefix ? `${currentPrefix}${file.fileName}` : file.fileName

    const load = async () => {
      setLoading(true)
      setError(null)
      setTextContent(null)
      setDocUrl(null)

      try {
        if (previewType === 'markdown' || previewType === 'json' || previewType === 'text') {
          const content = await files.fetchText(fileKey)
          setTextContent(content)
        } else {
          const { blob } = await files.download(fileKey)
          const blobUrl = URL.createObjectURL(blob)
          setDocUrl(blobUrl)
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load preview')
      } finally {
        setLoading(false)
      }
    }

    load()
  }, [file, open, currentPrefix])

  const handleDownload = async () => {
    if (!file) return
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
  }

  const previewType = file ? getPreviewType(file.fileName) : 'unsupported'

  return (
    <Sheet open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose() }}>
      <SheetContent side="right" className="sm:max-w-2xl flex flex-col overflow-hidden">
        <SheetHeader className="shrink-0">
          <div className="flex items-center gap-2 pr-8">
            <SheetTitle className="truncate">{file?.fileName ?? ''}</SheetTitle>
            {file && <Badge variant="secondary">{getTypeLabel(file.fileName)}</Badge>}
          </div>
          <SheetDescription className="flex items-center gap-2">
            <Eye className="h-3.5 w-3.5" />
            File preview
            <Button variant="ghost" size="sm" className="ml-auto" onClick={handleDownload}>
              <Download className="h-4 w-4 mr-1" />
              Download
            </Button>
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-auto mt-4">
          {loading && (
            <div className="flex items-center justify-center h-48">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          )}

          {error && (
            <div className="flex flex-col items-center justify-center h-48 text-center">
              <p className="text-sm text-destructive">{error}</p>
              <Button variant="outline" size="sm" className="mt-3" onClick={handleDownload}>
                <Download className="h-4 w-4 mr-1" />
                Download instead
              </Button>
            </div>
          )}

          {!loading && !error && previewType === 'markdown' && textContent !== null && (
            <div className="p-1">
              <MarkdownViewer content={textContent} />
            </div>
          )}

          {!loading && !error && previewType === 'json' && textContent !== null && (
            <JsonViewer data={textContent} collapsed={3} />
          )}

          {!loading && !error && previewType === 'text' && textContent !== null && (
            <pre className="text-sm bg-muted/50 rounded-lg p-4 overflow-auto whitespace-pre-wrap break-words font-mono border">
              {textContent}
            </pre>
          )}

          {!loading && !error && previewType === 'document' && docUrl && file && (
            <DocViewer
              documents={[{ uri: docUrl, fileType: getExtension(file.fileName) }]}
              pluginRenderers={DocViewerRenderers}
              config={{
                header: { disableHeader: true },
              }}
              style={{ height: '100%', minHeight: 400 }}
            />
          )}

          {!loading && !error && previewType === 'unsupported' && (
            <div className="flex flex-col items-center justify-center h-48 text-center">
              <div className="rounded-full bg-muted p-4 mb-4">
                <FileText className="h-8 w-8 text-muted-foreground" />
              </div>
              <p className="text-sm text-muted-foreground mb-3">
                Preview not available for this file type
              </p>
              <Button variant="outline" size="sm" onClick={handleDownload}>
                <Download className="h-4 w-4 mr-1" />
                Download file
              </Button>
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
