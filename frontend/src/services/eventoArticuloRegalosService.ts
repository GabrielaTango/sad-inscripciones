import { api } from './api'
import type { EventoArticuloRegalo, EventoArticuloRegaloForm } from '../types/models'

const BASE = '/eventoarticuloregalos'

export const eventoArticuloRegalosService = {
  getAll: () => api.get<EventoArticuloRegalo[]>(BASE),
  getByEventoId: (eventoId: number) => api.get<EventoArticuloRegalo[]>(`${BASE}?eventoId=${eventoId}`),
  getById: (id: number) => api.get<EventoArticuloRegalo>(`${BASE}/${id}`),
  create: (data: EventoArticuloRegaloForm) => api.post<EventoArticuloRegalo>(BASE, data),
  update: (id: number, data: EventoArticuloRegaloForm) => api.put<void>(`${BASE}/${id}`, data),
  remove: (id: number) => api.delete<void>(`${BASE}/${id}`),
}
