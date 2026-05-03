import { api } from './api'

export interface Vendedor {
  codVended: string
  ctaCaja: number
  ctaTransferencia: number
  ctaCuotas: number
  ctaOtra: number
}

export interface SocioCapitulo {
  cuit: string
  razonSoci: string
  domicilio?: string
  codPostal?: string
  codProvin?: string
  saldo: number
}

export interface ResumenItem {
  id: number
  cuit: string
  tComp: string
  nComp: string
  fechaVto: string
  saldo: number
  pendienteSync: boolean
}

export type MedioPagoCapitulo = 'Contado' | 'Transferencia'

export interface RegistrarCobroPayload {
  cuit: string
  medioPago: MedioPagoCapitulo
  monto: number
  ids?: number[]
}

export interface RegistrarCobroResult {
  id: number
  externalReference: string
  monto: number
}

export interface CobroCapitulo {
  id: number
  cuit: string
  monto: number
  comprobantes?: string | null
  medioPago?: string | null
  externalReference: string
  estadoPago: string
  sincronizadoTango: boolean
  fechaPago?: string
  createdAt: string
}

export const capituloService = {
  me: () => api.get<Vendedor>('/capitulo/me'),
  buscarSocios: (q: string) => api.get<SocioCapitulo[]>(`/capitulo/socios${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  getResumen: (cuit: string) => api.get<ResumenItem[]>(`/capitulo/socios/${encodeURIComponent(cuit)}/resumen`),
  registrarCobro: (payload: RegistrarCobroPayload) => api.post<RegistrarCobroResult>('/capitulo/cobros', payload),
  listarCobros: () => api.get<CobroCapitulo[]>('/capitulo/cobros'),
}
