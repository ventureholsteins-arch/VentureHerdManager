const API_BASE = import.meta.env.VITE_API_URL

export interface HeatEvent {
  heatEventId: number
  animalId: number
  heatDateTime: string
  notes?: string | null
  pictureUrl?: string | null
  hasEmbryoTransfer?: boolean
  embryoImplantDate?: string | null
  createdBy?: string | null
}

export interface HeatRecordData {
  animalId: number
  heatDateTime: string
  heatStrength: number
  standingHeat: boolean
  pictureUrl?: string | null
  notes?: string | null
  hasEmbryoTransfer?: boolean
  embryoImplantDate?: string | null
  createdBy?: string | null
}

type LegacyHeatRecordArgs = [
  animalId: number,
  notes: string,
  pictureUrl?: string | null,
  hasEmbryoTransfer?: boolean
]

export async function getHeatEvents(animalId: number): Promise<HeatEvent[]> {
  const response = await fetch(`${API_BASE}/HeatEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load heat events')
  }

  return await response.json()
}

export async function recordHeat(
  ...args: [HeatRecordData] | LegacyHeatRecordArgs
): Promise<void> {
  const heatData =
    typeof args[0] === 'object'
      ? args[0]
      : {
          animalId: args[0],
          heatDateTime: new Date().toISOString(),
          heatStrength: 2,
          standingHeat: false,
          notes: args[1],
          pictureUrl: args[2] ?? null,
          hasEmbryoTransfer: args[3] ?? false,
          embryoImplantDate: args[3]
            ? new Date(
                new Date().getTime() + 7 * 24 * 60 * 60 * 1000
              ).toISOString()
            : null,
          createdBy: 'Austin'
        }

  const response = await fetch(`${API_BASE}/HeatEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId: heatData.animalId,
      heatDateTime: heatData.heatDateTime,
      heatStrength: heatData.heatStrength,
      standingHeat: heatData.standingHeat,
      notes: heatData.notes ?? null,
      pictureUrl: heatData.pictureUrl ?? null,
      hasEmbryoTransfer: heatData.hasEmbryoTransfer ?? false,
      embryoImplantDate: heatData.embryoImplantDate ?? null,
      createdBy: heatData.createdBy ?? 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record heat')
  }
}

export async function updateHeatEvent(
  heatEventId: number,
  data: {
    heatDateTime: string
    notes?: string | null
    pictureUrl?: string | null
  }
): Promise<void> {
  const response = await fetch(`${API_BASE}/HeatEvents/${heatEventId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      heatDateTime: data.heatDateTime,
      notes: data.notes ?? null,
      pictureUrl: data.pictureUrl ?? null,
      updatedBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to update heat')
  }
}

export async function deleteHeatEvent(heatEventId: number): Promise<void> {
  const response = await fetch(`${API_BASE}/HeatEvents/${heatEventId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error('Failed to delete heat')
  }
}