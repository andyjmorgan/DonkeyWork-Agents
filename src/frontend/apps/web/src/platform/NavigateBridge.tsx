import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { setNavigate } from './web-platform'

export function NavigateBridge({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate()

  useEffect(() => {
    setNavigate((path: string) => navigate(path))
  }, [navigate])

  return <>{children}</>
}
