import type { Animal } from '../models/Animal'

export function animalDisplayName(animal: Animal): string {
  return animal.barnName?.trim()
    || animal.registeredName?.trim()
    || pedigreeDisplayName(animal.damName, animal.sireName)
    || `Animal #${animal.animalId}`
}

export function pedigreeDisplayName(
  damName: string | null | undefined,
  sireName: string | null | undefined
): string {
  const dam = damName?.trim()
  const sire = sireName?.trim()

  if (!dam && !sire) return ''
  return `${dam || 'Unknown dam'} × ${sire || 'Unknown sire'}`
}

export function animalSearchText(animal: Animal): string {
  return [
    animalDisplayName(animal),
    animal.barnName,
    animal.registeredName,
    animal.registrationNumber,
    animal.damName,
    animal.sireName,
    animal.breed
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()
}
