import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'

interface LoginPayload {
  token: string
  cuit: string
  isAdmin: boolean
  esCapitulo?: boolean
  codVended?: string | null
}

interface AuthState {
  isAuthenticated: boolean
  isAdmin: boolean
  esCapitulo: boolean
  codVended: string | null
  cuit: string | null
  token: string | null
  login: (payload: LoginPayload) => void
  logout: () => void
}

const AuthContext = createContext<AuthState>({
  isAuthenticated: false,
  isAdmin: false,
  esCapitulo: false,
  codVended: null,
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
  const [esCapitulo, setEsCapitulo] = useState<boolean>(localStorage.getItem('sad_es_capitulo') === 'true')
  const [codVended, setCodVended] = useState<string | null>(localStorage.getItem('sad_cod_vended'))

  const isAuthenticated = !!token

  const login = (payload: LoginPayload) => {
    const cap = payload.esCapitulo ?? false
    const cv = payload.codVended ?? null
    localStorage.setItem('sad_token', payload.token)
    localStorage.setItem('sad_cuit', payload.cuit)
    localStorage.setItem('sad_is_admin', String(payload.isAdmin))
    localStorage.setItem('sad_es_capitulo', String(cap))
    if (cv) localStorage.setItem('sad_cod_vended', cv)
    else localStorage.removeItem('sad_cod_vended')
    setToken(payload.token)
    setCuit(payload.cuit)
    setIsAdmin(payload.isAdmin)
    setEsCapitulo(cap)
    setCodVended(cv)
  }

  const logout = () => {
    localStorage.removeItem('sad_token')
    localStorage.removeItem('sad_cuit')
    localStorage.removeItem('sad_is_admin')
    localStorage.removeItem('sad_es_capitulo')
    localStorage.removeItem('sad_cod_vended')
    setToken(null)
    setCuit(null)
    setIsAdmin(false)
    setEsCapitulo(false)
    setCodVended(null)
  }

  useEffect(() => {
    const storedToken = localStorage.getItem('sad_token')
    const storedCuit = localStorage.getItem('sad_cuit')
    const storedIsAdmin = localStorage.getItem('sad_is_admin') === 'true'
    const storedEsCapitulo = localStorage.getItem('sad_es_capitulo') === 'true'
    const storedCodVended = localStorage.getItem('sad_cod_vended')
    if (storedToken && storedCuit) {
      setToken(storedToken)
      setCuit(storedCuit)
      setIsAdmin(storedIsAdmin)
      setEsCapitulo(storedEsCapitulo)
      setCodVended(storedCodVended)
    }
  }, [])

  return (
    <AuthContext.Provider value={{ isAuthenticated, isAdmin, esCapitulo, codVended, cuit, token, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}
