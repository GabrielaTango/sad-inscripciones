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
  medioPago?: string | null
  codigoBarra?: string | null
  fechaVencimiento?: string | null
  razonSoci?: string | null
}

export interface GenerarCuponPagoFacilResult {
  id: number
  externalReference: string
  total: number
  codigoBarra: string
  fechaVencimiento: string
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

export interface ResumenCuentaMeta {
  cuit: string
  codClient?: string | null
  razonSoci?: string | null
}

export const resumenCuentaService = {
  getByCuit: () => api.get<ResumenCuenta[]>('/resumen-cuenta'),
  getMeta: () => api.get<ResumenCuentaMeta>('/resumen-cuenta/meta'),
  getPagos: () => api.get<PagoCuentaCorriente[]>('/resumen-cuenta/pagos'),
  getPago: (id: number) => api.get<PagoCuentaCorriente>(`/resumen-cuenta/pagos/${id}`),
  generarPago: (ids: number[]) => api.post<GenerarPagoResult>('/resumen-cuenta/generar-pago', { ids }),
  generarCuponPagoFacil: (ids: number[]) =>
    api.post<GenerarCuponPagoFacilResult>('/resumen-cuenta/generar-cupon-pagofacil', { ids }),
  confirmarPago: (externalReference: string, paymentId?: number) =>
    api.post<ConfirmarPagoResult>('/resumen-cuenta/confirmar-pago', { externalReference, paymentId }),
  validarPendientes: () => api.post<ValidarPendientesResult>('/resumen-cuenta/validar-pendientes', {}),
  getCount: () => api.get<{ count: number }>('/resumen-cuenta/count'),
}
