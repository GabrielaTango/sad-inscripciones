import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CheckCircle, Hourglass, XCircle, Info, Home, AlertTriangle, RefreshCw } from 'lucide-react'
import { ventasProductoService } from '../services/ventasProductoService'
import type { VentaProductoEstado } from '../types/models'
import type { LucideIcon } from 'lucide-react'

const statusConfig: Record<string, { icon: LucideIcon; iconClass: string; title: string; message: string }> = {
  approved: {
    icon: CheckCircle,
    iconClass: 'text-green-500',
    title: '¡Pago aprobado!',
    message: 'Tu pago fue procesado correctamente. Te enviaremos un mail de confirmación.',
  },
  pending: {
    icon: Hourglass,
    iconClass: 'text-amber-500',
    title: 'Pago pendiente',
    message: 'Tu pago está siendo procesado. Te notificaremos por mail cuando se confirme.',
  },
  rejected: {
    icon: XCircle,
    iconClass: 'text-red-500',
    title: 'Pago rechazado',
    message: 'El pago no pudo ser procesado. Podés volver a intentarlo.',
  },
}

const defaultStatus = {
  icon: Info,
  iconClass: 'text-slate-500',
  title: 'Estado del pago',
  message: 'No se pudo determinar el estado del pago. Contactanos si tenés dudas.',
}

// Extrae el publicRef del external_reference "venta-{id}-{publicRef}".
const extraerPublicRef = (externalReference: string | null): string | null => {
  if (!externalReference || !externalReference.startsWith('venta-')) return null
  const resto = externalReference.slice('venta-'.length)
  const partes = resto.split('-')
  if (partes.length < 2) return null
  return partes.slice(1).join('-')
}

const ProductoPagoResultadoPage = () => {
  const [searchParams] = useSearchParams()
  const status = searchParams.get('status') || searchParams.get('collection_status') || ''
  const externalReference = searchParams.get('external_reference')
  const publicRef = extraerPublicRef(externalReference)

  const [verificando, setVerificando] = useState(false)
  const [venta, setVenta] = useState<VentaProductoEstado | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!publicRef) return

    const verificar = async () => {
      setVerificando(true)
      try {
        const res = await ventasProductoService.verificar(publicRef)
        setVenta(res)
      } catch {
        setError('No se pudo verificar el pago en el sistema. Contactanos con tu referencia de compra.')
      } finally {
        setVerificando(false)
      }
    }
    verificar()
  }, [publicRef])

  // El estado real de la venta manda sobre el "status" que devuelve MP en la URL:
  // si ya está Pagada (por el webhook o por esta misma verificación), lo mostramos como aprobado.
  const estadoEfectivo = venta?.estado === 'Pagada' ? 'approved' : status
  const config = statusConfig[estadoEfectivo] || defaultStatus

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold">Resultado de la Compra</h1>
        </div>
      </section>

      <section className="py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex justify-center">
            <div className="w-full max-w-2xl">
              <div className="text-center py-16">
                {verificando ? (
                  <>
                    <div className="animate-spin w-16 h-16 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
                    <h3 className="font-bold mt-3 text-slate-800">Verificando pago...</h3>
                    <p className="text-slate-600">Estamos verificando tu pago con Mercado Pago.</p>
                  </>
                ) : (
                  <>
                    <config.icon className={`w-20 h-20 mx-auto ${config.iconClass}`} />
                    <h3 className="font-bold mt-3 text-slate-800">{config.title}</h3>
                    <p className="text-slate-600 mt-2">{config.message}</p>

                    {error && (
                      <div className="alert-warning mt-3">
                        <AlertTriangle className="inline-block w-5 h-5 mr-2" />
                        {error}
                      </div>
                    )}

                    {venta && (
                      <div className="mt-3 p-3 bg-slate-50 rounded-2xl">
                        <p className="mb-1 text-sm text-slate-600">
                          <strong>Producto:</strong> {venta.productoNombre}
                        </p>
                        <p className="mb-0 text-sm text-slate-600">
                          <strong>Importe:</strong> ${venta.importe.toLocaleString('es-AR', { minimumFractionDigits: 2 })}
                        </p>
                      </div>
                    )}

                    <div className="mt-4 flex gap-3 justify-center">
                      {estadoEfectivo === 'rejected' && venta?.productoId && (
                        <Link to={`/productos/${venta.productoId}/comprar`} className="btn-primary">
                          <RefreshCw className="inline-block w-4 h-4 mr-1" />Volver a intentar
                        </Link>
                      )}
                      <Link to="/" className="btn-outline">
                        <Home className="inline-block w-4 h-4 mr-1" />Inicio
                      </Link>
                    </div>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  )
}

export default ProductoPagoResultadoPage
