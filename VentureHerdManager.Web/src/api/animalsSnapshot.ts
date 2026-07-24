import type { AnimalSnapshot } from '../models/AnimalSnapshot'

const API_BASE = import.meta.env.VITE_API_URL

export async function getAnimalSnapshot(animalId: number): Promise<AnimalSnapshot> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}/snapshot`)

  if (!response.ok) {
    throw new Error('Failed to load animal snapshot')
  }

  return await response.json()
}
