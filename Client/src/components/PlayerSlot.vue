<script setup lang="ts">
defineProps<{ index: number; name?: string; isSelf?: boolean; isReady?: boolean; empty?: boolean }>()
</script>

<template>
  <li class="slot" :class="{ empty }">
    <span class="avatar" aria-hidden="true">{{ empty ? '' : index }}</span>
    <span class="name">
      <template v-if="empty">Ожидание игрока…</template>
      <template v-else>{{ name ?? `Игрок ${index}` }}<em v-if="isSelf"> · вы</em></template>
    </span>
    <span v-if="!empty" class="badge" :class="{ ready: isReady }">{{ isReady ? 'Готов' : 'Выбирает' }}</span>
  </li>
</template>

<style scoped>
.slot {
  display: flex; align-items: center; gap: 12px; padding: 10px 14px;
  background: #fff; border: 1px solid var(--line);
}
.slot.empty { background: transparent; border-style: dashed; color: var(--muted); }
.avatar {
  width: 28px; height: 28px; border-radius: 50%; flex: none;
  display: grid; place-items: center; font-weight: 600; font-size: 12px;
  background: var(--yellow);
}
.empty .avatar { background: transparent; border: 1px dashed var(--line); }
.name { flex: 1; }
.name em { font-style: normal; color: var(--muted); }
.badge {
  font: 500 10px/1 var(--mono); padding: 5px 8px; border: 1px solid var(--line); color: var(--muted);
}
.badge.ready { color: var(--green); border-color: var(--green); }
</style>
