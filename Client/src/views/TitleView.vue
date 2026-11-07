<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { createLobby, findLobbies } from '@/api/lobbies'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'
import type { LobbySummary } from '@/types/contract'
import { errorText } from '@/utils/errors'
import { isValidLobbyName, normalizeLobbyName } from '@/utils/lobbyName'
import PondBackdrop from '@/components/PondBackdrop.vue'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const lobby = useLobbyStore()

const name = ref('')
const busy = ref(false)
const error = ref<string | null>(lobby.notice)
lobby.notice = null

/** null — показана форма; массив — показан список найденных лобби. */
const results = ref<LobbySummary[] | null>(null)
const searched = ref('')

const authMode = ref<'login' | 'register'>('login')
const authEmail = ref('')
const authPassword = ref('')
const authNickname = ref('')
const authBusy = ref(false)
const authError = ref<string | null>(null)

function redirectTarget(): string | null {
  const value = route.query.redirect
  // Не допускаем внешние URL в redirect-параметре.
  return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//')
    ? value
    : null
}

async function redirectAfterAuth() {
  const target = redirectTarget()
  // Не перетираем навигацию, если сервер уже восстановил активный матч/результат.
  if (auth.user && target && lobby.phase !== 'match' && lobby.phase !== 'result') {
    await router.replace(target)
  }
}

onMounted(async () => {
  await auth.initialize()
  if (auth.initializationError) authError.value = auth.initializationError

  // Cookie-сессия уже существует: поднимаем hub прямо на тайтл-скрине.
  if (auth.user) {
    try {
      await lobby.connectFromTitle()
    } catch (e) {
      error.value = errorText(e)
    }
  }
  await redirectAfterAuth()
})

watch(() => lobby.notice, (message) => {
  if (message) error.value = message
})

watch(() => lobby.phase, (phase) => {
  // На тайтле может прийти MatchStarted/MatchEnded при восстановлении состояния
  // после перезагрузки вкладки.
  if (phase === 'match') {
    void router.replace({ name: 'match', params: { id: lobby.lobbyId || 'active' } })
  } else if (phase === 'result') {
    void router.replace({ name: 'result' })
  }
})

function setAuthMode(mode: 'login' | 'register') {
  authMode.value = mode
  authError.value = null
}

async function submitAuth() {
  if (authBusy.value) return
  authBusy.value = true
  authError.value = null
  try {
    const email = authEmail.value.trim()
    if (authMode.value === 'register') {
      await auth.signUp({
        nickname: authNickname.value.trim(),
        email,
        password: authPassword.value,
      })
    } else {
      await auth.signIn({ email, password: authPassword.value })
    }
    authPassword.value = ''

    try {
      await lobby.connectFromTitle(true)
    } catch (e) {
      error.value = errorText(e)
    }
    await redirectAfterAuth()
  } catch (e) {
    authError.value = errorText(e)
  } finally {
    authBusy.value = false
  }
}

async function signOut() {
  if (authBusy.value) return
  authBusy.value = true
  authError.value = null
  try {
    await lobby.disconnectHub()
    await auth.signOut()
    results.value = null
    name.value = ''
    authPassword.value = ''
  } catch (e) {
    authError.value = errorText(e)
  } finally {
    authBusy.value = false
  }
}

watch(name, () => (error.value = null))

const valid = computed(() => isValidLobbyName(name.value))
const canSubmit = computed(() => valid.value && !busy.value)

async function enter(lobbyId: string, lobbyName: string) {
  await lobby.join(lobbyId, lobbyName)
  // Если сервер восстановил матч при подключении, join() завершится без вступления
  // в лобби, и навигация должна следовать фактической фазе клиента.
  if (lobby.phase === 'match') {
    await router.replace({ name: 'match', params: { id: lobby.lobbyId || 'active' } })
  } else if (lobby.phase === 'result') {
    await router.replace({ name: 'result' })
  } else {
    await router.push({ name: 'lobby', params: { id: lobby.lobbyId } })
  }
}

async function run(action: () => Promise<void>) {
  if (busy.value) return
  busy.value = true
  error.value = null
  try {
    await action()
  } catch (e) {
    error.value = errorText(e)
  } finally {
    busy.value = false
  }
}

function onCreate() {
  if (!canSubmit.value) return
  const n = normalizeLobbyName(name.value)
  void run(async () => {
    const { lobbyId } = await createLobby(n)
    await enter(lobbyId, n)
  })
}

