import { cn } from '@/lib/utils'

interface LogoProps {
  className?: string
  size?: 'sm' | 'md' | 'lg'
  showText?: boolean
}

export function Logo({ className, size = 'md', showText = true }: LogoProps) {
  const sizes = {
    sm: 'h-8 w-8',
    md: 'h-10 w-10',
    lg: 'h-24 w-24',
  }

  const textSizes = {
    sm: 'text-sm',
    md: 'text-lg',
    lg: 'text-2xl',
  }

  return (
    <div className={cn('flex items-center gap-2', className)}>
      <img
        src="/donkeywork.png"
        alt="DonkeyWork"
        className={cn(sizes[size], 'shrink-0')}
      />
      {showText && (
        <span className={cn('font-bold', textSizes[size])}>DonkeyWork</span>
      )}
    </div>
  )
}
