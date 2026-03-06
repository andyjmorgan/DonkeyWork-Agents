import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { configurePlatform } from '@donkeywork/platform'
import { webPlatformConfig } from './platform/web-platform'

// Configure platform before any store imports trigger hydration
configurePlatform(webPlatformConfig)

// Apply theme immediately to prevent flash of wrong theme
const savedTheme = localStorage.getItem('donkeywork-theme')
if (savedTheme) {
  try {
    const parsed = JSON.parse(savedTheme)
    webPlatformConfig.applyTheme(parsed.state?.theme || 'dark')
  } catch {
    webPlatformConfig.applyTheme('dark')
  }
} else {
  webPlatformConfig.applyTheme('dark')
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
