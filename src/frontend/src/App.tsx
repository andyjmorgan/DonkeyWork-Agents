import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AppLayout } from '@/components/layout'
import { AgentsPage, ApiKeysPage, SecretsPage, LoginPage, LoginCallbackPage, NotFoundPage, ProfilePage } from '@/pages'
import { useAuthStore } from '@/store/auth'

export default function App() {
  const { isAuthenticated } = useAuthStore()

  return (
    <BrowserRouter>
      <Routes>
        {/* Public routes */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/login/callback" element={<LoginCallbackPage />} />

        {/* Protected routes */}
        {isAuthenticated ? (
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/agents" replace />} />
            <Route path="/agents" element={<AgentsPage />} />
            <Route path="/api-keys" element={<ApiKeysPage />} />
            <Route path="/secrets" element={<SecretsPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        ) : (
          <Route path="*" element={<Navigate to="/login" replace />} />
        )}
      </Routes>
    </BrowserRouter>
  )
}
