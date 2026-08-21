const SAVE_TIMEOUT_MS = 45_000

/**
 * Writes must never leave a button spinning forever. This deliberately does
 * not retry POST requests automatically: the API's duplicate protection makes
 * a user-requested retry safe without risking a second herd record.
 */
export async function saveRequest(
  url: string,
  init: RequestInit
): Promise<Response> {
  const controller = new AbortController()
  const timeout = window.setTimeout(() => controller.abort(), SAVE_TIMEOUT_MS)

  try {
    return await fetch(url, { ...init, signal: controller.signal })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw new Error(
        'Saving took too long. Check your connection and try once more; the app will prevent a duplicate record.'
      )
    }
    throw new Error(
      'The save did not receive a response. Check your connection and try again; the app will prevent a duplicate record.'
    )
  } finally {
    window.clearTimeout(timeout)
  }
}
