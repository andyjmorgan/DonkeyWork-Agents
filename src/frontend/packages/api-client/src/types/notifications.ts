/**
 * Types for real-time notifications via SignalR.
 * These types mirror the backend contracts in DonkeyWork.Agents.Notifications.Contracts.
 */

/**
 * Types of real-time notifications.
 */
export type NotificationType =
  // Project notifications
  | 'ProjectCreated'
  | 'ProjectUpdated'
  | 'ProjectDeleted'
  // Milestone notifications
  | 'MilestoneCreated'
  | 'MilestoneUpdated'
  | 'MilestoneDeleted'
  // Task notifications
  | 'TaskCreated'
  | 'TaskUpdated'
  | 'TaskDeleted'
  // Note notifications
  | 'NoteCreated'
  | 'NoteUpdated'
  | 'NoteDeleted'
  // Conversation notifications
  | 'ConversationAgentStarted'
  | 'ConversationAgentCompleted'
  // Audio recording notifications
  | 'AudioRecordingUpdated'

/**
 * Notification payload received from SignalR hub.
 */
export interface WorkspaceNotification {
  /** The type of notification */
  type: NotificationType
  /** Human-readable title for the notification */
  title: string
  /** Human-readable message for the notification */
  message: string
  /** The ID of the entity that was affected */
  entityId: string
  /** Optional parent entity ID for hierarchical entities */
  parentId?: string | null
  /** Timestamp when the notification was created */
  timestamp: string
}

/**
 * SignalR connection states.
 */
export type ConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting'
