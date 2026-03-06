import { localStorageAdapter, type PlatformConfig } from '@donkeywork/platform'

let navigateFn: ((path: string) => void) | null = null

export function setNavigate(fn: (path: string) => void): void {
  navigateFn = fn
}

export const webPlatformConfig: PlatformConfig = {
  platform: 'web',
  apiBaseUrl: '',
  wsBaseUrl: `${window.location.protocol === 'https:' ? 'wss:' : 'ws:'}//${window.location.host}`,
  navigate: (path: string) => {
    if (navigateFn) {
      navigateFn(path)
    } else {
      // Fallback before React Router is mounted
      window.location.href = path
    }
  },
  storage: localStorageAdapter,
  applyTheme: (theme: 'light' | 'dark') => {
    if (theme === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  },
  openExternal: (url: string) => {
    window.open(url, '_blank', 'noopener,noreferrer')
  },
}
