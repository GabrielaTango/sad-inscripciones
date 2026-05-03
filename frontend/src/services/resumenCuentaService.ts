import { api } from './api'
import type { ResumenCuenta } from '../types/models'

export interface GenerarPagoResult {
  preferenceId: string
  initPoint: string
  total: number
  externalReference: string
}

export interface PagoCuentaCorriente {
  id: number
  cuit: string
  monto: number
  comprobantes: string
  externalReference: string
  preferenceId?: string
  estadoPago: string
  mpPaymentId?: number
  fechaPago?: string
  createdAt: string
}

export interface ConfirmarPagoResult {
  estadoPago: string
  monto?: number
  message?: string
}

export interface ValidarPendientesResult {
  eliminados: number
  revisados: number
  aprobados: number
  rechazados: number
  pendientes: number
}

export const resumenCuentaService = {
  getByCuit: () => api.get<ResumenCuenta[]>('/resumen-cuenta'),
  getPagos: () => api.get<PagoCuentaCorriente[]>('/resumen-cuenta/pagos'),
  generarPago: (ids: number[]) => api.post<GenerarPagoResult>('/resumen-cuenta/generar-pago', { ids }),
  confirmarPago: (externalReference: string, paymentId?: number) =>
    api.post<ConfirmarPagoResult>('/resumen-cuenta/confirmar-pago', { externalReference, paymentId }),
  validarPendientes: () => api.post<ValidarPendientesResult>('/resumen-cuenta/validar-pendientes', {}),
  getCount: () => api.get<{ count: number }>('/resumen-cuenta/count'),
}
