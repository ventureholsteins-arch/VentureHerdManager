export interface Animal {
  animalId: number
  barnName: string | null
  registeredName: string | null
  registrationNumber: string | null
  birthDate: string | null
  sex: number
  animalStage: number
  breed: string | null
  sireId: number | null
  sireName: string | null
  damId: number | null
  damName: string | null
  notes: string | null
  animalStatus?: number
}