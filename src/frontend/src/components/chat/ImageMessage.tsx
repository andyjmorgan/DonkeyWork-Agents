import { useState, useCallback, useEffect } from 'react'
import { Loader2, ImageOff, ZoomIn, X } from 'lucide-react'
import { fetchImageAsBlob } from '@/lib/api'
import { cn } from '@/lib/utils'

interface ImageMessageProps {
  fileId: string
  alt?: string
  className?: string
  /** Render as small thumbnail that expands on click */
  thumbnail?: boolean
  /** Size variant for thumbnails: 'single' (80x60) or 'multi' (64x52) */
  size?: 'single' | 'multi'
}

export function ImageMessage({ fileId, alt = 'Message image', className, thumbnail = false, size = 'single' }: ImageMessageProps) {
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

  // Thumbnail dimensions based on size prop
  const thumbnailDimensions = size === 'single'
    ? { width: 80, height: 60 }
    : { width: 64, height: 52 }

  // Check if className contains size constraints (for legacy usage)
  const hasSizeConstraint = className?.includes('max-w-') || className?.includes('max-h-')

  // Thumbnail mode: tiny images that blow up on click
  if (thumbnail) {
    return (
      <>
        <div
          onClick={openLightbox}
          className={cn(
            "rounded-[10px] overflow-hidden cursor-zoom-in transition-all duration-150",
            "shadow-[0_1px_6px_rgba(0,0,0,0.22)] hover:shadow-[0_3px_14px_rgba(0,0,0,0.35)]",
            "hover:scale-[1.08]",
            className
          )}
          style={{ width: thumbnailDimensions.width, height: thumbnailDimensions.height }}
        >
          {isLoading && (
            <div className="w-full h-full flex items-center justify-center bg-muted animate-pulse">
              <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
            </div>
          )}
          {blobUrl && (
            <img
              src={blobUrl}
              alt={alt}
              onLoad={handleLoad}
              onError={handleError}
              className={cn(
                "w-full h-full object-cover",
                isLoading ? "opacity-0" : "opacity-100"
              )}
            />
          )}
        </div>

        {/* Lightbox with blow-up animation */}
        {isLightboxOpen && (
          <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 backdrop-blur-sm cursor-zoom-out animate-in fade-in duration-150"
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
                className="max-w-[88vw] max-h-[88vh] object-contain rounded-[14px] shadow-[0_30px_100px_rgba(0,0,0,0.6)] animate-in zoom-in-75 duration-300"
                onClick={(e) => e.stopPropagation()}
              />
            )}
          </div>
        )}
      </>
    )
  }

  // Default mode: larger images in message view
  return (
    <>
      <div className={cn("relative inline-block overflow-hidden", className)}>
        {isLoading && (
          <div className={cn(
            "flex items-center justify-center rounded-lg border border-border bg-muted animate-pulse",
            hasSizeConstraint ? "w-full h-full min-w-[60px] min-h-[60px]" : "w-48 h-32"
          )}>
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
              "rounded-lg object-contain cursor-zoom-in transition-opacity",
              hasSizeConstraint ? "w-full h-full" : "max-w-[300px] max-h-[400px]",
              isLoading ? "opacity-0 absolute" : "opacity-100"
            )}
          />
        )}
        {!isLoading && !hasSizeConstraint && (
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
