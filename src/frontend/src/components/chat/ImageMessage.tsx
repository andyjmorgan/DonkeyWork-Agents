import { useState, useCallback, useEffect } from 'react'
import { Loader2, ImageOff, ZoomIn, X } from 'lucide-react'
import { fetchImageAsBlob } from '@/lib/api'
import { cn } from '@/lib/utils'

interface ImageMessageProps {
  fileId: string
  alt?: string
  className?: string
}

export function ImageMessage({ fileId, alt = 'Message image', className }: ImageMessageProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [hasError, setHasError] = useState(false)
  const [isLightboxOpen, setIsLightboxOpen] = useState(false)
  const [blobUrl, setBlobUrl] = useState<string | null>(null)

  // Fetch image with auth header and convert to blob URL
  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setHasError(false)

    fetchImageAsBlob(fileId)
      .then((url) => {
        if (isMounted) {
          setBlobUrl(url)
          setIsLoading(false)
        }
      })
      .catch(() => {
        if (isMounted) {
          setHasError(true)
          setIsLoading(false)
        }
      })

    return () => {
      isMounted = false
      // Revoke blob URL on cleanup
      if (blobUrl) {
        URL.revokeObjectURL(blobUrl)
      }
    }
  }, [fileId])

  const handleLoad = useCallback(() => {
    setIsLoading(false)
    setHasError(false)
  }, [])

  const handleError = useCallback(() => {
    setIsLoading(false)
    setHasError(true)
  }, [])

  const openLightbox = useCallback(() => {
    if (!hasError) {
      setIsLightboxOpen(true)
    }
  }, [hasError])

  const closeLightbox = useCallback(() => {
    setIsLightboxOpen(false)
  }, [])

  if (hasError) {
    return (
      <div className={cn(
        "flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive",
        className
      )}>
        <ImageOff className="h-4 w-4" />
        <span>Failed to load image</span>
      </div>
    )
  }

  return (
    <>
      <div className={cn("relative inline-block", className)}>
        {isLoading && (
          <div className="flex items-center justify-center w-48 h-32 rounded-lg border border-border bg-muted animate-pulse">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        )}
        {blobUrl && (
          <img
            src={blobUrl}
            alt={alt}
            onLoad={handleLoad}
            onError={handleError}
            onClick={openLightbox}
            className={cn(
              "max-w-full max-h-[400px] rounded-lg object-contain cursor-zoom-in transition-opacity",
              "w-auto",
              isLoading ? "opacity-0 absolute" : "opacity-100"
            )}
            style={{ maxWidth: 'min(300px, 100%)' }}
          />
        )}
        {!isLoading && (
          <button
            onClick={openLightbox}
            className="absolute bottom-2 right-2 p-1.5 rounded-full bg-black/50 text-white opacity-0 hover:opacity-100 transition-opacity"
            aria-label="View full size"
          >
            <ZoomIn className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Lightbox */}
      {isLightboxOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/90"
          onClick={closeLightbox}
        >
          <button
            onClick={closeLightbox}
            className="absolute top-4 right-4 p-2 rounded-full bg-white/10 text-white hover:bg-white/20 transition-colors"
            aria-label="Close"
          >
            <X className="h-6 w-6" />
          </button>
          {blobUrl && (
            <img
              src={blobUrl}
              alt={alt}
              className="max-w-[90vw] max-h-[90vh] object-contain"
              onClick={(e) => e.stopPropagation()}
            />
          )}
        </div>
      )}
    </>
  )
}
