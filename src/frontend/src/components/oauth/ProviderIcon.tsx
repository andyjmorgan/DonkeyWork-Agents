import { type OAuthProvider } from '@/lib/api'
import { GoogleIcon } from '@/components/icons/GoogleIcon'
import { Github } from 'lucide-react'

interface ProviderIconProps {
  provider: OAuthProvider
  className?: string
}

export function ProviderIcon({ provider, className = 'h-5 w-5' }: ProviderIconProps) {
  switch (provider) {
    case 'Google':
      return <GoogleIcon className={className} />
    case 'Microsoft':
      return (
        <svg className={className} viewBox="0 0 23 23" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect width="11" height="11" fill="#F25022"/>
          <rect y="12" width="11" height="11" fill="#00A4EF"/>
          <rect x="12" width="11" height="11" fill="#7FBA00"/>
          <rect x="12" y="12" width="11" height="11" fill="#FFB900"/>
        </svg>
      )
    case 'GitHub':
      return <Github className={className} />
    default:
      return null
  }
}
