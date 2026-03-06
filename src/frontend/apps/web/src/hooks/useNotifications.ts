import { useEffect, useRef, useState, useCallback } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'
import { useAuthStore } from '@donkeywork/stores'
import type { WorkspaceNotification, ConnectionState } from '@donkeywork/api-client'

const HUB_URL = '/hubs/notifications'

interface UseNotificationsOptions {
  /** Called when a notification is received */
  onNotification?: (notification: WorkspaceNotification) => void
  /** Whether to automatically connect when authenticated */
  autoConnect?: boolean
}

interface UseNotificationsReturn {
  /** Current connection state */
  connectionState: ConnectionState
  /** Manually connect to the hub */
  connect: () => Promise<void>
  /** Manually disconnect from the hub */
  disconnect: () => Promise<void>
}

/**
 * Hook for receiving real-time notifications via SignalR.
 *
 * @example
 * ```tsx
 * useNotifications({
 *   onNotification: (notification) => {
 *     toast(notification.title, { description: notification.message })
 *   },
 * })
 * ```
 */
export function useNotifications(
  options: UseNotificationsOptions = {}
): UseNotificationsReturn {
  const { onNotification, autoConnect = true } = options

  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected')
  const connectionRef = useRef<HubConnection | null>(null)
  const onNotificationRef = useRef(onNotification)

  // Keep callback ref up to date (in useEffect to avoid updating during render)
  useEffect(() => {
    onNotificationRef.current = onNotification
  }, [onNotification])

  const { isAuthenticated, accessToken } = useAuthStore()

  const updateConnectionState = useCallback((connection: HubConnection) => {
    switch (connection.state) {
      case HubConnectionState.Connected:
        setConnectionState('connected')
        break
      case HubConnectionState.Connecting:
        setConnectionState('connecting')
        break
      case HubConnectionState.Reconnecting:
        setConnectionState('reconnecting')
        break
      case HubConnectionState.Disconnected:
      case HubConnectionState.Disconnecting:
      default:
        setConnectionState('disconnected')
        break
    }
  }, [])

  const connect = useCallback(async () => {
    // Don't connect if already connected or connecting
    if (connectionRef.current?.state === HubConnectionState.Connected ||
        connectionRef.current?.state === HubConnectionState.Connecting) {
      return
    }

    // Don't connect without auth
    if (!isAuthenticated || !accessToken) {
      console.log('[Notifications] Skipping connection - not authenticated')
      return
    }

    // Clean up existing connection
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
      } catch {
        // Ignore stop errors
      }
    }

    console.log('[Notifications] Building connection to', HUB_URL)

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => {
          // Get fresh token each time (in case it was refreshed)
          const currentToken = useAuthStore.getState().accessToken
          return currentToken ?? ''
        },
        skipNegotiation: true,
        transport: HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 4s, 8s, 16s, then cap at 30s
          if (retryContext.previousRetryCount === 0) return 0
          const delay = Math.min(30000, Math.pow(2, retryContext.previousRetryCount) * 1000)
          console.log(`[Notifications] Reconnecting in ${delay}ms (attempt ${retryContext.previousRetryCount + 1})`)
          return delay
        },
      })
      .configureLogging(LogLevel.Warning)
      .build()

    // Set up event handlers
    connection.on('ReceiveNotification', (notification: WorkspaceNotification) => {
      console.log('[Notifications] Received:', notification.type, notification.entityId)
      onNotificationRef.current?.(notification)
    })

    connection.onclose((error) => {
      console.log('[Notifications] Connection closed', error?.message)
      setConnectionState('disconnected')
    })

    connection.onreconnecting((error) => {
      console.log('[Notifications] Reconnecting...', error?.message)
      setConnectionState('reconnecting')
    })

    connection.onreconnected((connectionId) => {
      console.log('[Notifications] Reconnected with ID:', connectionId)
      setConnectionState('connected')
    })

    connectionRef.current = connection
    setConnectionState('connecting')

    try {
      await connection.start()
      console.log('[Notifications] Connected successfully')
      updateConnectionState(connection)
    } catch (error) {
      console.error('[Notifications] Failed to connect:', error)
      setConnectionState('disconnected')
    }
  }, [isAuthenticated, accessToken, updateConnectionState])

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
        console.log('[Notifications] Disconnected')
      } catch (error) {
        console.error('[Notifications] Error disconnecting:', error)
      }
      connectionRef.current = null
      setConnectionState('disconnected')
    }
  }, [])

  // Auto-connect when authenticated
  useEffect(() => {
    if (autoConnect && isAuthenticated && accessToken) {
      connect()
    } else if (!isAuthenticated) {
      disconnect()
    }

    return () => {
      // Cleanup on unmount
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {
          // Ignore cleanup errors
        })
      }
    }
  }, [autoConnect, isAuthenticated, accessToken, connect, disconnect])

  return {
    connectionState,
    connect,
    disconnect,
  }
}
