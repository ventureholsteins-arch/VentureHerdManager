import type { Animal } from '../models/Animal'

const API_BASE = import.meta.env.VITE_API_URL

interface LatestClassificationResponse {
  animalId: number
  score?: number
  baa?: number
}

export async function getAnimals(): Promise<Animal[]> {
  const response = await fetch(`${API_BASE}/Animals`)

  if (!response.ok) {
    let apiMessage = ''
    try {
      const payload = await response.clone().json() as { message?: string }
      apiMessage = payload?.message?.trim() ?? ''
    } catch {
      try {
        apiMessage = (await response.text()).trim()
      } catch {
        apiMessage = ''
      }
    }

    throw new Error(
      apiMessage || `Failed to load animals (HTTP ${response.status})`
    )
  }

  const animals: Animal[] = await response.json()
  
  // Load latest classifications for all animals
  try {
    const animalIds = animals.map(a => a.animalId)
    const classificationsResponse = await fetch(`${API_BASE}/ClassificationRecords/latest-for-animals`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(animalIds)
    })
    
    if (classificationsResponse.ok) {
      const classifications: LatestClassificationResponse[] = await classificationsResponse.json()
      const classificationMap = new Map(
        classifications.map((c) => [c.animalId, c])
      )
      
      // Attach classifications to animals
      animals.forEach(animal => {
        const classification = classificationMap.get(animal.animalId)
        if (classification) {
          animal.latestScore = classification.score
          animal.latestBaa = classification.baa
        }
      })
    }
  } catch (error) {
    console.warn('Failed to load classifications:', error)
    // Continue anyway - animals will just not have scores
  }

  return animals
}

export async function getAnimalsBasic(): Promise<Animal[]> {
  const response = await fetch(`${API_BASE}/Animals`)
  if (!response.ok) throw new Error('Failed to load animals')
  return response.json()
}

export async function getAnimal(animalId: number): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load animal')
  }

  return await response.json()
}

export interface UpdateAnimalRequest {
  barnName?: string | null
  registeredName?: string | null
  registrationNumber?: string | null
  birthDate?: string | null
  sex?: number
  animalStage?: number
  animalStatus?: number
  breed?: string | null
  sireId?: number | null
  sireName?: string | null
  damId?: number | null
  damName?: string | null
  currentLactation?: number | null
  notes?: string | null
  profilePictureUrl?: string | null
  isFavorite?: boolean
}

export interface CreateAnimalRequest {
  barnName?: string | null
  registeredName?: string | null
  registrationNumber?: string | null
  birthDate?: string | null
  sex: number
  animalStage: number
  animalStatus: number
  breed?: string | null
  sireName?: string | null
  damName?: string | null
  notes?: string | null
  isFavorite?: boolean
}

export async function createAnimal(data: CreateAnimalRequest): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    const errorText = await response.text()
    throw new Error(errorText || 'Failed to create animal')
  }

  return await response.json()
}

export async function updateAnimal(animalId: number, data: UpdateAnimalRequest): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  })

  if (!response.ok) {
    throw new Error('Failed to update animal')
  }

  return await response.json()
}

export async function setAnimalFavorite(
  animalId: number,
  isFavorite: boolean
): Promise<Animal> {
  const response = await fetch(
    `${API_BASE}/Animals/${animalId}/favorite?isFavorite=${isFavorite}`,
    { method: 'PUT' }
  )

  if (!response.ok) {
    throw new Error('Failed to update favorite')
  }

  return await response.json()
}

export async function markAnimalSold(animalId: number, soldDate: string, soldNotes?: string): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}/archive/sold`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ soldDate, soldNotes, updatedBy: 'Animal card' }) })
  if (!response.ok) throw new Error(await response.text() || 'Failed to mark animal sold')
  return response.json()
}

export async function restoreAnimal(animalId: number): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}/restore`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ updatedBy: 'Animal card' }) })
  if (!response.ok) throw new Error(await response.text() || 'Failed to restore animal')
  return response.json()
}
