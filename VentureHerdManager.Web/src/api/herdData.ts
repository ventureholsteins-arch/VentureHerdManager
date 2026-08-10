const API_BASE = import.meta.env.VITE_API_URL
const KEY = 'venture-herd-admin-key'

export type HerdDataSource = 1 | 2
export interface HerdDataImportRequest { source: HerdDataSource; fileName: string; csvText: string; reportDate: string; animalMappings: Record<string, number> }
export interface HerdDataPreviewRow { sourceKey: string; sourceName: string; officialId?: string | null; animalId?: number | null; animalName?: string | null; needsConfirmation: boolean; candidates: Array<{ animalId: number; animalName: string; registrationNumber?: string | null }> }
export interface HerdDataPreview { source: HerdDataSource; rowsRead: number; duplicateImport: boolean; rows: HerdDataPreviewRow[] }

export const getAdminKey = () => sessionStorage.getItem(KEY) || ''
export const setAdminKey = (value: string) => sessionStorage.setItem(KEY, value)
const headers = () => ({ 'Content-Type': 'application/json', 'X-Herd-Admin-Key': getAdminKey() })

async function request(path: string, init?: RequestInit) {
  const response = await fetch(`${API_BASE}/HerdData/${path}`, { ...init, headers: { ...headers(), ...(init?.headers || {}) } })
  if (!response.ok) throw new Error(await response.text() || `Request failed (${response.status})`)
  return response.json()
}

export const previewHerdData = (payload: HerdDataImportRequest): Promise<HerdDataPreview> => request('preview', { method: 'POST', body: JSON.stringify(payload) })
export const applyHerdData = (payload: HerdDataImportRequest) => request('apply', { method: 'POST', body: JSON.stringify(payload) })
export const getHerdDataAnalytics = () => request('analytics')
export const getAnimalHerdData = (animalId: number) => request(`animal/${animalId}`)
export const getMatingSuggestions = (animalId: number) => request(`mating/${animalId}`)
