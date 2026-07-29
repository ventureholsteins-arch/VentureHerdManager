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

export interface LatestPregnancyStatus {
  animalId: number
  pregnancyStatus: number
}

export async function getBreedings(animalId: number): Promise<BreedingEvent[]> {
  const response = await fetch(`${API_BASE}/BreedingEvents/animal/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load breedings')
  }

  return await response.json()
}

export async function recordBreeding(breedingData: {
  animalId: number
  breedingDate: string
  sireUsed: string
  breedingType: number
  pregnancyStatus: number
  notes?: string
}): Promise<void> {
  const response = await fetch(`${API_BASE}/BreedingEvents`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      animalId: breedingData.animalId,
      breedingDate: breedingData.breedingDate,
      sireUsed: breedingData.sireUsed,
      breedingType: breedingData.breedingType,
      pregnancyStatus: breedingData.pregnancyStatus,
      notes: breedingData.notes,
      createdBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error(await response.text() || 'Failed to record breeding')
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
    throw new Error(await response.text() || 'Failed to update pregnancy status')
  }
}

export async function updateBreedingEvent(
  breedingEventId: number,
  data: {
    breedingDate: string
    sireUsed: string
    breedingType: number
    pregnancyStatus: number
    notes?: string | null
  }
): Promise<void> {
  const response = await fetch(`${API_BASE}/BreedingEvents/${breedingEventId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      breedingDate: data.breedingDate,
      sireUsed: data.sireUsed,
      breedingType: data.breedingType,
      pregnancyStatus: data.pregnancyStatus,
      notes: data.notes ?? null,
      updatedBy: 'Austin'
    })
  })

  if (!response.ok) {
    throw new Error(await response.text() || 'Failed to update breeding')
  }
}

export async function deleteBreedingEvent(
  breedingEventId: number
): Promise<void> {
  const response = await fetch(`${API_BASE}/BreedingEvents/${breedingEventId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error('Failed to delete breeding')
  }
}

export async function getLatestPregnancyStatuses(): Promise<Record<number, number>> {
  const response = await fetch(`${API_BASE}/BreedingEvents/latest-status`)

  if (!response.ok) {
    throw new Error('Failed to load latest pregnancy statuses')
  }

  const rows: LatestPregnancyStatus[] = await response.json()
  const map: Record<number, number> = {}

  for (const row of rows) {
    map[row.animalId] = row.pregnancyStatus
  }

  return map
}
