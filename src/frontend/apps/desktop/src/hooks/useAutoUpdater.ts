import { useEffect } from 'react'
import { check } from '@tauri-apps/plugin-updater'
import { relaunch } from '@tauri-apps/plugin-process'

export function useAutoUpdater() {
  useEffect(() => {
    async function checkForUpdates() {
      try {
        const update = await check()
        if (update) {
          console.log(`[Updater] Update available: ${update.version}`)
          // Download and install
          await update.downloadAndInstall()
          // Prompt relaunch
          await relaunch()
        }
      } catch (err) {
        console.error('[Updater] Check failed:', err)
      }
    }

    // Check on launch (with 5s delay to not block startup)
    const initialTimeout = setTimeout(checkForUpdates, 5000)

    // Check every 4 hours
    const interval = setInterval(checkForUpdates, 4 * 60 * 60 * 1000)

    return () => {
      clearTimeout(initialTimeout)
      clearInterval(interval)
    }
  }, [])
}
