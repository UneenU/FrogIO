<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
import HeroCard from '@/components/HeroCard.vue'
import PlayerSlot from '@/components/PlayerSlot.vue'
import { useHeroesStore } from '@/stores/heroes'
import { useLobbyStore } from '@/stores/lobby'
import { GAME_MODES } from '@/types/contract'
import { errorText } from '@/utils/errors'

const props = defineProps<{ id: string }>()

const route = useRoute()
const router = useRouter()
const lobby = useLobbyStore()
const heroes = useHeroesStore()

const actionError = ref<string | null>(null)
const pending = ref(false)

const slots = computed(() =>
  Array.from({ length: lobby.maxPlayers }, (_, i) => {
    const member = lobby.members[i]
    // Пока никнейм совпадает с userId (заглушка на сервере), показываем «Игрок N».
    const name = member?.nickname && member.nickname !== String(member.userId) ? member.nickname : undefined
    return { index: i + 1, member, name }
  }),
)
const canToggle = computed(
  () => lobby.phase === 'lobby' && !pending.value && (lobby.isReady || !!lobby.heroId),
)

onMounted(async () => {
  void heroes.load()
  // Прямая ссылка или перезагрузка: подключаемся сами. Если пришли с тайтла, уже в лобби.
  if (lobby.phase === 'idle') {
    try {
      await lobby.join(props.id)
    } catch (e) {
      lobby.notice = errorText(e)
      router.replace('/')
    }
  }
})

// По умолчанию выбран первый герой.
watch(
  () => heroes.items,
  (items) => {
    if (!lobby.heroId && items.length) lobby.heroId = items[0].id
  },
  { immediate: true },
)

// Обрыв связи или выход: возвращаемся на тайтл.
watch(
  () => lobby.phase,
  (p) => {
    if (route.name !== 'lobby') return
    if (p === 'idle') router.replace('/')
    if (p === 'match') router.replace({ name: 'match', params: { id: lobby.lobbyId } })
  },
)

onBeforeRouteLeave(() => {
  // Переход на экран матча не считается выходом из лобби.
  if (lobby.phase !== 'match') void lobby.leave()
})

async function act(action: () => Promise<void>) {
  pending.value = true
  actionError.value = null
  try {
    await action()
  } catch (e) {
    actionError.value = errorText(e)
  } finally {
    pending.value = false
  }
}

const toggleReady = () => act(() => lobby.toggleReady())
const startMatch = () => act(() => lobby.startMatch())

const back = () => router.push('/')
</script>

<template>
  <main class="lobby">
    <nav class="crumbs">
      <button class="back" @click="back">← Назад</button>
      <span class="sep">/</span>
      <span class="path">{{ lobby.lobbyName || lobby.lobbyId || props.id }}</span>
    </nav>

    <h1>Выберите лягушку</h1>

    <div class="layout">
      <section class="heroes" aria-label="Герои">
        <div v-if="heroes.failed" class="state">
          <p>Не удалось загрузить героев.</p>
          <button class="btn-line" @click="heroes.load()">Повторить</button>
        </div>
        <p v-else-if="!heroes.items.length" class="state">Загружаем героев…</p>
        <HeroCard
          v-for="h in heroes.items"
          :key="h.id"
          :hero="h"
          :selected="lobby.heroId === h.id"
          :locked="lobby.isReady"
          @select="lobby.heroId = h.id"
        />
      </section>

      <aside class="side">
        <label class="field">
          <span class="label">Режим</span>
          <select v-model="lobby.mode" :disabled="lobby.isReady">
            <option v-for="m in GAME_MODES" :key="m.id" :value="m.id">{{ m.label }}</option>
          </select>
        </label>

        <div>
          <button class="ready" :class="{ on: lobby.isReady }" :disabled="!canToggle" @click="toggleReady">
            {{ lobby.isReady ? 'Отменить готовность' : 'Я готов!' }}
          </button>
          <button
            v-if="lobby.allReady"
            class="start"
            :disabled="pending || lobby.phase !== 'lobby'"
            @click="startMatch"
          >
            Начать матч
          </button>
          <p v-if="actionError" class="err" role="alert">{{ actionError }}</p>
        </div>

        <div>
          <h3>Игроки</h3>
          <ul class="players">
            <PlayerSlot
              v-for="s in slots"
              :key="s.index"
              :index="s.index"
              :empty="!s.member"
              :name="s.name"
              :is-self="s.member?.userId === lobby.selfId"
              :is-ready="s.member?.isReady"
            />
          </ul>
        </div>
      </aside>
    </div>

  </main>
</template>

<style scoped>
.lobby { max-width: 1320px; margin: 0 auto; padding: 32px 32px 64px; }

.crumbs { display: flex; align-items: center; gap: 10px; font-size: 13px; }
.back { background: none; border: 0; padding: 0; color: var(--text); }
.back:hover { text-decoration: underline; }
.sep { color: var(--line); }
.path { font-family: var(--mono); font-size: 12px; color: var(--muted); }

h1 { margin: 20px 0 28px; font-size: 52px; line-height: 1; font-weight: 800; letter-spacing: -.035em; color: var(--ink); }

.layout { display: grid; grid-template-columns: minmax(0, 1fr) 340px; gap: 32px; align-items: start; }
.heroes { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 16px; align-items: stretch; }
.state { grid-column: 1 / -1; color: var(--muted); display: flex; align-items: center; gap: 12px; margin: 0; }

.side {
  position: sticky; top: 24px; display: flex; flex-direction: column; gap: 24px;
  padding: 24px; background: var(--card); border: 1px solid var(--line); border-radius: 14px;
}
.field { display: flex; flex-direction: column; gap: 8px; }
.label, h3 { margin: 0; font-size: 14px; font-weight: 600; }
h3 { margin-bottom: 10px; }
select { height: 44px; padding: 0 12px; background: #fff; border: 1px solid var(--line); border-radius: 8px; }
select:disabled { opacity: .6; }

.ready {
  width: 100%; height: 52px; border: 0; border-radius: 8px; font-weight: 600; font-size: 15px;
  background: var(--green); color: #fff; transition: background .15s, opacity .15s;
}
.ready:not(:disabled):hover { background: var(--green-hover); }
.ready.on { background: var(--yellow); color: #1a1403; }
.ready.on:not(:disabled):hover { background: var(--yellow-hover); }
.ready:disabled { opacity: .45; }
.start {
  width: 100%; height: 52px; margin-top: 8px; border: 0; border-radius: 8px;
  font-weight: 600; font-size: 15px; background: var(--ink); color: #fff;
  transition: opacity .15s;
}
.start:not(:disabled):hover { opacity: .9; }
.start:disabled { opacity: .45; }
.err { margin: 8px 0 0; color: var(--danger); font-size: 13px; }

.players { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }

.btn-line { padding: 8px 14px; background: #fff; border: 1px solid var(--ink); border-radius: 8px; }
.btn-line:hover { background: var(--bg); }

</style>
