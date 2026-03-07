import { ResearchPage as SharedResearchPage } from '@donkeywork/workspace'
import { useWorkspaceNav } from '@/hooks/useWorkspaceNav'

export function ResearchPage() {
  const nav = useWorkspaceNav()
  return <SharedResearchPage nav={nav} />
}
