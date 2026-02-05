import type { ReactNode } from 'react'
import { Trash2, Loader2, Calendar, CheckSquare, Circle, Clock, CheckCircle2, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { MarkdownViewer } from '@/components/editor/MarkdownViewer'
import type { TodoStatus, TodoPriority } from '@/lib/api'

const statusIcons: Record<TodoStatus, ReactNode> = {
  Pending: <Circle className="h-4 w-4 text-gray-400" />,
  InProgress: <Clock className="h-4 w-4 text-blue-500" />,
  Completed: <CheckCircle2 className="h-4 w-4 text-green-500" />,
  Cancelled: <AlertCircle className="h-4 w-4 text-red-500" />,
}

const priorityColors: Record<TodoPriority, string> = {
  Low: 'bg-gray-500',
  Medium: 'bg-blue-500',
  High: 'bg-orange-500',
  Critical: 'bg-red-500',
}

interface ContentCardProps {
  /** Card title */
  title: string
  /** Markdown content to display */
  content?: string
  /** Click handler for the card */
  onClick?: () => void
  /** Delete handler - shows delete button when provided */
  onDelete?: () => void
  /** Whether delete is in progress */
  isDeleting?: boolean
  /** Created/updated date to display in footer */
  date?: string
  /** Optional icon to show next to title */
  icon?: ReactNode
  /** Tags to display (for notes) */
  tags?: Array<{ id: string; name: string }>
  // Task-specific props
  /** Task status - enables task mode when provided */
  status?: TodoStatus
  /** Task priority */
  priority?: TodoPriority
  /** Task due date */
  dueDate?: string
  /** Handler for toggling task completion */
  onToggleComplete?: () => void
  /** Whether the card content should be strikethrough (completed state) */
  isCompleted?: boolean
}

/**
 * Shared card component for displaying tasks and notes with consistent styling.
 * Renders markdown content and includes optional task-specific features.
 */
export function ContentCard({
  title,
  content,
  onClick,
  onDelete,
  isDeleting,
  date,
  icon,
  tags,
  status,
  priority,
  dueDate,
  onToggleComplete,
  isCompleted,
}: ContentCardProps) {
  const isTask = status !== undefined

  return (
    <div
      className={`group rounded-lg border border-border bg-card p-4 hover:shadow-md transition-shadow cursor-pointer h-[330px] flex flex-col ${
        isCompleted ? 'opacity-75 bg-card/50' : ''
      }`}
      onClick={onClick}
    >
      {/* Title row */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0 flex-1">
          {/* Task completion checkbox */}
          {isTask && onToggleComplete && (
            <button
              onClick={(e) => {
                e.stopPropagation()
                onToggleComplete()
              }}
              className={`h-5 w-5 rounded border-2 flex items-center justify-center transition-colors shrink-0 ${
                isCompleted
                  ? 'bg-primary border-primary text-primary-foreground'
                  : 'border-muted-foreground hover:border-primary'
              }`}
              title={isCompleted ? 'Mark as pending' : 'Complete task'}
            >
              {isCompleted && <CheckSquare className="h-3 w-3" />}
            </button>
          )}
          {/* Icon (for notes) */}
          {icon && !isTask && (
            <span className="shrink-0">{icon}</span>
          )}
          {/* Status icon (for tasks without toggle) */}
          {isTask && !onToggleComplete && status && (
            <span className="shrink-0">{statusIcons[status]}</span>
          )}
          <h4 className={`font-medium truncate ${isCompleted ? 'line-through text-muted-foreground' : ''}`}>
            {title}
          </h4>
        </div>

        {/* Delete button */}
        {onDelete && (
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-destructive opacity-0 group-hover:opacity-100 transition-opacity shrink-0"
            onClick={(e) => {
              e.stopPropagation()
              onDelete()
            }}
            disabled={isDeleting}
          >
            {isDeleting ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              <Trash2 className="h-3 w-3" />
            )}
          </Button>
        )}
      </div>

      {/* Content - rendered as markdown */}
      {content && (
        <div className="mt-3 flex-1 overflow-hidden">
          <div className="text-sm text-muted-foreground line-clamp-4">
            <MarkdownViewer content={content} className="prose-p:my-1 prose-headings:my-1" />
          </div>
        </div>
      )}

      {/* Footer */}
      <div className="mt-auto pt-3 flex items-center gap-2 flex-wrap border-t border-border/50 -mx-4 px-4 -mb-4 pb-3 bg-muted/30 rounded-b-lg">
        {/* Task metadata (priority, status, due date) */}
        {isTask && (
          <div className="flex items-center gap-2 flex-wrap">
            {priority && (
              <Badge variant="outline" className={`${priorityColors[priority]} text-white border-0 text-xs`}>
                {priority}
              </Badge>
            )}
            {status && (
              <Badge
                variant={
                  status === 'Completed' ? 'success' :
                  status === 'InProgress' ? 'inProgress' :
                  status === 'Cancelled' ? 'destructive' :
                  'pending'
                }
              >
                {status}
              </Badge>
            )}
            {dueDate && (
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <Calendar className="h-3 w-3" />
                {new Date(dueDate).toLocaleDateString()}
              </span>
            )}
          </div>
        )}

        {/* Tags (for notes) */}
        {tags && tags.length > 0 && (
          <div className="flex items-center gap-1 flex-wrap">
            {tags.slice(0, 3).map((tag) => (
              <Badge key={tag.id} variant="secondary" className="text-xs">
                {tag.name}
              </Badge>
            ))}
            {tags.length > 3 && (
              <Badge variant="secondary" className="text-xs">
                +{tags.length - 3}
              </Badge>
            )}
          </div>
        )}

        {/* Date */}
        {date && (
          <span className="text-xs text-muted-foreground ml-auto">
            {new Date(date).toLocaleDateString()}
          </span>
        )}
      </div>
    </div>
  )
}
