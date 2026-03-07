import { ProjectsPage as SharedProjectsPage } from '@donkeywork/workspace'
import { useDesktopWorkspaceNav } from '../hooks/useDesktopWorkspaceNav'
import type { Page, PageParams } from '../types'

interface Props {
  onNavigate: (page: Page, params?: PageParams) => void
}

export function ProjectsPage({ onNavigate }: Props) {
  const nav = useDesktopWorkspaceNav(onNavigate)
  return <SharedProjectsPage nav={nav} />
}
