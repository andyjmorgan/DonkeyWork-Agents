export type Page =
  | 'chat'
  | 'conversations'
  | 'notes'
  | 'research'
  | 'tasks'
  | 'projects'
  | 'settings'
  | 'note-editor'
  | 'task-editor'
  | 'research-editor'
  | 'project-detail'
  | 'milestone-detail'

export interface PageParams {
  conversationId?: string
  noteId?: string
  taskId?: string
  isNew?: boolean
  researchId?: string
  projectId?: string
  milestoneId?: string
}
