const API_BASE = import.meta.env.VITE_API_URL

export interface BreedingEvent {
  breedingEventId: number
  animalId: number
  breedingDate: string
  sireUsed: string
  breedingType: number
  expectedDueDate: string
  pregnancyCheckDueDate: string
  pregnancyStatus: number
  notes?: string | null
  createdBy?: string | null
}

export async function getBreedings(animalId: number): Promise<BreedingEvent[]> {
  const response = await fetch(`${API_BASE}/BreedingEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load breedings')
  }

  return await response.json()
}

export async function recordBreeding(
  animalId: number,
  sireUsed: string,
  breedingType: number,
  notes: string
): Promise<void> {
  const response = await fetch(`${API_BASE}/BreedingEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId,
      breedingDate: new Date().toISOString(),
      sireUsed,
      breedingType,
      notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error('Failed to record breeding')
  }
}

export async function updatePregnancyStatus(
  breedingEventId: number,
  pregnancyStatus: number
): Promise<void> {
  const response = await fetch(
    `${API_BASE}/BreedingEvents/${breedingEventId}/pregnancy-status`,
    {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(pregnancyStatus)
    }
  )

  if (!response.ok) {
    throw new Error('Failed to update pregnancy status')
  }
}