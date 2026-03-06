import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import { createPlatformStorage, getPlatformConfig } from '@donkeywork/platform'

type Theme = 'light' | 'dark'

interface ThemeState {
  theme: Theme
  setTheme: (theme: Theme) => void
  toggleTheme: () => void
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'dark',
      setTheme: (theme) => {
        set({ theme })
        getPlatformConfig().applyTheme(theme)
      },
      toggleTheme: () => {
        const newTheme = get().theme === 'light' ? 'dark' : 'light'
        set({ theme: newTheme })
        getPlatformConfig().applyTheme(newTheme)
      },
    }),
    {
      name: 'donkeywork-theme',
      storage: createJSONStorage(() => createPlatformStorage()),
      onRehydrateStorage: () => (state) => {
        if (state) {
          getPlatformConfig().applyTheme(state.theme)
        }
      },
    }
  )
)
