<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useLobbyStore } from '@/stores/lobby'
import { useMatchStore } from '@/stores/match'

const router = useRouter()
const lobby = useLobbyStore()
const match = useMatchStore()

type ResultRow = {
  playerId: number
  score: number
  newRating: number | null
}

function finiteNumber(value: unknown): number | null {
  if (typeof value !== 'number' && typeof value !== 'string') return null
  if (typeof value === 'string' && value.trim() === '') return null
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : null
}

function readProperty(obj: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (obj[key] !== undefined && obj[key] !== null) return obj[key]
  }
  return undefined
}

/** Нормализует Dictionary<int, UserStatistic>, принимая и Map, и обычный объект. */
function normalizeUserStatistics(raw: unknown): ResultRow[] {
  let entries: Array<[unknown, unknown]> = []

  if (raw instanceof Map) {
    entries = Array.from(raw.entries())
  } else if (Array.isArray(raw)) {
    // Дополнительная совместимость с сериализаторами, представляющими словарь парами.
    entries = raw.flatMap((entry): Array<[unknown, unknown]> => {
      if (Array.isArray(entry) && entry.length >= 2) return [[entry[0], entry[1]]]
      if (entry && typeof entry === 'object') {
        const pair = entry as Record<string, unknown>
        const key = readProperty(pair, 'key', 'Key')
        const value = readProperty(pair, 'value', 'Value')
        if (key !== undefined && value !== undefined) return [[key, value]]
      }
      return []
    })
  } else if (raw && typeof raw === 'object') {
    entries = Object.entries(raw as Record<string, unknown>)
  }

  return entries.flatMap(([rawId, rawStatistic]): ResultRow[] => {
    const playerId = finiteNumber(rawId)
    if (playerId === null || !rawStatistic || typeof rawStatistic !== 'object') return []

    const statistic = rawStatistic as Record<string, unknown>
    const score = finiteNumber(readProperty(statistic, 'score', 'Score'))
    const newRating = finiteNumber(readProperty(statistic, 'newRating', 'NewRating'))
    if (score === null) return []

    return [{ playerId, score, newRating }]
  })
}

function normalizeScores(raw: unknown): ResultRow[] {
  if (raw instanceof Map) {
    return Array.from(raw.entries()).flatMap(([rawId, rawScore]): ResultRow[] => {
      const playerId = finiteNumber(rawId)
      const score = finiteNumber(rawScore)
      return playerId === null || score === null ? [] : [{ playerId, score, newRating: null }]
    })
  }


  if (raw && typeof raw === 'object' && !Array.isArray(raw)) {
    return Object.entries(raw as Record<string, unknown>).flatMap(([rawId, rawScore]): ResultRow[] => {
      const playerId = finiteNumber(rawId)
      const score = finiteNumber(rawScore)
      return playerId === null || score === null ? [] : [{ playerId, score, newRating: null }]
    })
  }

  if (!Array.isArray(raw)) return []
  return raw.flatMap((item): ResultRow[] => {
    if (!item || typeof item !== 'object') return []
    const row = item as Record<string, unknown>
    const playerId = finiteNumber(readProperty(row, 'playerId', 'PlayerId', 'userId', 'UserId'))
    const score = finiteNumber(readProperty(row, 'score', 'Score'))
    if (playerId === null || score === null) return []
    return [{ playerId, score, newRating: finiteNumber(readProperty(row, 'newRating', 'NewRating')) }]
  })
}

const scores = computed(() => {
  const result = match.result as Record<string, unknown> | null
  const statistics = normalizeUserStatistics(readProperty(result ?? {}, 'userStatistics', 'UserStatistics'))
  const fromResult = normalizeScores(readProperty(result ?? {}, 'scores', 'Scores'))
  const fromState = normalizeScores(match.state?.scores)
  // Новый контракт — UserStatistics; старые поля используются только как запасной вариант.
  const source = statistics.length > 0 ? statistics : fromResult.length > 0 ? fromResult : fromState
  const init = match.init

  return source
    .map((score) => {
      const playerIndex = init?.players.findIndex((p) => p.userId === score.playerId) ?? -1
      const member = lobby.members.find((m) => m.userId === score.playerId)
      const nickname = member?.nickname?.trim()
      return {
        ...score,
        place: 0,
        name: nickname && nickname !== String(score.playerId)
          ? nickname
          : `Игрок ${playerIndex >= 0 ? playerIndex + 1 : score.playerId}`,
        isSelf: score.playerId === lobby.selfId,
      }
    })
    .sort((a, b) => b.score - a.score)
    .map((row, index, array) => ({
      ...row,
      place: index > 0 && row.score === array[index - 1].score ? array[index - 1].place : index + 1,
    }))
})

const topScore = computed(() => scores.value[0]?.score ?? null)
const winners = computed(() => topScore.value == null ? [] : scores.value.filter((row) => row.score === topScore.value))
const headline = computed(() => {
  if (scores.value.length === 0) return 'Матч завершён'
  if (winners.value.length > 1) return 'Ничья!'
  return winners.value[0]?.isSelf ? 'Победа!' : 'Матч завершён'
})
const winnerText = computed(() => {
  if (winners.value.length === 0) return 'Результаты матча'
  if (winners.value.length > 1) return `Лидируют: ${winners.value.map((w) => w.name).join(', ')}`
  return `Победитель: ${winners.value[0]?.name ?? 'не определён'}`
})

