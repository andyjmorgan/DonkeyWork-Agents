import { useAuthStore } from '@/store/auth'

export function ProfilePage() {
  const { user } = useAuthStore()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Profile</h1>
        <p className="text-muted-foreground">
          Your account information
        </p>
      </div>

      <div className="rounded-lg border border-border p-6 space-y-4">
        <div>
          <label className="text-sm font-medium text-muted-foreground">Name</label>
          <p className="text-lg">{user?.name || 'Not set'}</p>
        </div>

        <div>
          <label className="text-sm font-medium text-muted-foreground">Email</label>
          <p className="text-lg">{user?.email || 'Not set'}</p>
        </div>

        <div>
          <label className="text-sm font-medium text-muted-foreground">Username</label>
          <p className="text-lg">{user?.username || 'Not set'}</p>
        </div>

        <div>
          <label className="text-sm font-medium text-muted-foreground">User ID</label>
          <p className="text-sm font-mono text-muted-foreground">{user?.id || 'Unknown'}</p>
        </div>
      </div>
    </div>
  )
}
