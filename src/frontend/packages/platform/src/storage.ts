import type { StorageAdapter } from './types'
import { getPlatformConfig } from './config'

export function createPlatformStorage(): StorageAdapter {
  return {
    getItem: (name) => getPlatformConfig().storage.getItem(name),
    setItem: (name, value) => getPlatformConfig().storage.setItem(name, value),
    removeItem: (name) => getPlatformConfig().storage.removeItem(name),
  }
}
