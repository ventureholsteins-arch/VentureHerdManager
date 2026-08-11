export interface Animal {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
  birthDate: string | null
  sex: number
  animalStage: number
  currentLactation?: number | null
  breed: string | null
  sireId: number | null
  sireName: string | null
  damId: number | null
  damName: string | null
  notes: string | null
  animalStatus?: number
  herdLocation?: number
  soldDate?: string | null
  soldNotes?: string | null
  latestScore?: number | null
  latestBaa?: number | null
  scoreLabel?: string
  baaLabel?: string
  isFavorite?: boolean
  profilePictureUrl?: string | null
}
