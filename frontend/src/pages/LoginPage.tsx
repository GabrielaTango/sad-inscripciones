import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { UserCircle, User, Lock, LogIn, AlertTriangle } from 'lucide-react'

const LoginPage = () => {
  const [usuario, setUsuario] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()
  const { login } = useAuth()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ usuario, password }),
      })

      if (!response.ok) {
        const data = await response.json()
        setError(data.message || 'Credenciales inválidas.')
        return
      }

      const data = await response.json()
      login({
        token: data.token,
        cuit: data.cuit,
        isAdmin: data.isAdmin ?? false,
        esCapitulo: data.esCapitulo ?? false,
        codVended: data.codVended ?? null,
      })
      if (data.esCapitulo) navigate('/capitulo')
      else if (data.isAdmin) navigate('/admin')
      else navigate('/resumen-cuenta')
    } catch {
      setError('Error de conexión con el servidor.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <section className="bg-gradient-to-br from-slate-50 to-blue-50 min-h-[80vh] py-16 md:py-24">
      <div className="max-w-7xl mx-auto px-4">
        <div className="flex justify-center">
          <div className="w-full max-w-md">
            <div className="bg-white rounded-2xl shadow-lg border border-slate-200">
              <div className="p-8">
                <div className="text-center mb-6">
                  <div className="w-16 h-16 bg-blue-100 rounded-2xl inline-flex items-center justify-center mb-3">
                    <UserCircle className="w-8 h-8 text-blue-600" />
                  </div>
                  <h3 className="mt-2 text-slate-800 font-bold text-xl">Acceso al Sistema</h3>
                  <p className="text-slate-600">Ingrese sus credenciales para acceder</p>
                </div>

                {error && (
                  <div className="alert-danger flex items-center gap-2" role="alert">
                    <AlertTriangle className="w-4 h-4" />
                    {error}
                  </div>
                )}

                <form onSubmit={handleSubmit}>
                  <div className="mb-4">
                    <label htmlFor="usuario" className="form-label">
                      <User className="w-4 h-4 inline mr-1" /> Usuario
                    </label>
                    <input
                      type="text"
                      className="form-input"
                      id="usuario"
                      value={usuario}
                      onChange={(e) => setUsuario(e.target.value)}
                      placeholder="CUIT o usuario"
                      required
                    />
                  </div>

                  <div className="mb-6">
                    <label htmlFor="password" className="form-label">
                      <Lock className="w-4 h-4 inline mr-1" /> Contraseña
                    </label>
                    <input
                      type="password"
                      className="form-input"
                      id="password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      placeholder="Ingrese su contraseña"
                      required
                    />
                  </div>

                  <button
                    type="submit"
                    className="btn-primary w-full py-2.5"
                    disabled={loading}
                  >
                    {loading ? (
                      <>
                        <span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2" role="status" aria-hidden="true"></span>
                        Ingresando...
                      </>
                    ) : (
                      <>
                        <LogIn className="w-4 h-4 inline mr-1" /> Ingresar
                      </>
                    )}
                  </button>
                </form>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

export default LoginPage
