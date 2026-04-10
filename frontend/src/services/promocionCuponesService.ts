import { api } from './api'
import type { PromocionCupon } from '../types/models'

export interface PromocionCuponDisponible {
  id: number
  codigo: string
  tipoDescuento: string
  valor: number
  acumulable: boolean
  fechaVencimiento?: string
  promocionNombre: string
}

const BASE = '/promocioncupones'

export const promocionCuponesService = {
  getAll: () => api.get<PromocionCupon[]>(BASE),
  getByPromocionId: (promocionId: number) => api.get<PromocionCupon[]>(`${BASE}?promocionId=${promocionId}`),
  getByDocumento: (documento: string) => api.get<PromocionCupon[]>(`${BASE}?documento=${documento}`),
  getDisponibles: (documento: string) => api.get<PromocionCuponDisponible[]>(`${BASE}/disponibles/${documento}`),
  validarCodigo: (codigo: string) => api.get<{ valido: boolean; promocionId: number; tipoDescuento: string; valor: number; acumulable: boolean; documento: string }>(`${BASE}/validar/${codigo}`),
}
