import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { fetchHeroes } from '@/api/heroes'
import type { Hero } from '@/types/contract'

export const useHeroesStore = defineStore('heroes', () => {
  const items = ref<Hero[]>([])
  const loading = ref(false)
  const failed = ref(false)
  const byId = computed(() => new Map(items.value.map((h) => [h.id, h])))

  /** Каталог грузится один раз; при ошибке можно вызвать повторно. */
  async function load() {
    if (loading.value || items.value.length) return
    loading.value = true
    failed.value = false
    try {
      items.value = await fetchHeroes()
    } catch {
      failed.value = true
    } finally {
      loading.value = false
    }
  }

  return { items, loading, failed, byId, load }
})
