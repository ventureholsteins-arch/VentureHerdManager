import type { Animal } from './Animal'

export interface AnimalTimelineEntry {
  eventId: number
  eventType: string
  title: string
  summary: string
  eventDate: string
  notes?: string | null
  photoUrl?: string | null
}

export interface AnimalSnapshot {
  animal: Animal
  latestHeatEvent?: { heatEventId: number; heatDateTime: string } | null
  latestBreedingEvent?: { breedingEventId: number; breedingDate: string } | null
  latestCalvingEvent?: { calvingEventId: number; calvingDate: string } | null
  latestDryOffEvent?: { dryOffEventId: number; dryOffDate: string } | null
  latestClassificationRecord?: { classificationRecordId: number; classificationDate: string } | null
  photos: Array<{
    animalPhotoId: number
    animalId: number
    photoUrl: string
    photoType: number
    photoDate: string
    caption?: string | null
  }>
  timeline: AnimalTimelineEntry[]
}
