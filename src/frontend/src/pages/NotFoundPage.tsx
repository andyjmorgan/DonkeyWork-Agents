import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center p-4 text-center">
      <h1 className="text-6xl font-bold">404</h1>
      <p className="mt-4 text-xl text-muted-foreground">
        This page wandered off and got lost
      </p>
      <Button asChild className="mt-6">
        <Link to="/agents">Back to Agents</Link>
      </Button>
    </div>
  )
}
