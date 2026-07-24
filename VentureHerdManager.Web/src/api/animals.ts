import type { Animal } from '../models/Animal'

const API_BASE = import.meta.env.VITE_API_URL

interface LatestClassificationResponse {
  animalId: number
  score?: number
  baa?: number
}

let latestForAnimalsEndpointAvailable: boolean | null = null

export async function getAnimals(): Promise<Animal[]> {
  const response = await fetch(`${API_BASE}/Animals`)

  if (!response.ok) {
    throw new Error('Failed to load animals')
  }

  const animals: Animal[] = await response.json()
  
  // Load latest classifications for all animals
  try {
    if (latestForAnimalsEndpointAvailable === false) {
      return animals
    }

    const animalIds = animals.map(a => a.animalId)
    const classificationsResponse = await fetch(`${API_BASE}/ClassificationRecords/latest-for-animals`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(animalIds)
    })
    
    if (classificationsResponse.ok) {
      latestForAnimalsEndpointAvailable = true
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
    } else if (classificationsResponse.status === 404) {
      latestForAnimalsEndpointAvailable = false
    }
  } catch (error) {
    console.warn('Failed to load classifications:', error)
    // Continue anyway - animals will just not have scores
  }

  return animals
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
  breed?: string | null
  sireName?: string | null
  currentLactation?: number | null
  notes?: string | null
  isFavorite?: boolean
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