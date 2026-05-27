import { useNotifications } from '@/hooks/useNotifications'
import { toast } from 'sonner'
import { useNavigate } from 'react-router-dom'
import { useActiveConversationsStore } from '@donkeywork/stores'
import type { WorkspaceNotification } from '@donkeywork/api-client'

export function NotificationListener() {
  const navigate = useNavigate()
  const { add: addActiveConversation, remove: removeActiveConversation } = useActiveConversationsStore()

  useNotifications({
    onNotification: (notification: WorkspaceNotification) => {
      if (notification.type === 'ConversationAgentStarted') {
        addActiveConversation(notification.entityId)
        return
      }

      if (notification.type === 'ConversationAgentCompleted') {
        removeActiveConversation(notification.entityId)
        const isViewingConversation = useActiveConversationsStore.getState().currentConversationId === notification.entityId
        if (!isViewingConversation) {
          toast.success(notification.title, {
            description: notification.message,
            action: {
              label: 'View',
              onClick: () => navigate(`/agent-chat/${notification.entityId}`),
            },
          })
        }
        return
      }
    },
    autoConnect: true,
  })

  return null
}
