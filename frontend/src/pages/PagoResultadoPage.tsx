import { useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { CheckCircle, Hourglass, XCircle, Info, CalendarDays, Home, AlertTriangle, BadgeCheck } from 'lucide-react'
import { inscripcionesService } from '../services/inscripcionesService'
import type { LucideIcon } from 'lucide-react'

const statusConfig: Record<string, { icon: LucideIcon; iconClass: string; title: string; message: string }> = {
  approved: {
    icon: CheckCircle,
    iconClass: 'text-green-500',
    title: '¡Pago aprobado!',
    message: 'Tu pago fue procesado correctamente. Tu inscripcion queda confirmada.',
  },
  pending: {
    icon: Hourglass,
    iconClass: 'text-amber-500',
    title: 'Pago pendiente',
    message: 'Tu pago esta siendo procesado. Te notificaremos cuando se confirme. Tu inscripcion queda reservada.',
  },
  rejected: {
    icon: XCircle,
    iconClass: 'text-red-500',
    title: 'Pago rechazado',
    message: 'El pago no pudo ser procesado. Podes intentar nuevamente o contactarnos para asistencia.',
  },
}

const defaultStatus = {
  icon: Info,
  iconClass: 'text-slate-500',
  title: 'Estado del pago',
  message: 'No se pudo determinar el estado del pago. Contactanos si tenes dudas.',
}

const PagoResultadoPage = () => {
  const [searchParams] = useSearchParams()
  const status = searchParams.get('status') || searchParams.get('collection_status') || ''
  const paymentId = searchParams.get('payment_id') || searchParams.get('collection_id')
  const externalReference = searchParams.get('external_reference')

  const [confirmando, setConfirmando] = useState(false)
  const [confirmado, setConfirmado] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!paymentId) return

    const confirmar = async () => {
      setConfirmando(true)
      try {
        await inscripcionesService.confirmarPago(Number(paymentId), externalReference || undefined)
        setConfirmado(true)
      } catch {
        setError('No se pudo confirmar el pago en el sistema. Contactanos con tu numero de inscripcion.')
      } finally {
        setConfirmando(false)
      }
    }
    confirmar()
  }, [paymentId, externalReference])

  const config = statusConfig[status] || defaultStatus

  return (
    <>
      <section className="page-header">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="font-bold">Resultado del Pago</h1>
        </div>
      </section>

      <section className="py-16 md:py-24">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex justify-center">
            <div className="w-full max-w-2xl">
              <div className="text-center py-16">
                {confirmando ? (
                  <>
                    <div className="animate-spin w-16 h-16 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
                    <h3 className="font-bold mt-3 text-slate-800">Confirmando pago...</h3>
                    <p className="text-slate-600">Estamos verificando tu pago con Mercado Pago.</p>
                  </>
                ) : (
                  <>
                    <config.icon className={`w-20 h-20 mx-auto ${config.iconClass}`} />
                    <h3 className="font-bold mt-3 text-slate-800">{config.title}</h3>
                    <p className="text-slate-600 mt-2">{config.message}</p>

                    {confirmado && status === 'approved' && (
                      <div className="alert-success mt-3">
                        <BadgeCheck className="inline-block w-5 h-5 mr-2" />
                        Inscripcion confirmada en el sistema.
                      </div>
                    )}

                    {error && (
                      <div className="alert-warning mt-3">
                        <AlertTriangle className="inline-block w-5 h-5 mr-2" />
                        {error}
                      </div>
                    )}

                    {(paymentId || externalReference) && (
                      <div className="mt-3 p-3 bg-slate-50 rounded-2xl">
                        {externalReference && (
                          <p className="mb-1 text-sm text-slate-600">
                            <strong>Inscripcion #:</strong> {externalReference}
                          </p>
                        )}
                        {paymentId && (
                          <p className="mb-0 text-sm text-slate-600">
                            <strong>Referencia de pago:</strong> {paymentId}
                          </p>
                        )}
                      </div>
                    )}

                    <div className="mt-4 flex gap-3 justify-center">
                      <Link to="/eventos" className="btn-primary">
                        <CalendarDays className="inline-block w-4 h-4 mr-1" />Ver Eventos
                      </Link>
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

export default PagoResultadoPage
