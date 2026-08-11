import { getAdminKey } from './herdData'
const API_BASE = import.meta.env.VITE_API_URL
const headers = () => ({ 'Content-Type': 'application/json', 'X-Herd-Admin-Key': getAdminKey() })
async function request(path = '', init?: RequestInit) { const response = await fetch(`${API_BASE}/Audit${path}`, { ...init, headers: { ...headers(), ...(init?.headers || {}) } }); if (!response.ok) throw new Error(await response.text() || `Audit request failed (${response.status})`); return response.status === 204 ? null : response.json() }
export const getAudit = () => request()
export const mergeAnimals = (keepAnimalId: number, removeAnimalId: number) => request('/merge', { method: 'POST', body: JSON.stringify({ keepAnimalId, removeAnimalId }) })
export const removeDuplicateEvent = (eventType: string, eventId: number) => request(`/event/${encodeURIComponent(eventType)}/${eventId}`, { method: 'DELETE' })
export const acceptPcdartDifference = (animalId: number, field: 'birthdate' | 'registration' | 'calvingdate') => request('/accept-pcdart', { method: 'POST', body: JSON.stringify({ animalId, field }) })
export const acceptSireSuggestion = (animalId: number) => request(`/accept-sire-suggestion/${animalId}`, { method: 'POST' })
