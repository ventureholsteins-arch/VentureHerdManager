const API_BASE = import.meta.env.VITE_API_URL

const defaultAppearance: AppearanceSetting = {
  appearanceSettingId: 0,
  farmName: 'Venture Herd Manager',
  logoUrl: '/farm-logo.png',
  backgroundImageUrl: '/herd-manager-bg.jpg',
  backgroundOpacity: 0.15,
  overlayOpacity: 0.85,
  theme: 'light',
  accentColor: '#31572c',
  updatedAt: new Date(0).toISOString()
}

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

export async function getAppearance(): Promise<AppearanceSetting> {
  try {
    const response = await fetch(`${API_BASE}/Appearance`)

    if (!response.ok) {
      // Older API deployments may not expose this endpoint yet.
      if (response.status === 404) {
        return defaultAppearance
      }

      throw new Error(
        `Appearance request failed with status ${response.status}`
      )
    }

    return response.json()
  } catch {
    // Keep the UI functional when the API is unavailable.
    return defaultAppearance
  }
}

export async function updateAppearance(
  appearance: AppearanceSetting
): Promise<AppearanceSetting> {
  const response = await fetch(`${API_BASE}/Appearance`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(appearance)
  })

  if (!response.ok) {
    throw new Error(
      `Appearance update failed with status ${response.status}`
    )
  }

  return response.json()
}
