import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getCurrentUser, login, logout, register, type AuthUser, type LoginPayload, type RegisterPayload } from '@/api/auth'
import { ApiError } from '@/api/http'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const initialized = ref(false)
  const initializing = ref(false)
  const initializationError = ref<string | null>(null)
  let initializationPromise: Promise<void> | null = null

  async function initialize() {
    if (initialized.value) return
    if (initializationPromise) return initializationPromise

    initializing.value = true
    initializationPromise = (async () => {
      initializationError.value = null
      try {
        user.value = await getCurrentUser()
      } catch (error) {
        user.value = null
        // 401 означает обычное отсутствие сессии; остальные ошибки показываем отдельно.
        if (!(error instanceof ApiError && (error.status === 401 || error.status === 403))) {
          initializationError.value = 'Не удалось проверить сессию. Проверьте доступность сервера.'
        }
      } finally {
        initialized.value = true
        initializing.value = false
        initializationPromise = null
      }
    })()

    return initializationPromise
  }

  async function signIn(payload: LoginPayload) {
    initializationError.value = null
    user.value = await login(payload)
    initialized.value = true
  }

  async function signUp(payload: RegisterPayload) {
    initializationError.value = null
    user.value = await register(payload)
    initialized.value = true
  }

  async function signOut() {
    try {
      await logout()
    } catch (error) {
      // Если сессия уже истекла, локально всё равно завершаем сеанс.
      if (!(error instanceof ApiError && error.status === 401)) throw error
    }
    user.value = null
    initialized.value = true
  }

  return { user, initialized, initializing, initializationError, initialize, signIn, signUp, signOut }
})
