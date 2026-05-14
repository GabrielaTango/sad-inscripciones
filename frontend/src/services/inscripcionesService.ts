import { api } from './api'
import type { Inscripcion, InscripcionForm } from '../types/models'

const BASE = '/inscripciones'

export interface InscripcionCreateResult {
  id: number
  precioFinal: number
  initPoint: string | null
  preferenceId?: string
  message?: string
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
  estado: string
  fechaInscripcion: string
  eventoFechaInicio: string
  eventoModalidad: string
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
  generarPago: (id: number, cuotas: number = 1) => api.post<InscripcionCreateResult>(`${BASE}/${id}/generar-pago`, { cuotas }),
  getEstadoPago: (id: number) => api.get<EstadoPagoResult>(`${BASE}/${id}/estado-pago`),
  confirmarPago: (paymentId: number, externalReference?: string) =>
    api.post<ConfirmarPagoResult>(`${BASE}/confirmar-pago`, { paymentId, externalReference }),
  updateEstado: (id: number, estado: string) => api.patch<void>(`${BASE}/${id}/estado`, { estado }),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
  countPendientes: () => api.get<{ count: number }>(`${BASE}/pendientes/count`),
  validarPendientes: () => api.post<ValidarInscripcionesResult>(`${BASE}/validar-pendientes`, {}),
  verificarMP: (id: number) => api.post<ValidacionInscripcion>(`${BASE}/${id}/verificar-mp`, {}),
  exportExcel: async (eventoId?: number) => {
    const token = localStorage.getItem('sad_token')
    const qs = eventoId ? `?eventoId=${eventoId}` : ''
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
