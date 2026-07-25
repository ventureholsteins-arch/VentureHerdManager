export type CalendarEventType =
  | 'heat'
  | 'breeding'
  | 'pregnancyCheck'
  | 'dueDate'
  | 'calving'
  | 'dryOff'
  | 'lutalyse'
  | 'lutalyseWatch'
  | 'classification'

export interface CalendarEvent {
  eventId: string
  animalId: number
  animalName: string
  eventType: CalendarEventType
  title: string
  eventDate: string
  description?: string | null
}