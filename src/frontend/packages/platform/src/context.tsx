import { createContext, useContext } from 'react'
import type { PlatformConfig } from './types'

const PlatformContext = createContext<PlatformConfig | null>(null)

export function PlatformProvider({
  config,
  children,
}: {
  config: PlatformConfig
  children: React.ReactNode
}) {
  return (
    <PlatformContext.Provider value={config}>
      {children}
    </PlatformContext.Provider>
  )
}

export function usePlatform(): PlatformConfig {
  const config = useContext(PlatformContext)
  if (!config) {
    throw new Error('usePlatform must be used within a PlatformProvider')
  }
  return config
}
