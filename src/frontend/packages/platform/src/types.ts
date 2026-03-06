export interface StorageAdapter {
  getItem: (name: string) => string | null | Promise<string | null>
  setItem: (name: string, value: string) => void | Promise<void>
  removeItem: (name: string) => void | Promise<void>
}

export interface PlatformConfig {
  platform: 'web' | 'desktop'
  apiBaseUrl: string
  wsBaseUrl: string
  navigate: (path: string) => void
  storage: StorageAdapter
  applyTheme: (theme: 'light' | 'dark') => void
  openExternal?: (url: string) => void
}
