

import { createApp } from 'vue'
import { createPinia } from 'pinia'


import App from './App.vue'
import router from './router'
import './style.css'

const isDemoOnly = import.meta.env.VITE_DEMO_ONLY === 'true'
const apiBase = import.meta.env.VITE_API_URL as string | undefined

if (isDemoOnly && apiBase) {
  const storageKey = 'venture-herd-demo-session-id'
  const legacySessionId = sessionStorage.getItem(storageKey)
  let sessionId = localStorage.getItem(storageKey) || legacySessionId

  if (!sessionId) {
    sessionId = `demo-${crypto.randomUUID().replaceAll('-', '')}`
  }

  // Keep one stable demo session id across tabs so data does not appear to vanish.
  localStorage.setItem(storageKey, sessionId)
  sessionStorage.setItem(storageKey, sessionId)

  const nativeFetch = globalThis.fetch.bind(globalThis)

  globalThis.fetch = (input: RequestInfo | URL, init?: RequestInit) => {
    const requestUrl =
      input instanceof Request
        ? input.url
        : input.toString()

    if (!requestUrl.startsWith(apiBase)) {
      return nativeFetch(input, init)
    }

    const headers = new Headers(
      input instanceof Request ? input.headers : init?.headers
    )
    headers.set('X-Demo-Session', sessionId)

    return nativeFetch(input, {
      ...init,
      headers
    })
  }
}

const app = createApp(App)

app.use(createPinia())
app.use(router)

app.mount('#app')
