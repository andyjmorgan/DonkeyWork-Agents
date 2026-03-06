import { configurePlatform } from '@donkeywork/platform'
import { desktopPlatformConfig } from './platform/desktop-platform'

// Configure platform before any store imports
configurePlatform(desktopPlatformConfig)

// Apply theme immediately to prevent FOUC
// Desktop uses Tauri store (async), so default to dark
document.documentElement.classList.add('dark')

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
