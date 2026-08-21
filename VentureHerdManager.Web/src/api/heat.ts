import type { HeatEvent } from '../models/HeatEvent'
import { saveRequest } from './saveRequest'

const API_BASE = import.meta.env.VITE_API_URL

export async function recordHeat(heatData: {
  animalId: number
  heatDateTime: string
  heatStrength: number
  standingHeat: boolean
  pictureUrl?: string
  notes?: string
  hasEmbryoTransfer?: boolean
  embryoImplantDate?: string
}): Promise<HeatEvent> {
  const response = await saveRequest(`${API_BASE}/HeatEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(heatData)
  })

  if (!response.ok) {
    const details = await response.text()
    throw new Error(details || `Failed to record heat (${response.status})`)
  }

  return response.json()
}
