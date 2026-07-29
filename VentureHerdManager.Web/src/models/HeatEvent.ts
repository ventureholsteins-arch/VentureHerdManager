export interface HeatEvent {
  heatEventId: number
  animalId: number
  heatDateTime: string
  heatStrength: number
  standingHeat: boolean
  pictureUrl?: string
  notes?: string
  hasEmbryoTransfer?: boolean
  embryoImplantDate?: string
  createdAt: string
  createdBy?: string
}
