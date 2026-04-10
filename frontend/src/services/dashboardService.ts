import { api } from './api'

export interface InscripcionesPorEstado {
  estado: string
  cantidad: number
}

export interface InscripcionesPorEvento {
  eventoId: number
  titulo: string
  total: number
  confirmadas: number
  pendientes: number
  canceladas: number
}

export interface PagosPorEstado {
  estadoPago: string
  cantidad: number
  monto: number
}

export interface DashboardStats {
  totalEventosActivos: number
  totalInscripciones: number
  inscripcionesHoy: number
  inscripcionesSemana: number
  inscripcionesPorEstado: InscripcionesPorEstado[]
  inscripcionesPorEvento: InscripcionesPorEvento[]
  pagosPorEstado: PagosPorEstado[]
  becasUsadas: number
  becasDisponibles: number
}

export const dashboardService = {
  getStats: () => api.get<DashboardStats>('/dashboard/stats'),
}
