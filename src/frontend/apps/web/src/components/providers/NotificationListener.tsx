import { useNotifications } from '@/hooks/useNotifications'
import { toast } from 'sonner'
import type { WorkspaceNotification, NotificationType } from '@donkeywork/api-client'

/**
 * Get the appropriate toast variant based on notification type.
 */
function getToastVariant(type: NotificationType): 'success' | 'info' | 'warning' {
  if (type.endsWith('Created')) return 'success'
  if (type.endsWith('Deleted')) return 'warning'
  return 'info'
}

/**
 * Get an icon/emoji for the notification type.
 */
function getToastIcon(type: NotificationType): string {
  if (type.startsWith('Project')) return 'project'
  if (type.startsWith('Milestone')) return 'milestone'
  if (type.startsWith('Task')) return 'task'
  if (type.startsWith('Note')) return 'note'
  return 'notification'
}

/**
 * Component that listens for real-time notifications and displays them as toasts.
 * Should be rendered once in the app when the user is authenticated.
 */
export function NotificationListener() {
  useNotifications({
    onNotification: (notification: WorkspaceNotification) => {
      const variant = getToastVariant(notification.type)
      const icon = getToastIcon(notification.type)

      // Use different toast methods based on variant
      switch (variant) {
        case 'success':
          toast.success(notification.title, {
            description: notification.message,
            id: `${icon}-${notification.entityId}`,
          })
          break
        case 'warning':
          toast.warning(notification.title, {
            description: notification.message,
            id: `${icon}-${notification.entityId}`,
          })
          break
        default:
          toast.info(notification.title, {
            description: notification.message,
            id: `${icon}-${notification.entityId}`,
          })
      }
    },
    autoConnect: true,
  })

  // This component doesn't render anything visible
  return null
}
