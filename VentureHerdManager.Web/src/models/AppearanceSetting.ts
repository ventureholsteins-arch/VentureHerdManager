export interface AppearanceSetting {
  appearanceSettingId: number
  farmName: string
  logoUrl?: string | null
  backgroundImageUrl?: string | null
  backgroundOpacity: number
  overlayOpacity: number
  theme: string
  accentColor: string
  updatedAt: string
}
