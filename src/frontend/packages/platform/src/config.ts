import type { PlatformConfig } from './types'

let _config: PlatformConfig | null = null

export function configurePlatform(config: PlatformConfig): void {
  _config = config
}

export function getPlatformConfig(): PlatformConfig {
  if (!_config) {
    throw new Error(
      'Platform not configured. Call configurePlatform() before using platform features.'
    )
  }
  return _config
}
