import { useNotifications } from '@/hooks/useNotifications'
import { toast } from 'sonner'
import { useNavigate } from 'react-router-dom'
import { useActiveConversationsStore, useAudioRecordingEventsStore } from '@donkeywork/stores'
import type { WorkspaceNotification } from '@donkeywork/api-client'

export function NotificationListener() {
  const navigate = useNavigate()
  const { add: addActiveConversation, remove: removeActiveConversation } = useActiveConversationsStore()
  const { recordUpdate: recordAudioUpdate } = useAudioRecordingEventsStore()

  useNotifications({
    onNotification: (notification: WorkspaceNotification) => {
      if (notification.type === 'ConversationAgentStarted') {
        addActiveConversation(notification.entityId)
        return
      }

      if (notification.type === 'AudioRecordingUpdated') {
        recordAudioUpdate(notification.entityId, notification.parentId ?? null)
        toast.success(notification.title, {
          description: notification.message,
          id: `audio-${notification.entityId}`,
        })
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
