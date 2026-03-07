import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import type { WorkspaceNavigation } from '@donkeywork/workspace'

export function useWorkspaceNav(): WorkspaceNavigation {
  const navigate = useNavigate()
  return useMemo(() => ({
    goToNote: (id: string) => navigate(`/notes/${id}`),
    goToTask: (id: string) => navigate(`/tasks/${id}`),
    goToNewTask: () => navigate('/tasks/new'),
    goToResearch: (id: string) => navigate(`/research/${id}`),
    goToNewResearch: () => navigate('/research/new'),
    goToProject: (id: string) => navigate(`/workspace/${id}`),
    goToMilestone: (pid: string, mid: string) => navigate(`/workspace/${pid}/milestones/${mid}`),
    goToNotesList: () => navigate('/notes'),
    goToTasksList: () => navigate('/tasks'),
    goToResearchList: () => navigate('/research'),
    goToProjectsList: () => navigate('/workspace'),
  }), [navigate])
}
