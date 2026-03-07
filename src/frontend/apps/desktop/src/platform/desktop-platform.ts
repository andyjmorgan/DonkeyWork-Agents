import { type PlatformConfig, type StorageAdapter } from '@donkeywork/platform'
import { Store } from '@tauri-apps/plugin-store'
import { open } from '@tauri-apps/plugin-shell'

const API_BASE_URL = 'https://agents.donkeywork.dev'
const WS_BASE_URL = 'wss://agents.donkeywork.dev'

let navigateFn: ((path: string) => void) | null = null

export function setDesktopNavigate(fn: (path: string) => void): void {
  navigateFn = fn
}

class TauriStorageAdapter implements StorageAdapter {
  private store: Store | null = null
  private cache = new Map<string, string>()
  private initPromise: Promise<void> | null = null

  private async init(): Promise<void> {
    if (!this.initPromise) {
      this.initPromise = (async () => {
        this.store = await Store.load('settings.json')
      })()
    }
    return this.initPromise
  }

  async getItem(name: string): Promise<string | null> {
    if (this.cache.has(name)) return this.cache.get(name)!
    await this.init()
    const value = await this.store!.get<string>(name)
    if (value !== null && value !== undefined) {
      this.cache.set(name, value)
    }
    return value ?? null
  }

  async setItem(name: string, value: string): Promise<void> {
    this.cache.set(name, value)
    await this.init()
    await this.store!.set(name, value)
  }

  async removeItem(name: string): Promise<void> {
    this.cache.delete(name)
    await this.init()
    await this.store!.delete(name)
  }
}

const tauriStorage = new TauriStorageAdapter()

export const desktopPlatformConfig: PlatformConfig = {
  platform: 'desktop',
  apiBaseUrl: API_BASE_URL,
  wsBaseUrl: WS_BASE_URL,
  navigate: (path: string) => {
    if (navigateFn) {
      navigateFn(path)
    }
  },
  storage: tauriStorage,
  applyTheme: (theme: 'light' | 'dark') => {
    if (theme === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  },
  openExternal: (url: string) => {
    open(url)
  },
}
