<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { fetchMatchHistory } from '@/api/history'
import { useAuthStore } from '@/stores/auth'
import type { MatchStatus, UserMatchHistoryDto } from '@/types/contract'
import { errorText } from '@/utils/errors'
import { titleBackground } from '@/utils/assets'

const PAGE_SIZE = 10

const router = useRouter()
const auth = useAuthStore()

const page = ref(1)
const items = ref<UserMatchHistoryDto[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const bg = titleBackground ? { backgroundImage: `url(${titleBackground})` } : {}

// Сервер возвращает только массив без общего количества: если пришла неполная
// страница, значит, дальше матчей нет.
const hasNext = computed(() => items.value.length === PAGE_SIZE)
const hasPrev = computed(() => page.value > 1)

// Если на сервере enum сериализуется числом, подписи зависят от порядка значений
// в C# MatchStatus — при необходимости поправьте таблицу под свой enum.
const STATUS_TEXT: Record<string, string> = {
  '0': 'Создан',
  '1': 'Идёт',
  '2': 'Завершён',
  '3': 'Отменён',
  created: 'Создан',
  inprogress: 'Идёт',
  started: 'Идёт',
  finished: 'Завершён',
  completed: 'Завершён',
  cancelled: 'Отменён',
  canceled: 'Отменён',
  aborted: 'Прерван',
}

function statusText(status: MatchStatus): string {
  return STATUS_TEXT[String(status).toLowerCase()] ?? String(status)
}

const dateFormat = new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' })

function formatDate(iso: string): string {
  // Сервер отдаёт UTC; если суффикс часового пояса потерялся, добавляем его сами.
  const normalized = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`
  const date = new Date(normalized)
  return Number.isNaN(date.getTime()) ? '—' : dateFormat.format(date)
}

function duration(row: UserMatchHistoryDto): string | null {
  const ms = new Date(row.finishedAtUtc).getTime() - new Date(row.startedAtUtc).getTime()
  if (!Number.isFinite(ms) || ms <= 0) return null
  const total = Math.round(ms / 1000)
  return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`
}

function ratingDelta(row: UserMatchHistoryDto): string {
  const delta = row.ratingAfter - row.ratingBefore
  return delta > 0 ? `+${delta}` : String(delta)
}

function deltaClass(row: UserMatchHistoryDto) {
  const delta = row.ratingAfter - row.ratingBefore
  return { up: delta > 0, down: delta < 0 }
}

async function load(targetPage = page.value) {
  const userId = auth.user?.userId
  if (userId == null || loading.value) return
  loading.value = true
  error.value = null
  try {
    items.value = await fetchMatchHistory(userId, targetPage, PAGE_SIZE)
    page.value = targetPage
  } catch (e) {
    error.value = errorText(e)
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await auth.initialize()
  if (!auth.user) {
    await router.replace({ name: 'title', query: { redirect: '/history' } })
    return
  }
  await load(1)
})
</script>

<template>
  <main class="title" :style="bg">
    <section class="panel">
      <header class="head">
        <button class="link" @click="router.push({ name: 'title' })">← Назад</button>
        <h1>История матчей</h1>
        <button class="link" :disabled="loading" @click="load()">Обновить</button>
      </header>

      <p v-if="error" class="note err" role="alert">{{ error }}</p>
      <p v-else-if="loading && !items.length" class="note" role="status">Загружаем…</p>
      <p v-else-if="!items.length" class="empty">
        {{ hasPrev ? 'На этой странице матчей нет.' : 'Вы ещё не сыграли ни одного матча.' }}
      </p>

      <ul v-else class="list" :aria-busy="loading">
        <li v-for="m in items" :key="m.matchId" class="row">
          <div class="place" :class="{ gold: m.place === 1 }" :title="`Место: ${m.place}`">
            {{ m.place }}
          </div>
          <div class="info">
            <strong>
              {{ m.score }} очк.
              <span v-if="m.gameMode" class="mode">· {{ m.gameMode }}</span>
            </strong>
            <span class="meta">
              {{ formatDate(m.startedAtUtc) }}
              <template v-if="duration(m)"> · {{ duration(m) }}</template>
              · {{ statusText(m.status) }}
            </span>
          </div>
          <div class="rating">
            <strong :class="deltaClass(m)">{{ ratingDelta(m) }}</strong>
            <span class="meta">{{ m.ratingBefore }} → {{ m.ratingAfter }}</span>
          </div>
        </li>
      </ul>

      <footer class="pager">
        <button class="join" :disabled="!hasPrev || loading" @click="load(page - 1)">← Новее</button>
        <span class="meta">Страница {{ page }}</span>
        <button class="join" :disabled="!hasNext || loading" @click="load(page + 1)">Старее →</button>
      </footer>
    </section>
  </main>
</template>

<style scoped>
.title {
  min-height: 100vh; display: grid; place-items: center; padding: 32px;
  background:
    radial-gradient(ellipse 60% 22% at 22% 78%, #f3a8bd 0 40%, transparent 41%),
    radial-gradient(ellipse 50% 18% at 80% 70%, #f3a8bd 0 40%, transparent 41%),
    linear-gradient(180deg, #b9e3f4 0%, #9ed3a0 32%, #5fbfb6 55%, #3d9fa8 100%);
  background-size: cover; background-position: center;
}
.panel {
  width: min(640px, 100%); padding: 32px; border-radius: 28px;
  background: rgba(255, 251, 234, .96); border: 1px solid rgba(255, 255, 255, .6);
  box-shadow: 0 20px 60px rgba(15, 61, 43, .25);
  display: flex; flex-direction: column; gap: 16px;
}
.head { display: grid; grid-template-columns: 1fr auto 1fr; align-items: center; gap: 12px; }
.head .link:last-child { justify-self: end; }
h1 { margin: 0; color: var(--ink); font-size: 24px; letter-spacing: -.025em; text-align: center; }
.link { background: none; border: 0; padding: 0; font-size: 13px; color: var(--text); justify-self: start; }
.link:hover:not(:disabled) { text-decoration: underline; }
.link:disabled { opacity: .5; }

.note { margin: 0; color: var(--muted); font-size: 13px; }
.note.err { color: var(--danger); }
.empty { margin: 8px 0; color: var(--muted); text-align: center; }

.list { list-style: none; margin: 0; padding: 0; max-height: 460px; overflow-y: auto; display: flex; flex-direction: column; gap: 8px; }
.list[aria-busy='true'] { opacity: .6; }
.row {
  display: flex; align-items: center; gap: 14px;
  padding: 12px 14px; background: #fff; border: 1px solid var(--line); border-radius: 10px;
}
.place {
  flex: none; width: 38px; height: 38px; border-radius: 50%;
  display: grid; place-items: center; background: #eeeadd; color: var(--ink); font-weight: 800;
}
.place.gold { background: var(--yellow); color: #1a1403; }
.info, .rating { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.info { flex: 1; }
.rating { text-align: right; }
.info strong, .rating strong { font-weight: 600; }
.mode { color: var(--muted); font-weight: 400; }
.meta { color: var(--muted); font-size: 12px; }
.up { color: var(--green); }
.down { color: var(--danger); }

.pager { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.join {
  height: 40px; padding: 0 16px; border: 0; border-radius: 8px;
  background: var(--yellow); color: #1a1403; font-weight: 600; transition: background .15s, opacity .15s;
}
.join:not(:disabled):hover { background: var(--yellow-hover); }
.join:disabled { opacity: .45; }
</style>
