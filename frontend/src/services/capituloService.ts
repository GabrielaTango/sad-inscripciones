import { api } from './api'
import type { DebitoAutomaticoInfo, DebitoAutomaticoFormData } from '../components/DebitoAutomaticoForm'

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
  email?: string
  categoria?: string
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
  exportSociosExcel: async (q?: string) => {
    const token = localStorage.getItem('sad_token')
    const qs = q ? `?q=${encodeURIComponent(q)}` : ''
    const response = await fetch(`/api/capitulo/socios/export${qs}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!response.ok) throw new Error('Error al exportar')
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'socios.xlsx'
    a.click()
    window.URL.revokeObjectURL(url)
  },
  getResumen: (cuit: string) => api.get<ResumenItem[]>(`/capitulo/socios/${encodeURIComponent(cuit)}/resumen`),
  registrarCobro: (payload: RegistrarCobroPayload) => api.post<RegistrarCobroResult>('/capitulo/cobros', payload),
  listarCobros: () => api.get<CobroCapitulo[]>('/capitulo/cobros'),
  getDebitoAutomatico: (cuit: string) =>
    api.get<DebitoAutomaticoInfo>(`/capitulo/socios/${encodeURIComponent(cuit)}/debito-automatico`),
  guardarDebitoAutomatico: (cuit: string, data: DebitoAutomaticoFormData) =>
    api.post<DebitoAutomaticoInfo>(`/capitulo/socios/${encodeURIComponent(cuit)}/debito-automatico`, data),
  darDeBajaDebitoAutomatico: (cuit: string) =>
    api.delete<DebitoAutomaticoInfo>(`/capitulo/socios/${encodeURIComponent(cuit)}/debito-automatico`),
}
