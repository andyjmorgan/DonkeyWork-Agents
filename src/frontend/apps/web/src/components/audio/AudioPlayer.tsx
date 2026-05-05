import { useState, useEffect, useRef, useCallback } from 'react'
import { Play, Pause, RotateCcw, Volume2, ChevronDown, ChevronUp } from 'lucide-react'
import { Button } from '@donkeywork/ui'
import { tts } from '@donkeywork/api-client'
import { cn } from '@/lib/utils'

interface AudioPlayerProps {
  recordingId: string
  name?: string
  transcript?: string
  className?: string
}

const PLAYBACK_REPORT_INTERVAL = 5000 // Report every 5 seconds
const SPEED_OPTIONS = [0.5, 0.75, 1, 1.25, 1.5, 2]

export function AudioPlayer({ recordingId, name, transcript, className }: AudioPlayerProps) {
  const audioRef = useRef<HTMLAudioElement>(null)
  const reportTimerRef = useRef<ReturnType<typeof setInterval>>(undefined)

  const audioUrl = tts.getAudioStreamUrl(recordingId)
  const [error, setError] = useState<string>()

  const [isPlaying, setIsPlaying] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)
  const [playbackSpeed, setPlaybackSpeed] = useState(1)
  const [completed, setCompleted] = useState(false)
  const [showTranscript, setShowTranscript] = useState(true)

  // Restore playback state in the background — playback can begin before this resolves.
  useEffect(() => {
    let cancelled = false

    tts.getPlayback(recordingId).then((playbackData) => {
      if (cancelled || !playbackData) return
      setCurrentTime(playbackData.positionSeconds)
      setPlaybackSpeed(playbackData.playbackSpeed)
      setCompleted(playbackData.completed)
      if (playbackData.durationSeconds > 0) {
        setDuration(playbackData.durationSeconds)
      }
    }).catch(() => {})

    return () => { cancelled = true }
  }, [recordingId])

  // Apply restored playback position when audio is ready
  useEffect(() => {
    const audio = audioRef.current
    if (!audio || !audioUrl) return

    const handleLoadedMetadata = () => {
      setDuration(audio.duration)
      audio.playbackRate = playbackSpeed
      // Restore position if we had one
      if (currentTime > 0 && currentTime < audio.duration) {
        audio.currentTime = currentTime
      }
    }

    audio.addEventListener('loadedmetadata', handleLoadedMetadata)
    return () => audio.removeEventListener('loadedmetadata', handleLoadedMetadata)
    // Only run on initial load
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [audioUrl])

  // Report playback state periodically
  const reportPlayback = useCallback(async () => {
    const audio = audioRef.current
    if (!audio) return

    try {
      await tts.updatePlayback(recordingId, {
        positionSeconds: audio.currentTime,
        durationSeconds: audio.duration || 0,
        completed,
        playbackSpeed: audio.playbackRate,
      })
    } catch {
      // Silently fail - playback reporting is best-effort
    }
  }, [recordingId, completed])

  // Set up periodic reporting when playing
  useEffect(() => {
    if (isPlaying) {
      reportTimerRef.current = setInterval(reportPlayback, PLAYBACK_REPORT_INTERVAL)
    } else {
      if (reportTimerRef.current) {
        clearInterval(reportTimerRef.current)
      }
      // Report immediately when pausing
      reportPlayback()
    }

    return () => {
      if (reportTimerRef.current) {
        clearInterval(reportTimerRef.current)
      }
    }
  }, [isPlaying, reportPlayback])

  // Audio event handlers
  useEffect(() => {
    const audio = audioRef.current
    if (!audio) return

    const onTimeUpdate = () => setCurrentTime(audio.currentTime)
    const onPlay = () => setIsPlaying(true)
    const onPause = () => setIsPlaying(false)
    const onEnded = () => {
      setIsPlaying(false)
      setCompleted(true)
      // Report completion immediately
      tts.updatePlayback(recordingId, {
        positionSeconds: audio.duration,
        durationSeconds: audio.duration,
        completed: true,
        playbackSpeed: audio.playbackRate,
      }).catch(() => {})
    }
    const onErr = () => setError('Failed to load audio')

    audio.addEventListener('timeupdate', onTimeUpdate)
    audio.addEventListener('play', onPlay)
    audio.addEventListener('pause', onPause)
    audio.addEventListener('ended', onEnded)
    audio.addEventListener('error', onErr)

    return () => {
      audio.removeEventListener('timeupdate', onTimeUpdate)
      audio.removeEventListener('play', onPlay)
      audio.removeEventListener('pause', onPause)
      audio.removeEventListener('ended', onEnded)
      audio.removeEventListener('error', onErr)
    }
  }, [recordingId, audioUrl])

  const togglePlay = () => {
    const audio = audioRef.current
    if (!audio) return
    if (isPlaying) {
      audio.pause()
    } else {
      audio.play()
    }
  }

  const restart = () => {
    const audio = audioRef.current
    if (!audio) return
    audio.currentTime = 0
    setCurrentTime(0)
    audio.play()
  }

  const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
    const audio = audioRef.current
    if (!audio) return
    const time = parseFloat(e.target.value)
    audio.currentTime = time
    setCurrentTime(time)
  }

  const cycleSpeed = () => {
    const currentIndex = SPEED_OPTIONS.indexOf(playbackSpeed)
    const nextIndex = (currentIndex + 1) % SPEED_OPTIONS.length
    const newSpeed = SPEED_OPTIONS[nextIndex]
    setPlaybackSpeed(newSpeed)
    if (audioRef.current) {
      audioRef.current.playbackRate = newSpeed
    }
  }

  const formatTime = (seconds: number) => {
    if (!isFinite(seconds) || seconds < 0) return '0:00'
    const mins = Math.floor(seconds / 60)
    const secs = Math.floor(seconds % 60)
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  const progress = duration > 0 ? (currentTime / duration) * 100 : 0

  if (error) {
    return (
      <div className={cn('flex items-center gap-2 rounded-xl border border-destructive/30 bg-destructive/5 p-3', className)}>
        <Volume2 className="h-4 w-4 text-destructive" />
        <span className="text-sm text-destructive">{error}</span>
      </div>
    )
  }

  return (
    <div className={cn('rounded-xl border border-border bg-secondary/50 overflow-hidden', className)}>
      {audioUrl && <audio ref={audioRef} src={audioUrl} preload="metadata" />}

      {/* Player Controls */}
      <div className="flex items-center gap-3 p-3">
        {/* Play/Pause */}
        <Button
          variant="ghost"
          size="sm"
          onClick={togglePlay}
          className="h-9 w-9 shrink-0 rounded-full bg-gradient-to-br from-pink-500 to-rose-600 text-white shadow-lg shadow-pink-500/25 hover:from-pink-600 hover:to-rose-700 hover:text-white"
        >
          {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4 ml-0.5" />}
        </Button>

        {/* Progress */}
        <div className="flex flex-1 flex-col gap-1">
          {name && (
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium truncate max-w-[200px]">{name}</span>
              {completed && (
                <span className="text-xs text-emerald-500 font-medium">Completed</span>
              )}
            </div>
          )}
          <div className="flex items-center gap-2">
            <input
              type="range"
              min={0}
              max={duration || 100}
              step={0.1}
              value={currentTime}
              onChange={handleSeek}
              className="h-1.5 w-full cursor-pointer appearance-none rounded-full bg-muted [&::-webkit-slider-thumb]:h-3 [&::-webkit-slider-thumb]:w-3 [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-pink-500"
              style={{
                background: `linear-gradient(to right, rgb(236, 72, 153) ${progress}%, hsl(var(--muted)) ${progress}%)`
              }}
            />
          </div>
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>{formatTime(currentTime)}</span>
            <span>{formatTime(duration)}</span>
          </div>
        </div>

        {/* Speed */}
        <Button
          variant="ghost"
          size="sm"
          onClick={cycleSpeed}
          className="h-7 shrink-0 rounded-lg px-2 text-xs font-mono text-muted-foreground hover:text-foreground"
        >
          {playbackSpeed}x
        </Button>

        {/* Restart */}
        <Button
          variant="ghost"
          size="sm"
          onClick={restart}
          className="h-7 w-7 shrink-0 rounded-lg text-muted-foreground hover:text-foreground"
        >
          <RotateCcw className="h-3.5 w-3.5" />
        </Button>

        {/* Transcript toggle */}
        {transcript && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowTranscript(prev => !prev)}
            className="h-7 w-7 shrink-0 rounded-lg text-muted-foreground hover:text-foreground"
          >
            {showTranscript ? <ChevronUp className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
          </Button>
        )}
      </div>

      {/* Transcript */}
      {transcript && showTranscript && (
        <div className="border-t border-border bg-background/50 px-4 py-3">
          <div className="text-xs font-medium text-muted-foreground mb-1.5">Transcript</div>
          <div className="text-sm leading-relaxed text-foreground/90 max-h-48 overflow-y-auto whitespace-pre-wrap">
            {transcript}
          </div>
        </div>
      )}
    </div>
  )
}
