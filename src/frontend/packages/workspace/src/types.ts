export interface WorkspaceNavigation {
  goToNote: (noteId: string) => void
  goToTask: (taskId: string) => void
  goToNewTask: () => void
  goToResearch: (researchId: string) => void
  goToNewResearch: () => void
  goToProject: (projectId: string) => void
  goToMilestone: (projectId: string, milestoneId: string) => void
  goToNotesList: () => void
  goToTasksList: () => void
  goToResearchList: () => void
  goToProjectsList: () => void
}
