import { Navigate } from 'react-router-dom'
import { useAuth } from '../../context/AuthContext'

const CapituloRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated, esCapitulo } = useAuth()

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (!esCapitulo) return <Navigate to="/" replace />

  return <>{children}</>
}

export default CapituloRoute
