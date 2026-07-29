import type { HeatEvent } from '../models/HeatEvent'

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
  const response = await fetch('http://localhost:5051/api/HeatEvents', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(heatData)
  })

  if (!response.ok) {
    throw new Error(`Failed to record heat: ${response.statusText}`)
  }

  return response.json()
}
