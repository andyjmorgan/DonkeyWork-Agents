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
import Editor from '@monaco-editor/react'
import DocViewer, { DocViewerRenderers } from '@iamjariwala/react-doc-viewer'
import { files, type FileItem } from '@donkeywork/api-client'
import { JsonViewer } from '../ui/json-viewer'
import * as XLSX from 'xlsx'

interface FilePreviewPanelProps {
  file: FileItem | null
  currentPrefix: string
  open: boolean
  onClose: () => void
}

type PreviewType = 'markdown' | 'json' | 'text' | 'code' | 'spreadsheet' | 'document' | 'unsupported'

interface SpreadsheetData {
  sheets: string[]
  data: Record<string, unknown[][]>
}

const TEXT_EXTENSIONS = ['txt', 'log', 'xml', 'yaml', 'yml']
const JSON_EXTENSIONS = ['json']
const MARKDOWN_EXTENSIONS = ['md']
const CODE_EXTENSIONS = [
  'js', 'jsx', 'ts', 'tsx', 'py', 'css', 'html', 'sh', 'bash',
  'cs', 'go', 'rs', 'rb', 'java', 'sql', 'toml',
]
const SPREADSHEET_EXTENSIONS = ['xlsx', 'csv']
const DOCUMENT_EXTENSIONS = [
  'pdf', 'docx',
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
  if (CODE_EXTENSIONS.includes(ext)) return 'code'
  if (SPREADSHEET_EXTENSIONS.includes(ext)) return 'spreadsheet'
  if (DOCUMENT_EXTENSIONS.includes(ext)) return 'document'
  return 'unsupported'
}

function getMonacoLanguage(fileName: string): string {
  const ext = getExtension(fileName)
  switch (ext) {
    case 'js': case 'jsx': return 'javascript'
    case 'ts': case 'tsx': return 'typescript'
    case 'py': return 'python'
    case 'css': return 'css'
    case 'html': return 'html'
    case 'sh': case 'bash': return 'shell'
    case 'sql': return 'sql'
    case 'cs': return 'csharp'
    case 'go': return 'go'
    case 'rs': return 'rust'
    case 'rb': return 'ruby'
    case 'java': return 'java'
    case 'toml': return 'ini'
    default: return 'plaintext'
  }
}

function getTypeLabel(fileName: string): string {
  const ext = getExtension(fileName)
  const labels: Record<string, string> = {
    md: 'Markdown', txt: 'Text', json: 'JSON', log: 'Log',
    xml: 'XML', yaml: 'YAML', yml: 'YAML', pdf: 'PDF',
    docx: 'Word', xlsx: 'Excel', pptx: 'PowerPoint', csv: 'CSV',
    png: 'PNG', jpg: 'JPEG', jpeg: 'JPEG', gif: 'GIF', webp: 'WebP',
    js: 'JavaScript', jsx: 'JavaScript', ts: 'TypeScript', tsx: 'TypeScript',
    py: 'Python', css: 'CSS', html: 'HTML', sh: 'Shell', bash: 'Shell',
    cs: 'C#', go: 'Go', rs: 'Rust', rb: 'Ruby', java: 'Java',
    sql: 'SQL', toml: 'TOML',
  }
  return labels[ext] || ext.toUpperCase() || 'File'
}

export function FilePreviewPanel({ file, currentPrefix, open, onClose }: FilePreviewPanelProps) {
  const [textContent, setTextContent] = useState<string | null>(null)
  const [docUrl, setDocUrl] = useState<string | null>(null)
  const [spreadsheet, setSpreadsheet] = useState<SpreadsheetData | null>(null)
  const [activeSheet, setActiveSheet] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!file || !open) {
      setTextContent(null)
      setDocUrl(null)
      setSpreadsheet(null)
      setActiveSheet('')
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
      setSpreadsheet(null)
      setActiveSheet('')

      try {
        if (previewType === 'markdown' || previewType === 'json' || previewType === 'text' || previewType === 'code') {
          const content = await files.fetchText(fileKey)
          setTextContent(content)
        } else if (previewType === 'spreadsheet') {
          const { blob } = await files.download(fileKey)
          const arrayBuffer = await blob.arrayBuffer()
          const workbook = XLSX.read(arrayBuffer, { type: 'array' })
          const sheets: Record<string, unknown[][]> = {}
          for (const name of workbook.SheetNames) {
            sheets[name] = XLSX.utils.sheet_to_json(workbook.Sheets[name], { header: 1 }) as unknown[][]
          }
          setSpreadsheet({ sheets: workbook.SheetNames, data: sheets })
          setActiveSheet(workbook.SheetNames[0])
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
  const sheetRows = spreadsheet && activeSheet ? spreadsheet.data[activeSheet] || [] : []

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

          {!loading && !error && previewType === 'code' && textContent !== null && file && (
            <Editor
              height="calc(100vh - 200px)"
              language={getMonacoLanguage(file.fileName)}
              value={textContent}
              theme="vs-dark"
              options={{
                readOnly: true,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 13,
                wordWrap: 'on',
                lineNumbers: 'on',
                padding: { top: 12 },
              }}
            />
          )}

          {!loading && !error && previewType === 'spreadsheet' && spreadsheet && (
            <div className="space-y-2">
              {spreadsheet.sheets.length > 1 && (
                <div className="flex gap-1 border-b pb-2 overflow-x-auto">
                  {spreadsheet.sheets.map((name) => (
                    <button
                      key={name}
                      onClick={() => setActiveSheet(name)}
                      className={`px-3 py-1 text-xs rounded-md transition-colors whitespace-nowrap ${
                        activeSheet === name
                          ? 'bg-primary text-primary-foreground'
                          : 'bg-muted text-muted-foreground hover:bg-muted/80'
                      }`}
                    >
                      {name}
                    </button>
                  ))}
                </div>
              )}
              <div className="overflow-auto border rounded-md max-h-[calc(100vh-200px)]">
                <table className="w-full text-xs border-collapse">
                  <tbody>
                    {sheetRows.map((row, i) => (
                      <tr key={i} className={i === 0 ? 'bg-muted font-medium sticky top-0' : 'border-t border-border'}>
                        {(row as unknown[]).map((cell, j) => (
                          <td key={j} className="px-3 py-1.5 whitespace-nowrap border-r border-border last:border-r-0">
                            {cell != null ? String(cell) : ''}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
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
