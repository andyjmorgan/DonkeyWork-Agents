import { create } from 'zustand'

interface ActiveConversationsState {
  /** Set of conversation IDs currently being processed by an agent */
  activeIds: Set<string>
  /** Mark a conversation as actively processing */
  add: (conversationId: string) => void
  /** Mark a conversation as no longer processing */
  remove: (conversationId: string) => void
  /** Check if a conversation is currently active */
  isActive: (conversationId: string) => boolean
}

export const useActiveConversationsStore = create<ActiveConversationsState>()(
  (set, get) => ({
    activeIds: new Set<string>(),

    add: (conversationId) => {
      set((state) => {
        const next = new Set(state.activeIds)
        next.add(conversationId)
        return { activeIds: next }
      })
    },

    remove: (conversationId) => {
      set((state) => {
        const next = new Set(state.activeIds)
        next.delete(conversationId)
        return { activeIds: next }
      })
    },

    isActive: (conversationId) => {
      return get().activeIds.has(conversationId)
    },
  })
)