const search = () =>
  run(async () => {
    searched.value = normalizeLobbyName(name.value)
    results.value = await findLobbies(searched.value)
  })

function onFind() {
  if (canSubmit.value) void search()
}

function onConnect(l: LobbySummary) {
  void run(async () => {
    try {
      await enter(l.lobbyId, l.lobbyName)
    } catch (e) {
      // Лобби могло исчезнуть или заполниться: обновляем список и показываем причину.
      results.value = await findLobbies(searched.value).catch(() => results.value)
      throw e
    }
  })
}

function backToForm() {
  results.value = null
  error.value = null
}
</script>

<template>
  <main class="title title-home">
    <div class="home-shell">
    <header class="home-nav"><a class="brand" href="/">FROG<span>.IO</span></a></header>
    <div class="home-grid">
    <section class="home-intro">
      <PondBackdrop />
    </section>
    <section class="panel">
      <h1>{{ auth.user ? 'Лобби' : 'Вход в игру' }}</h1>

      <template v-if="!auth.initialized">
        <p class="note" role="status">Проверяем сессию…</p>
      </template>

      <template v-else-if="!auth.user">
        <div class="auth-tabs" role="tablist" aria-label="Тип авторизации">
          <button type="button" role="tab" :aria-selected="authMode === 'login'" :class="{ active: authMode === 'login' }" @click="setAuthMode('login')">Вход</button>
          <button type="button" role="tab" :aria-selected="authMode === 'register'" :class="{ active: authMode === 'register' }" @click="setAuthMode('register')">Регистрация</button>
        </div>

        <form class="auth-form" @submit.prevent="submitAuth">
          <label v-if="authMode === 'register'" class="field">
            <span class="label">Никнейм</span>
            <input v-model="authNickname" required maxlength="32" autocomplete="nickname" placeholder="Как вас называть" />
          </label>
          <label class="field">
            <span class="label">Email</span>
            <input v-model="authEmail" required type="email" maxlength="256" autocomplete="email" placeholder="you@example.com" />
          </label>
          <label class="field">
            <span class="label">Пароль</span>
            <input v-model="authPassword" required type="password" minlength="8" maxlength="128" :autocomplete="authMode === 'login' ? 'current-password' : 'new-password'" placeholder="Не менее 8 символов" />
          </label>
          <p v-if="authError" class="note err" role="alert">{{ authError }}</p>
          <button class="btn create auth-submit" type="submit" :disabled="authBusy">
            {{ authBusy ? 'Подождите…' : authMode === 'login' ? 'Войти' : 'Создать аккаунт' }}
            <span aria-hidden="true">↗</span>
          </button>
        </form>
      </template>

      <template v-else>
        <div class="user-bar">
          <div class="user-details">
            <strong>{{ auth.user.nickname }}</strong>
            <span>{{ auth.user.email }} · рейтинг {{ auth.user.rating }}</span>
          </div>
          <button class="link" :disabled="authBusy" @click="signOut">Выйти</button>
        </div>

        <p v-if="authError" class="note err" role="alert">{{ authError }}</p>

        <template v-if="!results">
        <label class="field">
          <span class="label">Название лобби</span>
          <input
            v-model="name"
            maxlength="20"
            autocomplete="off"
            spellcheck="false"
            placeholder="например, pond-42"
            :aria-invalid="!!error"
            @keydown.enter="onFind"
          />
        </label>

        <p class="note" :class="{ err: error }" role="status">
          {{ error ?? '3–20 символов: буквы, цифры, - и _' }}
        </p>

        <div class="actions">
          <button class="btn create" :disabled="!canSubmit" @click="onCreate">
            Создать лобби <span aria-hidden="true">↗</span>
          </button>
          <button class="btn find" :disabled="!canSubmit" @click="onFind">
            Найти лобби <span aria-hidden="true">→</span>
          </button>
          <button class="btn history" type="button" @click="router.push({ name: 'history' })">
            История матчей <span aria-hidden="true">🕘</span>
          </button>
        </div>
      </template>

      <template v-else>
        <header class="list-head">
          <button class="link" @click="backToForm">← Назад</button>
          <span class="query">«{{ searched }}»</span>
          <button class="link" :disabled="busy" @click="search">Обновить</button>
        </header>

        <p v-if="error" class="note err" role="alert">{{ error }}</p>

        <p v-if="!results.length" class="empty">
          Лобби не найдено.
        </p>

        <ul v-else class="list">
          <li v-for="l in results" :key="l.lobbyId" class="row">
            <div class="info">
              <strong>{{ l.lobbyName }}</strong>
              <span class="id" :title="l.lobbyId">{{ l.lobbyId }}</span>
            </div>
            <button class="join" :disabled="busy" @click="onConnect(l)">Подключиться</button>
          </li>
        </ul>
        </template>
      </template>
    </section>
    </div>
    </div>
  </main>
