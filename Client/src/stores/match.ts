import { computed, ref, shallowRef } from 'vue'
import { defineStore } from 'pinia'
import type { LevelListPosition, MatchInit, MatchStatistic, MatchState } from '@/types/contract'

/** Снимки мира приходят много раз в секунду, глубокая реактивность не нужна. */
export const useMatchStore = defineStore('match', () => {
  const init = shallowRef<MatchInit | null>(null)
  const state = shallowRef<MatchState | null>(null)
  const result = shallowRef<MatchStatistic | null>(null)
  /** Очередь списков: уровни могут прийти пачкой до того, как игрок выберет улучшение. */
  const pendingLevelUps = ref<LevelListPosition[][]>([])
  const pendingLevelUpCount = computed(() => pendingLevelUps.value.length)

  function start(i: MatchInit) {
    init.value = i
    state.value = null
    result.value = null
    pendingLevelUps.value = []
  }
  const update = (s: MatchState) => { state.value = s }
  function setResult(value: MatchStatistic) { result.value = value }
  function enqueueLevelUp(positions: LevelListPosition[]) {
    // Копируем массив: SignalR может повторно использовать объект/буфер на своей стороне.
    pendingLevelUps.value = [...pendingLevelUps.value, [...positions]]
  }
  function consumeLevelUp() {
    if (pendingLevelUps.value.length > 0) {
      pendingLevelUps.value = pendingLevelUps.value.slice(1)
    }
  }
  function clear() {
    init.value = null
    state.value = null
    result.value = null
    pendingLevelUps.value = []
  }

  return {
    init, state, result, pendingLevelUps, pendingLevelUpCount,
    start, update, setResult, enqueueLevelUp, consumeLevelUp, clear,
  }
})
