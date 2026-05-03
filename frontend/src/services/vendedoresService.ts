import { api } from './api'

export interface VendedorAdmin {
  codVended: string
  ctaCaja: number
  ctaTransferencia: number
  ctaCuotas: number
  ctaOtra: number
}

export const vendedoresService = {
  getAll: () => api.get<VendedorAdmin[]>('/vendedores'),
}