</template>

<style scoped>
.title {
  min-height: 100vh; display: grid; place-items: center; padding: 32px;
}

.panel {
  width: min(560px, 100%); padding: 40px; border-radius: 28px;
  background: rgba(255, 251, 234, .96); border: 1px solid rgba(255, 255, 255, .6);
  box-shadow: 0 20px 60px rgba(15, 61, 43, .25);
  display: flex; flex-direction: column; gap: 16px;
}

h1 {
  margin: 0 0 12px; text-align: center; color: var(--ink);
  font-size: 68px; line-height: 1; font-weight: 800; letter-spacing: -.045em;
}


.auth-tabs { display: grid; grid-template-columns: 1fr 1fr; padding: 4px; border-radius: 10px; background: #eeeadd; gap: 4px; }
.auth-tabs button { min-height: 40px; border: 0; border-radius: 7px; background: transparent; color: var(--muted); font-weight: 600; }
.auth-tabs button.active { background: #fff; color: var(--ink); box-shadow: 0 1px 4px rgba(15, 61, 43, .12); }
.auth-form { display: flex; flex-direction: column; gap: 16px; }
.auth-submit { width: 100%; }
.user-bar { display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 12px 14px; background: rgba(255,255,255,.72); border: 1px solid var(--line); border-radius: 10px; }
.user-details { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.user-details strong { color: var(--ink); font-weight: 700; }
.user-details span { color: var(--muted); font-size: 12px; overflow-wrap: anywhere; }
.field { display: flex; flex-direction: column; gap: 8px; }
.label { font-weight: 600; }
input {
  height: 52px; padding: 0 16px; border-radius: 10px; background: #fff;
  border: 1px solid var(--line); font-family: var(--mono); font-size: 14px;
}
input:focus { outline: none; border-color: var(--green); box-shadow: 0 0 0 3px rgba(31, 107, 74, .18); }
input[aria-invalid='true'] { border-color: var(--danger); }

.note { margin: -4px 0 4px; min-height: 1.45em; color: var(--muted); font-size: 13px; }
.note.err { color: var(--danger); }

.actions { display: flex; flex-direction: column; gap: 12px; }
.btn {
  height: 56px; padding: 0 20px; border: 0; border-radius: 10px;
  display: flex; align-items: center; justify-content: space-between;
  font-weight: 600; font-size: 15px; transition: background .15s, opacity .15s;
}
.btn:disabled { opacity: .45; }
.create { background: var(--green); color: #fff; }
.create:not(:disabled):hover { background: var(--green-hover); }
.find { background: var(--yellow); color: #1a1403; }
.find:not(:disabled):hover { background: var(--yellow-hover); }
.history { background: #eeeadd; color: var(--ink); }
.history:hover { background: #e4dfcc; }

/* список лобби */
.list-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.link { background: none; border: 0; padding: 0; font-size: 13px; color: var(--text); }
.link:hover:not(:disabled) { text-decoration: underline; }
.link:disabled { opacity: .5; }
.query { font-family: var(--mono); font-size: 13px; color: var(--muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.empty { margin: 8px 0 0; color: var(--muted); }

.list { list-style: none; margin: 0; padding: 0; max-height: 340px; overflow-y: auto; display: flex; flex-direction: column; gap: 8px; }
.row {
  display: flex; align-items: center; justify-content: space-between; gap: 16px;
  padding: 12px 14px; background: #fff; border: 1px solid var(--line); border-radius: 10px;
}
.info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.info strong { font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.id { font: 400 11px/1.3 var(--mono); color: var(--muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.join {
  flex: none; height: 40px; padding: 0 16px; border: 0; border-radius: 8px;
  background: var(--yellow); color: #1a1403; font-weight: 600; transition: background .15s, opacity .15s;
}
.join:not(:disabled):hover { background: var(--yellow-hover); }
.join:disabled { opacity: .45; }
</style>
