import { useState, useRef, useCallback, useEffect } from 'react'
import { ImagePlus, X, Loader2, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { conversations, type UploadImageResponse } from '@/lib/api'
import { cn } from '@/lib/utils'

interface ImageUploaderProps {
  conversationId: string | null
  onImageUploaded: (response: UploadImageResponse) => void
  onError: (error: string) => void
  disabled?: boolean
}

interface PendingImage {
  file: File
  preview: string
}

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']
const MAX_SIZE_MB = 10
const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024

export function ImageUploader({
  conversationId,
  onImageUploaded,
  onError,
  disabled = false
}: ImageUploaderProps) {
  const [pendingImage, setPendingImage] = useState<PendingImage | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [isDragging, setIsDragging] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // Clean up object URL when component unmounts or image changes
  useEffect(() => {
    return () => {
      if (pendingImage?.preview) {
        URL.revokeObjectURL(pendingImage.preview)
      }
    }
  }, [pendingImage])

  const validateFile = useCallback((file: File): string | null => {
    if (!ALLOWED_TYPES.includes(file.type)) {
      return `Invalid file type. Allowed: ${ALLOWED_TYPES.map(t => t.split('/')[1].toUpperCase()).join(', ')}`
    }
    if (file.size > MAX_SIZE_BYTES) {
      return `File too large. Maximum size: ${MAX_SIZE_MB}MB`
    }
    return null
  }, [])

  const handleFileSelect = useCallback((file: File) => {
    const error = validateFile(file)
    if (error) {
      onError(error)
      return
    }

    // Clean up previous preview
    if (pendingImage?.preview) {
      URL.revokeObjectURL(pendingImage.preview)
    }

    setPendingImage({
      file,
      preview: URL.createObjectURL(file)
    })
  }, [validateFile, onError, pendingImage])

  const handleUpload = useCallback(async () => {
    if (!pendingImage || !conversationId) return

    setIsUploading(true)
    try {
      const response = await conversations.uploadImage(conversationId, pendingImage.file)
      onImageUploaded(response)
      setPendingImage(null)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setIsUploading(false)
    }
  }, [pendingImage, conversationId, onImageUploaded, onError])

  const handleRemove = useCallback(() => {
    if (pendingImage?.preview) {
      URL.revokeObjectURL(pendingImage.preview)
    }
    setPendingImage(null)
  }, [pendingImage])

  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFileSelect(file)
    }
    // Reset input so same file can be selected again
    e.target.value = ''
  }, [handleFileSelect])

  const handleClick = useCallback(() => {
    fileInputRef.current?.click()
  }, [])

  // Drag and drop handlers
  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)
  }, [])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)

    const file = e.dataTransfer.files?.[0]
    if (file && file.type.startsWith('image/')) {
      handleFileSelect(file)
    }
  }, [handleFileSelect])

  // Paste handler for clipboard images
  useEffect(() => {
    const handlePaste = (e: ClipboardEvent) => {
      if (disabled) return

      const items = e.clipboardData?.items
      if (!items) return

      for (const item of items) {
        if (item.type.startsWith('image/')) {
          const file = item.getAsFile()
          if (file) {
            handleFileSelect(file)
            break
          }
        }
      }
    }

    document.addEventListener('paste', handlePaste)
    return () => document.removeEventListener('paste', handlePaste)
  }, [disabled, handleFileSelect])

  // Auto-upload when image is selected and conversation exists
  useEffect(() => {
    if (pendingImage && conversationId && !isUploading) {
      handleUpload()
    }
  }, [pendingImage, conversationId, isUploading, handleUpload])

  return (
    <div
      className={cn(
        "relative",
        isDragging && "ring-2 ring-primary ring-offset-2 rounded-lg"
      )}
      onDragEnter={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
    >
      <input
        ref={fileInputRef}
        type="file"
        accept={ALLOWED_TYPES.join(',')}
        onChange={handleInputChange}
        className="hidden"
        disabled={disabled}
      />

      {/* Pending image preview */}
      {pendingImage && (
        <div className="mb-2 relative inline-block">
          <img
            src={pendingImage.preview}
            alt="Upload preview"
            className="max-w-[100px] max-h-[100px] rounded-lg border border-border object-cover"
          />
          {isUploading ? (
            <div className="absolute inset-0 flex items-center justify-center bg-black/50 rounded-lg">
              <Loader2 className="h-5 w-5 animate-spin text-white" />
            </div>
          ) : (
            <button
              onClick={handleRemove}
              className="absolute -top-2 -right-2 p-1 rounded-full bg-destructive text-destructive-foreground hover:bg-destructive/90"
              aria-label="Remove image"
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </div>
      )}

      {/* Upload button */}
      <Button
        type="button"
        variant="ghost"
        size="icon"
        onClick={handleClick}
        disabled={disabled || isUploading || !!pendingImage}
        className="h-[44px] w-[44px] shrink-0"
        title="Attach image"
      >
        <ImagePlus className="h-5 w-5" />
      </Button>

      {/* No conversation warning */}
      {pendingImage && !conversationId && (
        <div className="absolute bottom-full left-0 mb-2 flex items-center gap-1 text-xs text-amber-500">
          <AlertCircle className="h-3 w-3" />
          <span>Send a message first to start a conversation</span>
        </div>
      )}
    </div>
  )
}
