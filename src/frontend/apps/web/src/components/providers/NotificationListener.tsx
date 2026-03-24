import { useNotifications } from '@/hooks/useNotifications'
import { toast } from 'sonner'
import { useNavigate } from 'react-router-dom'
import { useActiveConversationsStore } from '@donkeywork/stores'
import type { WorkspaceNotification, NotificationType } from '@donkeywork/api-client'

/**
 * Get the appropriate toast variant based on notification type.
 */
function getToastVariant(type: NotificationType): 'success' | 'info' | 'warning' {
  if (type.endsWith('Created')) return 'success'
  if (type.endsWith('Deleted')) return 'warning'
  if (type.endsWith('Completed')) return 'success'
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
  if (type.startsWith('Conversation')) return 'conversation'
  return 'notification'
}

/**
 * Component that listens for real-time notifications and displays them as toasts.
 * Should be rendered once in the app when the user is authenticated.
 */
export function NotificationListener() {
  const navigate = useNavigate()
  const { add: addActiveConversation, remove: removeActiveConversation } = useActiveConversationsStore()

  useNotifications({
    onNotification: (notification: WorkspaceNotification) => {
      // Handle conversation activity tracking
      if (notification.type === 'ConversationAgentStarted') {
        addActiveConversation(notification.entityId)
        return
      }

      if (notification.type === 'ConversationAgentCompleted') {
        removeActiveConversation(notification.entityId)
        toast.success(notification.title, {
          description: notification.message,
          action: {
            label: 'View',
            onClick: () => navigate(`/agent-chat/${notification.entityId}`),
          },
        })
        return
      }

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
