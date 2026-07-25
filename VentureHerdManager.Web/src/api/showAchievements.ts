const API_BASE = import.meta.env.VITE_API_URL

export interface ShowAchievement {
  showAchievementId: number
  animalId: number
  showName: string | null
  showDate: string | null
  placed: string | null
  bagged: string | null
  notes: string | null
  createdAt?: string
}

export interface LatestAchievementDto {
  showAchievementId: number
  animalId: number
  showName: string | null
  showDate: string | null
  placed: string | null
  bagged: string | null
}

export async function getAchievementsForAnimal(animalId: number): Promise<ShowAchievement[]> {
  const response = await fetch(`${API_BASE}/ShowAchievements/animal/${animalId}`)
  if (!response.ok) throw new Error('Failed to load achievements')
  return response.json()
}

export async function getAllAchievements(): Promise<ShowAchievement[]> {
  const response = await fetch(`${API_BASE}/ShowAchievements`)
  if (!response.ok) throw new Error('Failed to load achievements')
  return response.json()
}

export async function getLatestAchievementsPerAnimal(): Promise<LatestAchievementDto[]> {
  const response = await fetch(`${API_BASE}/ShowAchievements/latest-per-animal`)
  if (!response.ok) return []
  return response.json()
}

export async function createAchievement(data: Omit<ShowAchievement, 'showAchievementId' | 'createdAt'>): Promise<ShowAchievement> {
  const response = await fetch(`${API_BASE}/ShowAchievements`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })
  if (!response.ok) throw new Error('Failed to create achievement')
  return response.json()
}

export async function updateAchievement(id: number, data: Partial<ShowAchievement>): Promise<void> {
  const response = await fetch(`${API_BASE}/ShowAchievements/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ...data, showAchievementId: id }),
  })
  if (!response.ok) throw new Error('Failed to update achievement')
}

export async function deleteAchievement(id: number): Promise<void> {
  const response = await fetch(`${API_BASE}/ShowAchievements/${id}`, {
    method: 'DELETE',
  })
  if (!response.ok) throw new Error('Failed to delete achievement')
}
