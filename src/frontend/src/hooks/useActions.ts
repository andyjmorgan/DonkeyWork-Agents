import { useMemo } from 'react'
import type { ActionNodeSchema } from '@/types/actions'
import actionsSchemaRaw from '@/schemas/actions.json'

/**
 * Hook to load action schemas from the generated schema file
 * NOTE: This uses the static JSON file. Switch to API once backend is running.
 */
export function useActions() {
  // Import JSON directly - no async loading needed
  const allActions = actionsSchemaRaw as unknown as ActionNodeSchema[]

  // Filter to only enabled actions
  const actions = useMemo(
    () => allActions.filter(action => action.enabled),
    [allActions]
  )

  // Group actions by category
  const actionsByCategory = actions.reduce((acc, action) => {
    if (!acc[action.category]) {
      acc[action.category] = []
    }
    acc[action.category].push(action)
    return acc
  }, {} as Record<string, ActionNodeSchema[]>)

  // Get action by type
  const getAction = (actionType: string) => {
    return actions.find(action => action.actionType === actionType)
  }

  return {
    actions,
    actionsByCategory,
    getAction
  }
}
