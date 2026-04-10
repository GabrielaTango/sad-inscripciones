import { api } from './api'
import type { PagoCuentaCorriente } from './resumenCuentaService'

export interface VerificarMPResult {
  encontrado: boolean
  estadoPago?: string
  mpPaymentId?: number
  mpStatus?: string
  message?: string
}

export const pagosCuentaCorrienteService = {
  getAll: () => api.get<PagoCuentaCorriente[]>('/resumen-cuenta/admin/pagos'),
  aprobar: (id: number) => api.post<{ message: string }>(`/resumen-cuenta/admin/pagos/${id}/aprobar`, {}),
  rechazar: (id: number) => api.post<{ message: string }>(`/resumen-cuenta/admin/pagos/${id}/rechazar`, {}),
  verificarMP: (id: number) => api.post<VerificarMPResult>(`/resumen-cuenta/admin/pagos/${id}/verificar-mp`, {}),
}