onMounted(() => {
  // Результат хранится в памяти соединения. При прямом открытии URL без матча
  // показываем стартовый экран вместо пустой страницы.
  if (!match.result) void router.replace('/')
})

async function returnToMenu() {
  await lobby.leave()
  await router.replace('/')
}
</script>

<template>
  <main class="result-page">
    <div class="result-card">
      <div class="result-mark" aria-hidden="true">
        <svg viewBox="0 0 64 64" fill="none">
          <circle cx="32" cy="32" r="27" stroke="currentColor" stroke-width="2.5" />
          <path d="m18 33 9 9 19-21" stroke="currentColor" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </div>
      <h1>{{ headline }}</h1>
      <p class="subtitle">{{ winnerText }}</p>

      <section class="scoreboard" aria-label="Итоговые очки">
        <div class="scoreboard-head">
          <span>Место</span>
          <span>Игрок</span>
          <span>Очки</span>
          <span title="Рейтинг после матча">Новый рейтинг</span>
        </div>
        <div v-if="scores.length === 0" class="empty-scores">
          Сервер завершил матч, но не прислал таблицу очков.
        </div>
        <div
          v-for="row in scores"
          :key="row.playerId"
          class="score-row"
          :class="{ 'score-row--self': row.isSelf, 'score-row--winner': row.score === topScore }"
        >
          <span class="score-place">{{ row.place === 1 ? '♛' : `#${row.place}` }}</span>
          <span class="score-name">
            {{ row.name }}
            <small v-if="row.isSelf">Вы</small>
          </span>
          <strong class="score-value">{{ new Intl.NumberFormat('ru-RU').format(row.score) }}</strong>
          <strong class="rating-value" :title="row.newRating === null ? 'Рейтинг не передан сервером' : `Рейтинг после матча: ${row.newRating}`">
            {{ row.newRating === null ? '—' : new Intl.NumberFormat('ru-RU').format(row.newRating) }}
          </strong>
        </div>
      </section>

      <button class="return-button" type="button" @click="returnToMenu">
        Вернуться в меню
        <span aria-hidden="true">→</span>
      </button>
    </div>
  </main>
</template>

<style scoped>
.result-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 28px 16px;
  overflow: auto;
  background:
    radial-gradient(ellipse at 50% 0%, rgba(101, 205, 190, .23), transparent 42%),
    linear-gradient(145deg, #0a3442 0%, #104f5b 54%, #092c3b 100%);
  color: #1d3940;
}
.result-card {
  width: min(540px, 100%);
  padding: clamp(22px, 5vw, 40px);
  border: 1px solid rgba(255, 255, 255, .64);
  border-radius: 24px;
  background: rgba(255, 252, 239, .97);
  box-shadow: 0 26px 72px rgba(0, 0, 0, .28);
  text-align: center;
}
.result-mark { width: 58px; height: 58px; margin: 0 auto 12px; color: #198d76; }
.result-mark svg { width: 100%; height: 100%; }
h1 { margin: 0; color: #143d45; font-size: clamp(28px, 6vw, 40px); line-height: 1.1; }
.subtitle { margin: 10px 0 26px; color: #63777a; font-size: 14px; }
.scoreboard { overflow: hidden; border: 1px solid #dce4dc; border-radius: 13px; text-align: left; }
.scoreboard-head, .score-row { display: grid; grid-template-columns: 54px minmax(0, 1fr) 62px 76px; align-items: center; gap: 9px; padding: 12px 13px; }
.scoreboard-head { background: #edf2e9; color: #657575; font-size: 10px; font-weight: 800; letter-spacing: .04em; text-transform: uppercase; }
.scoreboard-head span:nth-last-child(-n + 2) { text-align: right; }
.score-row { border-top: 1px solid #e8ece5; background: rgba(255, 255, 255, .55); font-size: 13px; }
.score-row--winner { background: #fff4cb; }
.score-row--self { box-shadow: inset 3px 0 #21a184; }
.score-place { color: #637778; font: 700 13px/1 ui-monospace, SFMono-Regular, Menlo, monospace; }
.score-row--winner .score-place { color: #b28018; font-size: 20px; }
.score-name { display: flex; align-items: center; gap: 8px; min-width: 0; overflow-wrap: anywhere; font-weight: 650; }
.score-name small { padding: 3px 6px; border-radius: 5px; background: #d9eee5; color: #247760; font-size: 9px; font-weight: 800; }
.score-row strong { color: #173f47; text-align: right; font: 750 13px/1.2 ui-monospace, SFMono-Regular, Menlo, monospace; font-variant-numeric: tabular-nums; }
.score-row .rating-value { color: #167c70; }
.empty-scores { padding: 22px 16px; color: #6c7a79; font-size: 13px; text-align: center; }
.return-button { display: flex; align-items: center; justify-content: center; gap: 12px; width: 100%; min-height: 48px; margin-top: 22px; padding: 12px 18px; border: 0; border-radius: 11px; background: #167c70; color: #fffdf2; cursor: pointer; font: inherit; font-weight: 750; transition: background .16s, transform .16s; }
.return-button:hover { background: #11695f; transform: translateY(-1px); }
.return-button span { font-size: 18px; }
@media (max-width: 440px) {
  .scoreboard-head, .score-row { grid-template-columns: 40px minmax(0, 1fr) 48px 62px; padding: 11px 8px; gap: 6px; }
  .scoreboard-head { font-size: 9px; letter-spacing: 0; }
  .score-row { font-size: 12px; }
  .score-row strong { font-size: 11px; }
}
</style>
