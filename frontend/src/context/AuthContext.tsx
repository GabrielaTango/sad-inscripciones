import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'

interface AuthState {
  isAuthenticated: boolean
  isAdmin: boolean
  cuit: string | null
  token: string | null
  login: (token: string, cuit: string, isAdmin: boolean) => void
  logout: () => void
}

const AuthContext = createContext<AuthState>({
  isAuthenticated: false,
  isAdmin: false,
  cuit: null,
  token: null,
  login: () => {},
  logout: () => {},
})

export const useAuth = () => useContext(AuthContext)

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem('sad_token'))
  const [cuit, setCuit] = useState<string | null>(localStorage.getItem('sad_cuit'))
  const [isAdmin, setIsAdmin] = useState<boolean>(localStorage.getItem('sad_is_admin') === 'true')

  const isAuthenticated = !!token

  const login = (newToken: string, newCuit: string, newIsAdmin: boolean) => {
    localStorage.setItem('sad_token', newToken)
    localStorage.setItem('sad_cuit', newCuit)
    localStorage.setItem('sad_is_admin', String(newIsAdmin))
    setToken(newToken)
    setCuit(newCuit)
    setIsAdmin(newIsAdmin)
  }

  const logout = () => {
    localStorage.removeItem('sad_token')
    localStorage.removeItem('sad_cuit')
    localStorage.removeItem('sad_is_admin')
    setToken(null)
    setCuit(null)
    setIsAdmin(false)
  }

  useEffect(() => {
    const storedToken = localStorage.getItem('sad_token')
    const storedCuit = localStorage.getItem('sad_cuit')
    const storedIsAdmin = localStorage.getItem('sad_is_admin') === 'true'
    if (storedToken && storedCuit) {
      setToken(storedToken)
      setCuit(storedCuit)
      setIsAdmin(storedIsAdmin)
    }
  }, [])

  return (
    <AuthContext.Provider value={{ isAuthenticated, isAdmin, cuit, token, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
