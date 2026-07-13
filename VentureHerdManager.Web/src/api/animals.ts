import type { Animal } from '../models/Animal'

const API_BASE = import.meta.env.VITE_API_URL

export async function getAnimals(): Promise<Animal[]> {
  const response = await fetch(`${API_BASE}/Animals`)

  if (!response.ok) {
    throw new Error('Failed to load animals')
  }

  return await response.json()
}

export async function getAnimal(animalId: number): Promise<Animal> {
  const response = await fetch(`${API_BASE}/Animals/${animalId}`)

  if (!response.ok) {
    throw new Error('Failed to load animal')
  }

  return await response.json()
}