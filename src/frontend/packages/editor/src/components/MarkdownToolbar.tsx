import { Editor } from '@tiptap/react'
import {
  Bold,
  Italic,
  Underline,
  Strikethrough,
  Code,
  Heading1,
  Heading2,
  Heading3,
  List,
  ListOrdered,
  ListTodo,
  Quote,
  Minus,
  Link,
  Unlink,
  Undo,
  Redo,
  Image,
  FileCode,
  Eye,
  Columns,
  GitBranch,
  Table,
} from 'lucide-react'
import {
  Button,
  Separator,
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Input,
  Label,
  Textarea,
} from '@donkeywork/ui'
import { useCallback, useState } from 'react'
import type { ViewMode } from './MarkdownEditor'
import { TableDialog } from './TableDialog'
import { MermaidDiagram } from './MermaidDiagram'

interface MarkdownToolbarProps {
  editor: Editor
  viewMode: ViewMode
  onViewModeChange: (mode: ViewMode) => void
  onInsertMarkdown?: (markdown: string) => void
}

interface ToolbarButtonProps {
  onClick: () => void
  isActive?: boolean
  disabled?: boolean
  tooltip: string
  children: React.ReactNode
}

function ToolbarButton({ onClick, isActive, disabled, tooltip, children }: ToolbarButtonProps) {
  return (
    <TooltipProvider delayDuration={300}>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            onClick={onClick}
            disabled={disabled}
            className={`h-8 w-8 p-0 ${isActive ? 'bg-accent text-accent-foreground' : ''}`}
          >
            {children}
          </Button>
        </TooltipTrigger>
        <TooltipContent>
          <p>{tooltip}</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

export function MarkdownToolbar({ editor, viewMode, onViewModeChange, onInsertMarkdown }: MarkdownToolbarProps) {
  const [showImageDialog, setShowImageDialog] = useState(false)
  const [showLinkDialog, setShowLinkDialog] = useState(false)
  const [showMermaidDialog, setShowMermaidDialog] = useState(false)
  const [showTableDialog, setShowTableDialog] = useState(false)
  const [imageUrl, setImageUrl] = useState('')
  const [imageAlt, setImageAlt] = useState('')
  const [linkUrl, setLinkUrl] = useState('')
  const [mermaidCode, setMermaidCode] = useState('')

  const isCodeMode = viewMode === 'code'

  const openLinkDialog = useCallback(() => {
    const previousUrl = editor.getAttributes('link').href || ''
    setLinkUrl(previousUrl)
    setShowLinkDialog(true)
  }, [editor])

  const insertLink = useCallback(() => {
    if (!linkUrl) {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
    } else {
      editor.chain().focus().extendMarkRange('link').setLink({ href: linkUrl }).run()
    }
    setShowLinkDialog(false)
    setLinkUrl('')
  }, [editor, linkUrl])

  const insertImage = useCallback(() => {
    if (imageUrl) {
      editor.chain().focus().setImage({ src: imageUrl, alt: imageAlt }).run()
    }
    setShowImageDialog(false)
    setImageUrl('')
    setImageAlt('')
  }, [editor, imageUrl, imageAlt])

  const insertMermaid = useCallback(() => {
    if (mermaidCode) {
      const mermaidMarkdown = `\n\`\`\`mermaid\n${mermaidCode}\n\`\`\`\n`
      if (isCodeMode && onInsertMarkdown) {
        onInsertMarkdown(mermaidMarkdown)
      } else {
        editor.chain().focus().insertContent(mermaidMarkdown).run()
      }
    }
    setShowMermaidDialog(false)
    setMermaidCode('')
  }, [editor, mermaidCode, isCodeMode, onInsertMarkdown])

  const handleInsertTable = useCallback((rows: number, cols: number, markdown: string) => {
    if (isCodeMode && onInsertMarkdown) {
      onInsertMarkdown('\n' + markdown + '\n')
    } else {
      editor.chain().focus().insertTable({ rows, cols, withHeaderRow: true }).run()
    }
  }, [editor, isCodeMode, onInsertMarkdown])

  return (
    <>
      <div className="flex flex-wrap items-center gap-0.5 border-b border-border p-1 bg-muted/50">
        {/* Undo/Redo */}
        <ToolbarButton
          onClick={() => editor.chain().focus().undo().run()}
          disabled={!editor.can().undo()}
          tooltip="Undo"
        >
          <Undo className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().redo().run()}
          disabled={!editor.can().redo()}
          tooltip="Redo"
        >
          <Redo className="h-4 w-4" />
        </ToolbarButton>

        <Separator orientation="vertical" className="mx-1 h-6" />

        {/* Headings */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
          isActive={editor.isActive('heading', { level: 1 })}
          tooltip="Heading 1"
        >
          <Heading1 className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          isActive={editor.isActive('heading', { level: 2 })}
          tooltip="Heading 2"
        >
          <Heading2 className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
          isActive={editor.isActive('heading', { level: 3 })}
          tooltip="Heading 3"
        >
          <Heading3 className="h-4 w-4" />
        </ToolbarButton>

        <Separator orientation="vertical" className="mx-1 h-6" />

        {/* Text formatting */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBold().run()}
          isActive={editor.isActive('bold')}
          tooltip="Bold"
        >
          <Bold className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleItalic().run()}
          isActive={editor.isActive('italic')}
          tooltip="Italic"
        >
          <Italic className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleUnderline().run()}
          isActive={editor.isActive('underline')}
          tooltip="Underline"
        >
          <Underline className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleStrike().run()}
          isActive={editor.isActive('strike')}
          tooltip="Strikethrough"
        >
          <Strikethrough className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleCode().run()}
          isActive={editor.isActive('code')}
          tooltip="Inline Code"
        >
          <Code className="h-4 w-4" />
        </ToolbarButton>

        <Separator orientation="vertical" className="mx-1 h-6" />

        {/* Lists */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          isActive={editor.isActive('bulletList')}
          tooltip="Bullet List"
        >
          <List className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          isActive={editor.isActive('orderedList')}
          tooltip="Numbered List"
        >
          <ListOrdered className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleTaskList().run()}
          isActive={editor.isActive('taskList')}
          tooltip="Task List"
        >
          <ListTodo className="h-4 w-4" />
        </ToolbarButton>

        <Separator orientation="vertical" className="mx-1 h-6" />

        {/* Blocks */}
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
          isActive={editor.isActive('blockquote')}
          tooltip="Quote"
        >
          <Quote className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().toggleCodeBlock().run()}
          isActive={editor.isActive('codeBlock')}
          tooltip="Code Block"
        >
          <FileCode className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().setHorizontalRule().run()}
          tooltip="Horizontal Rule"
        >
          <Minus className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => setShowTableDialog(true)}
          isActive={editor.isActive('table')}
          tooltip="Insert Table"
        >
          <Table className="h-4 w-4" />
        </ToolbarButton>

        <Separator orientation="vertical" className="mx-1 h-6" />

        {/* Links & Media */}
        <ToolbarButton
          onClick={openLinkDialog}
          isActive={editor.isActive('link')}
          tooltip="Add Link"
        >
          <Link className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor.chain().focus().unsetLink().run()}
          disabled={!editor.isActive('link')}
          tooltip="Remove Link"
        >
          <Unlink className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => setShowImageDialog(true)}
          tooltip="Add Image"
        >
          <Image className="h-4 w-4" />
        </ToolbarButton>
        <ToolbarButton
          onClick={() => setShowMermaidDialog(true)}
          tooltip="Add Mermaid Diagram"
        >
          <GitBranch className="h-4 w-4" />
        </ToolbarButton>

        {/* Spacer */}
        <div className="flex-1" />

        {/* View mode toggle */}
        <div className="flex items-center border border-border rounded-md overflow-hidden">
          <ToolbarButton
            onClick={() => onViewModeChange('preview')}
            isActive={viewMode === 'preview'}
            tooltip="Preview"
          >
            <Eye className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => onViewModeChange('split')}
            isActive={viewMode === 'split'}
            tooltip="Split View"
          >
            <Columns className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => onViewModeChange('code')}
            isActive={viewMode === 'code'}
            tooltip="Markdown"
          >
            <Code className="h-4 w-4" />
          </ToolbarButton>
        </div>
      </div>

      {/* Link Dialog */}
      <Dialog open={showLinkDialog} onOpenChange={setShowLinkDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Insert Link</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="linkUrl">URL</Label>
              <Input
                id="linkUrl"
                value={linkUrl}
                onChange={(e) => setLinkUrl(e.target.value)}
                placeholder="https://example.com"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowLinkDialog(false)}>
              Cancel
            </Button>
            <Button onClick={insertLink}>Insert</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Image Dialog */}
      <Dialog open={showImageDialog} onOpenChange={setShowImageDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Insert Image</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="imageUrl">Image URL</Label>
              <Input
                id="imageUrl"
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="https://example.com/image.png"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="imageAlt">Alt Text (optional)</Label>
              <Input
                id="imageAlt"
                value={imageAlt}
                onChange={(e) => setImageAlt(e.target.value)}
                placeholder="Image description"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowImageDialog(false)}>
              Cancel
            </Button>
            <Button onClick={insertImage} disabled={!imageUrl}>Insert</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Mermaid Dialog with side-by-side editor and preview */}
      <Dialog open={showMermaidDialog} onOpenChange={setShowMermaidDialog}>
        <DialogContent className="max-w-5xl max-h-[80vh]">
          <DialogHeader>
            <DialogTitle>Insert Mermaid Diagram</DialogTitle>
          </DialogHeader>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 py-4 min-h-[400px]">
            {/* Left panel - Code editor */}
            <div className="flex flex-col space-y-2">
              <Label htmlFor="mermaidCode">Mermaid Code</Label>
              <Textarea
                id="mermaidCode"
                value={mermaidCode}
                onChange={(e) => setMermaidCode(e.target.value)}
                placeholder={`graph TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]`}
                className="flex-1 min-h-[300px] font-mono text-sm resize-none"
              />
              <p className="text-xs text-muted-foreground">
                Learn more at{' '}
                <a href="https://mermaid.js.org/syntax/flowchart.html" target="_blank" rel="noopener noreferrer" className="text-primary underline">
                  mermaid.js.org
                </a>
              </p>
            </div>
            {/* Right panel - Live preview */}
            <div className="flex flex-col space-y-2">
              <Label>Preview</Label>
              <div className="flex-1 border border-border rounded-md overflow-auto bg-muted/30 min-h-[300px]">
                <MermaidDiagram code={mermaidCode} className="h-full" />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowMermaidDialog(false)}>
              Cancel
            </Button>
            <Button onClick={insertMermaid} disabled={!mermaidCode}>Insert</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Table Dialog */}
      <TableDialog
        open={showTableDialog}
        onOpenChange={setShowTableDialog}
        onInsertTable={handleInsertTable}
      />
    </>
  )
}
