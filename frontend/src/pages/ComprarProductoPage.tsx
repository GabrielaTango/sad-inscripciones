import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { Send, AlertCircle } from 'lucide-react'
import { productosService } from '../services/productosService'
import { ventasProductoService } from '../services/ventasProductoService'
import type { ProductoPublico } from '../types/models'

const ComprarProductoPage = () => {
  const { id: idParam } = useParams<{ id: string }>()
  const id = Number(idParam)

  const [producto, setProducto] = useState<ProductoPublico | null>(null)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const [dni, setDni] = useState('')
  const [nombre, setNombre] = useState('')
  const [apellido, setApellido] = useState('')
  const [email, setEmail] = useState('')
  const [datosExtra, setDatosExtra] = useState<Record<string, string>>({})

  useEffect(() => {
    const load = async () => {
      try {
        const p = await productosService.getById(id)
        setProducto(p)
      } catch {
        setError('No se pudo cargar el producto.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [id])

  // Limpia el error inline de un campo (base o extra) a medida que se corrige.
  const clearFieldError = (key: string) => setErrors(prev => (prev[key] ? { ...prev, [key]: '' } : prev))

  const setExtra = (key: string, value: string) => {
    setDatosExtra(prev => ({ ...prev, [key]: value }))
    clearFieldError(key)
  }

  // className del input/select agregando borde rojo si el campo tiene error.
  const cls = (key: string, base: string) => (errors[key] ? `${base} border-red-500` : base)

  // Mensaje de error inline debajo del campo.
  const errorMsg = (key: string) =>
    errors[key] ? (
      <p className="mt-1 text-sm text-red-600 flex items-center gap-1">
        <AlertCircle size={14} />
        {errors[key]}
      </p>
    ) : null

  // Valida los datos del comprador y, en espejo del backend, los DatosExtra contra
  // el esquema CamposExtra del producto.
  const validar = (): boolean => {
    const next: Record<string, string> = {}

    const dniLimpio = dni.replace(/[.\s]/g, '')
    if (!/^\d{7,8}$/.test(dniLimpio)) next.dni = 'El DNI debe tener 7 u 8 dígitos'
    if (!nombre.trim()) next.nombre = 'Este campo es obligatorio'
    if (!apellido.trim()) next.apellido = 'Este campo es obligatorio'
    if (!email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) next.email = 'Email inválido'

    for (const campo of producto?.camposExtra ?? []) {
      const valor = (datosExtra[campo.key] ?? '').trim()
      if (campo.required && !valor) {
        next[campo.key] = 'Este campo es obligatorio'
        continue
      }
      if (!valor) continue
      if (campo.type === 'number' && Number.isNaN(Number(valor))) {
        next[campo.key] = 'Debe ser un valor numérico'
      } else if (campo.type === 'select' && !campo.options.includes(valor)) {
        next[campo.key] = 'Valor inválido'
      }
    }

    setErrors(next)
    return Object.keys(next).length === 0
  }

  const handleSubmit = async () => {
    if (!producto || !validar()) return
    setSubmitting(true)
    setError('')
    try {
      const result = await ventasProductoService.crear({
        productoId: producto.id,
        dni: dni.trim(),
        nombre: nombre.trim(),
        apellido: apellido.trim(),
        email: email.trim(),
        datosExtra,
      })
      window.location.href = result.initPoint
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al procesar la compra.')
      setSubmitting(false)
    }
  }

  if (loading) return <div className="text-center py-16"><div className="animate-spin w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div></div>
  if (!producto) return <div className="text-center py-16"><p className="text-slate-600">Producto no encontrado.</p><Link to="/" className="btn-primary">Volver al inicio</Link></div>

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold">Comprar</h1>
          <p className="text-lg text-white/90 mb-0">{producto.nombre}</p>
        </div>
      </section>

      <section className="py-16">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex justify-center">
            <div className="w-full max-w-3xl">
              <div className="card rounded-2xl border-slate-200">
                <div className="p-6 md:p-8">
                  {producto.imagenUrl && (
                    <img src={producto.imagenUrl} alt={producto.nombre} className="w-full max-h-80 object-contain rounded-xl mb-6" />
                  )}
                  {producto.descripcion && <p className="text-slate-600 mb-4">{producto.descripcion}</p>}
                  <div className="text-slate-700 mb-6">
                    Precio: <strong>${producto.precio.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</strong>
                  </div>

                  <h4 className="font-bold text-slate-800 mb-4">Datos del comprador</h4>
                  {error && <div className="alert-danger">{error}</div>}

                  <form onSubmit={e => e.preventDefault()}>
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 mb-6">
                      <div className="md:col-span-4">
                        <label className="form-label">Número de documento *</label>
                        <input type="text" maxLength={20} placeholder="12345678" className={cls('dni', 'form-input')} value={dni} onChange={(e) => { setDni(e.target.value); clearFieldError('dni') }} />
                        {errorMsg('dni')}
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Apellido *</label>
                        <input type="text" className={cls('apellido', 'form-input')} value={apellido} onChange={(e) => { setApellido(e.target.value); clearFieldError('apellido') }} />
                        {errorMsg('apellido')}
                      </div>
                      <div className="md:col-span-4">
                        <label className="form-label">Nombres *</label>
                        <input type="text" className={cls('nombre', 'form-input')} value={nombre} onChange={(e) => { setNombre(e.target.value); clearFieldError('nombre') }} />
                        {errorMsg('nombre')}
                      </div>
                      <div className="md:col-span-12">
                        <label className="form-label">Email *</label>
                        <input type="email" className={cls('email', 'form-input')} value={email} onChange={(e) => { setEmail(e.target.value); clearFieldError('email') }} />
                        {errorMsg('email')}
                      </div>

                      {producto.camposExtra.map(campo => (
                        <div className="md:col-span-6" key={campo.key}>
                          <label className="form-label">{campo.label}{campo.required ? ' *' : ''}</label>
                          {campo.type === 'select' ? (
                            <select className={cls(campo.key, 'form-select')} value={datosExtra[campo.key] ?? ''} onChange={(e) => setExtra(campo.key, e.target.value)}>
                              <option value="">Seleccionar...</option>
                              {campo.options.map(o => (
                                <option key={o} value={o}>{o}</option>
                              ))}
                            </select>
                          ) : (
                            <input
                              type={campo.type === 'number' ? 'number' : 'text'}
                              className={cls(campo.key, 'form-input')}
                              value={datosExtra[campo.key] ?? ''}
                              onChange={(e) => setExtra(campo.key, e.target.value)}
                            />
                          )}
                          {errorMsg(campo.key)}
                        </div>
                      ))}
                    </div>

                    <button type="button" className="btn-primary btn-lg w-full" disabled={submitting} onClick={handleSubmit}>
                      {submitting ? <><span className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block mr-2"></span>Procesando...</> : <><Send className="inline mr-2" size={18} />Comprar</>}
                    </button>
                  </form>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}

export default ComprarProductoPage
