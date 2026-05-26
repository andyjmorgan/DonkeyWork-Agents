export type Page =
  | 'chat'
  | 'conversations'
  | 'settings'

export interface PageParams {
  conversationId?: string
}
