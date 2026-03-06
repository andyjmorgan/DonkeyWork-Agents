import type { StorageAdapter } from './types'

export const localStorageAdapter: StorageAdapter = {
  getItem: (name) => localStorage.getItem(name),
  setItem: (name, value) => localStorage.setItem(name, value),
  removeItem: (name) => localStorage.removeItem(name),
}
