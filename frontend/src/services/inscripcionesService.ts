import { api } from './api'
import type { Inscripcion, InscripcionForm } from '../types/models'

const BASE = '/inscripciones'

export interface InscripcionCreateResult {
  id: number
  precioFinal: number
  initPoint: string | null
  // 'Pendiente' (hay que elegir como pagarla) o 'Confirmada' (quedo sin costo).
  estado?: string
  preferenceId?: string
  message?: string
  paypal?: boolean
  montoUsd?: number
  moneda?: string
}

export interface ConfirmarPagoPayPalResult {
  inscripcionId: number
  estadoInscripcion: string
  estadoPago: string
  transactionAmount: number
  currency: string
}

export interface EstadoPagoResult {
  id: number
  estado: string
  precioFinal: number
}

export interface ConfirmarPagoResult {
  inscripcionId: number
  estadoPago: string
  estadoInscripcion: string
  transactionAmount: number
}

export interface InscripcionPendiente {
  id: number
  eventoId: number
  eventoTitulo: string
  nombre: string
  apellido: string
  email: string
  documento: string
  precioBase: number
  descuentoAplicado: number
  precioFinal: number
  precioFinalCuotas?: number
  cantidadCuotas?: number
  montoReserva?: number
  // Cuánto costaría reservar la vacante (30% del precio final). Lo calcula el backend.
  montoReservaSugerido: number
  estado: string
  fechaInscripcion: string
  eventoFechaInicio: string
  eventoModalidad: string
  esExtranjero: boolean
  montoUsd?: number | null
}

export const inscripcionesService = {
  getAll: () => api.get<Inscripcion[]>(BASE),
  getByEventoId: (eventoId: number) => api.get<Inscripcion[]>(`${BASE}?eventoId=${eventoId}`),
  getById: (id: number) => api.get<Inscripcion>(`${BASE}/${id}`),
  getPendientes: (documento: string, eventoId?: number) => {
    const params = new URLSearchParams({ documento })
    if (eventoId) params.append('eventoId', String(eventoId))
    return api.get<InscripcionPendiente[]>(`${BASE}/pendientes?${params}`)
  },
  create: (data: InscripcionForm) => api.post<InscripcionCreateResult>(BASE, data),
  // modalidad 'reserva' cobra solo el anticipo del 30%; sin modalidad se cobra el total (o el saldo).
  generarPago: (id: number, cuotas: number = 1, modalidad?: 'reserva') =>
    api.post<InscripcionCreateResult>(`${BASE}/${id}/generar-pago`, { cuotas, modalidad }),
  getEstadoPago: (id: number) => api.get<EstadoPagoResult>(`${BASE}/${id}/estado-pago`),
  confirmarPago: (paymentId: number, externalReference?: string) =>
    api.post<ConfirmarPagoResult>(`${BASE}/confirmar-pago`, { paymentId, externalReference }),
  confirmarPagoPayPal: (inscripcionId: number, orderId: string, monto: number, observaciones?: string) =>
    api.post<ConfirmarPagoPayPalResult>(`${BASE}/confirmar-pago-paypal`, { inscripcionId, orderId, monto, observaciones }),
  updateEstado: (id: number, estado: string) => api.patch<void>(`${BASE}/${id}/estado`, { estado }),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
  countPendientes: () => api.get<{ count: number }>(`${BASE}/pendientes/count`),
  validarPendientes: () => api.post<ValidarInscripcionesResult>(`${BASE}/validar-pendientes`, {}),
  verificarMP: (id: number) => api.post<ValidacionInscripcion>(`${BASE}/${id}/verificar-mp`, {}),
  exportExcel: async (eventoId?: number, estado?: string) => {
    const token = localStorage.getItem('sad_token')
    const params = new URLSearchParams()
    if (eventoId) params.append('eventoId', String(eventoId))
    if (estado) params.append('estado', estado)
    const qs = params.toString() ? `?${params}` : ''
    const response = await fetch(`/api${BASE}/export${qs}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!response.ok) throw new Error('Error al exportar')
    const blob = await response.blob()
    const cd = response.headers.get('content-disposition') || ''
    const match = cd.match(/filename="?([^";]+)"?/i)
    const fechaArchivo = new Date().toISOString().slice(0, 10)
    const filename = match?.[1] ?? `inscripciones_${fechaArchivo}.xlsx`
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    window.URL.revokeObjectURL(url)
  },
}

export interface ValidacionInscripcion {
  inscripcionId: number
  estadoAnterior: string
  estadoNuevo: string
  montoAprobado: number
  pagosEncontrados: number
  pagosNuevos: number
  cambio: boolean
}

export interface ValidarInscripcionesResult {
  revisadas: number
  actualizadas: number
  detalles: ValidacionInscripcion[]
}
