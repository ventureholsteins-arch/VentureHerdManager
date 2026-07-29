const API_BASE = import.meta.env.VITE_API_URL

export interface CurrentClassificationDto {
  score?: number
  baa?: number
  label?: string
  classificationDate?: string
}

export interface AddClassificationRequest {
  animalId: number
  score: number
  baa?: number
  classificationDate?: string
  ageInMonthsAtScoring?: number
  classificationLabel?: string
  notes?: string
}

/**
 * Get the latest classification for an animal
 */
export async function getLatestClassification(animalId: number): Promise<CurrentClassificationDto | null> {
  try {
    const response = await fetch(`${API_BASE}/ClassificationRecords/latest/${animalId}`)
    if (!response.ok) {
      if (response.status === 404) {
        return null
      }
      throw new Error('Failed to get classification')
    }
    return await response.json()
  } catch (error: any) {
    if (error.message === 'Failed to get classification' && error.status === 404) {
      return null
    }
    throw error
  }
}

/**
 * Add a new classification (score) for an animal
 */
export async function addClassification(data: AddClassificationRequest): Promise<void> {
  const response = await fetch(`${API_BASE}/ClassificationRecords`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  })

  if (!response.ok) {
    throw new Error('Failed to save classification')
  }
}

/**
 * Get classification history for an animal
 */
export async function getClassificationHistory(animalId: number): Promise<any[]> {
  const response = await fetch(`${API_BASE}/ClassificationRecords/history/${animalId}`)
  if (!response.ok) {
    throw new Error('Failed to get classification history')
  }
  return await response.json()
}
