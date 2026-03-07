import { useMemo } from 'react'
import type { WorkspaceNavigation } from '@donkeywork/workspace'
import type { Page, PageParams } from '../types'

export function useDesktopWorkspaceNav(
  onNavigate: (page: Page, params?: PageParams) => void
): WorkspaceNavigation {
  return useMemo(() => ({
    goToNote: (id: string) => onNavigate('note-editor', { noteId: id }),
    goToTask: (id: string) => onNavigate('task-editor', { taskId: id }),
    goToNewTask: () => onNavigate('task-editor', { isNew: true }),
    goToResearch: (id: string) => onNavigate('research-editor', { researchId: id }),
    goToNewResearch: () => onNavigate('research-editor', { isNew: true }),
    goToProject: (id: string) => onNavigate('project-detail', { projectId: id }),
    goToMilestone: (pid: string, mid: string) => onNavigate('milestone-detail', { projectId: pid, milestoneId: mid }),
    goToNotesList: () => onNavigate('notes'),
    goToTasksList: () => onNavigate('tasks'),
    goToResearchList: () => onNavigate('research'),
    goToProjectsList: () => onNavigate('projects'),
  }), [onNavigate])
}
