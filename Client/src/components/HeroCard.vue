<script setup lang="ts">
import { computed } from 'vue'
import type { Hero } from '@/types/contract'
import { heroBackground, heroImage } from '@/utils/assets'
import padUrl from '@/assets/game/lily-pad-round-1.png?url'

const props = defineProps<{ hero: Hero; selected: boolean; locked: boolean }>()
defineEmits<{ select: [] }>()

const img = computed(() => heroImage(props.hero))
const background = computed(() => heroBackground(props.hero))
const formatStat = (value: number) => Number.isInteger(value) ? String(value) : value.toFixed(1)
</script>

<template>
  <button
    type="button"
    class="card"
    :class="{ selected, locked }"
    :aria-pressed="selected"
    :disabled="locked"
    @click="$emit('select')"
  >
    <header class="bar">
      <span class="role">Лягушка</span>
      <span class="state">{{ selected ? '✓ Выбрана' : 'Выбрать +' }}</span>
    </header>

    <div class="art" :style="{ background }">
      <div class="lily-pad" :style="{ backgroundImage: `url(${padUrl})` }" aria-hidden="true"></div>
      <img
        v-if="img"
        class="frog-idle"
        :src="img"
        :alt="hero.name"
        draggable="false"
      />
      <span v-else class="fallback" aria-hidden="true">🐸</span>
    </div>

    <div class="body">
      <h2>{{ hero.name }}</h2>

      <dl class="stats">
        <div><dd>{{ formatStat(hero.jumpDistance) }}</dd><dt>ПРЫЖОК</dt></div>
        <div><dd>{{ formatStat(hero.tongueLength) }}</dd><dt>ЯЗЫК</dt></div>
        <div><dd>{{ formatStat(hero.size) }}</dd><dt>РАЗМЕР</dt></div>
        <div><dd>{{ formatStat(hero.mass) }}</dd><dt>МАССА</dt></div>
        <div><dd>{{ formatStat(hero.speed) }}</dd><dt>СКОРОСТЬ</dt></div>
      </dl>
    </div>
  </button>
</template>

<style scoped>
.card {
  display: flex; flex-direction: column; text-align: left; padding: 0;
  background: var(--card); border: 1px solid var(--line); border-radius: 12px; overflow: hidden;
  transition: border-color .15s, box-shadow .15s;
}
.card:not(.locked):not(.selected):hover { border-color: var(--ink); }
.card.selected { border: 2px solid var(--green); }
.card.locked:not(.selected) { opacity: .55; }
.card.locked { cursor: not-allowed; }

.bar {
  display: flex; justify-content: space-between; padding: 8px 14px;
  font: 500 10px/1 var(--mono); letter-spacing: .06em; text-transform: uppercase;
  color: var(--muted); border-bottom: 1px solid var(--line);
}
.selected .bar { background: #0b0b0b; color: #fff; border-color: #0b0b0b; }

.art { position: relative; aspect-ratio: 1 / .9; display: grid; place-items: center; overflow: hidden; }
.lily-pad {
  position: absolute; z-index: 1; bottom: 4%; left: 50%; width: 68%; aspect-ratio: 1;
  transform: translateX(-50%); background-size: contain; background-position: center;
  background-repeat: no-repeat; filter: drop-shadow(0 8px 8px #23351a33);
}
.frog-idle {
  position: absolute; z-index: 2; bottom: 1%; left: 50%; width: 76%; height: auto;
  transform: translateX(-50%); object-fit: contain; user-select: none; pointer-events: none;
}
.fallback { font-size: 72px; }

.body { padding: 16px 18px 18px; display: flex; flex-direction: column; gap: 12px; flex: 1; }
h2 { margin: 0; font-size: 26px; line-height: 1; font-weight: 700; letter-spacing: -.02em; text-transform: capitalize; }
.stats {
  display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 6px;
  margin: 0; padding: 12px 0 0; border-top: 1px solid var(--line);
}
.stats div { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.stats dd { margin: 0; font-size: 16px; font-weight: 650; font-variant-numeric: tabular-nums; }
.stats dt { font: 500 8px/1 var(--mono); color: var(--muted); letter-spacing: .02em; }

</style>
