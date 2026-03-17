import { useParams } from 'react-router-dom'
import { SkillDetailView } from '@/components/skills/SkillDetailView'

export function SkillDetailPage() {
  const { name } = useParams<{ name: string }>()

  if (!name) {
    return (
      <div className="flex items-center justify-center p-12">
        <p className="text-muted-foreground">Skill not found</p>
      </div>
    )
  }

  return <SkillDetailView name={name} />
}
