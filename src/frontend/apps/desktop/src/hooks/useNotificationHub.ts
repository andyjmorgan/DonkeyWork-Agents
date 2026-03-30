import { useEffect, useRef, useCallback } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'
import { useAuthStore, useActiveConversationsStore } from '@donkeywork/stores'
import type { WorkspaceNotification } from '@donkeywork/api-client'
import {
  isPermissionGranted,
  requestPermission,
  sendNotification,
} from '@tauri-apps/plugin-notification'

const API_BASE_URL = 'https://agents.donkeywork.dev'
const HUB_URL = `${API_BASE_URL}/hubs/notifications`

/**
 * Hook that connects to the SignalR notification hub and displays
 * native macOS notifications via Tauri's notification plugin.
 *
 * Only connects when the user is authenticated. Automatically reconnects
 * with exponential backoff on disconnection.
 */
export function useNotificationHub() {
  const connectionRef = useRef<HubConnection | null>(null)
  const permissionRef = useRef(false)
  const { isAuthenticated, accessToken } = useAuthStore()

  const ensureNotificationPermission = useCallback(async () => {
    let granted = await isPermissionGranted()
    if (!granted) {
      const permission = await requestPermission()
      granted = permission === 'granted'
    }
    permissionRef.current = granted
    return granted
  }, [])

  const connect = useCallback(async () => {
    // Don't connect if already connected or connecting
    if (
      connectionRef.current?.state === HubConnectionState.Connected ||
      connectionRef.current?.state === HubConnectionState.Connecting
    ) {
      return
    }

    // Don't connect without auth
    if (!isAuthenticated || !accessToken) {
      console.log('[NotificationHub] Skipping connection - not authenticated')
      return
    }

    // Request notification permission
    await ensureNotificationPermission()

    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
      } catch {
        // Ignore stop errors
      }
    }

    console.log('[NotificationHub] Building connection to', HUB_URL)

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => {
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
          const delay = Math.min(
            30000,
            Math.pow(2, retryContext.previousRetryCount) * 1000
          )
          console.log(
            `[NotificationHub] Reconnecting in ${delay}ms (attempt ${retryContext.previousRetryCount + 1})`
          )
          return delay
        },
      })
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on(
      'ReceiveNotification',
      (notification: WorkspaceNotification) => {
        console.log(
          '[NotificationHub] Received:',
          notification.type,
          notification.entityId
        )

        if (notification.type === 'ConversationAgentCompleted') {
          const isViewing = useActiveConversationsStore.getState().currentConversationId === notification.entityId
          if (!isViewing && permissionRef.current) {
            sendNotification({
              title: notification.title,
              body: notification.message,
            })
          }
          return
        }

        // Skip activity tracking notifications silently
        if (notification.type === 'ConversationAgentStarted') {
          return
        }

        if (permissionRef.current) {
          sendNotification({
            title: notification.title,
            body: notification.message,
          })
        }
      }
    )

    connection.onclose((error) => {
      console.log('[NotificationHub] Connection closed', error?.message)
    })

    connection.onreconnecting((error) => {
      console.log('[NotificationHub] Reconnecting...', error?.message)
    })

    connection.onreconnected((connectionId) => {
      console.log('[NotificationHub] Reconnected with ID:', connectionId)
    })

    connectionRef.current = connection

    try {
      await connection.start()
      console.log('[NotificationHub] Connected successfully')
    } catch (error) {
      console.error('[NotificationHub] Failed to connect:', error)
    }
  }, [isAuthenticated, accessToken, ensureNotificationPermission])

  const disconnect = useCallback(async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop()
        console.log('[NotificationHub] Disconnected')
      } catch (error) {
        console.error('[NotificationHub] Error disconnecting:', error)
      }
      connectionRef.current = null
    }
  }, [])

  // Auto-connect when authenticated, disconnect when not
  useEffect(() => {
    if (isAuthenticated && accessToken) {
      connect()
    } else {
      disconnect()
    }

    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {
          // Ignore cleanup errors
        })
      }
    }
  }, [isAuthenticated, accessToken, connect, disconnect])
}
