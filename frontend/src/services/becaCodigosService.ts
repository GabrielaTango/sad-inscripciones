import { api } from './api'
import type { BecaCodigo } from '../types/models'

const BASE = '/becacodigos'

export const becaCodigosService = {
  getAll: () => api.get<BecaCodigo[]>(BASE),
  getByBecaEventoId: (becaEventoId: number) => api.get<BecaCodigo[]>(`${BASE}?becaEventoId=${becaEventoId}`),
  getById: (id: number) => api.get<BecaCodigo>(`${BASE}/${id}`),
  validarCodigo: (codigo: string) =>
    api.get<{ valido: boolean; becaEventoId: number; eventoId: number; tipoDescuento: string; valor: number }>(`${BASE}/validar/${codigo}`),
  exportExcel: async (becaEventoId: number) => {
    const token = localStorage.getItem('sad_token')
    const response = await fetch(`/api${BASE}/export?becaEventoId=${becaEventoId}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!response.ok) throw new Error('Error al exportar')
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `BecaCodigos_${becaEventoId}.xlsx`
    a.click()
    window.URL.revokeObjectURL(url)
  },
}
