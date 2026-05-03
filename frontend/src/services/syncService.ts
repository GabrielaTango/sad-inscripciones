import { api } from './api'

export interface SyncTriggerStatus {
  requestedAt: string | null
  requestedBy: string | null
  consumedAt: string | null
}

export const syncService = {
  triggerFullSync: () => api.post<{ requestedAt: string }>('/sync/trigger', {}),
  getTriggerStatus: () => api.get<SyncTriggerStatus>('/sync/trigger/status'),
}
