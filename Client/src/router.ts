import { createRouter, createWebHistory } from 'vue-router'
import TitleView from '@/views/TitleView.vue'
import LobbyView from '@/views/LobbyView.vue'
import MatchView from '@/views/MatchView.vue'
import MatchResultView from '@/views/MatchResultView.vue'
import HistoryView from '@/views/HistoryView.vue'
import { useAuthStore } from '@/stores/auth'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'title', component: TitleView },
    { path: '/lobby/:id', name: 'lobby', component: LobbyView, props: true },
    { path: '/match/:id', name: 'match', component: MatchView, props: true },
    { path: '/result', name: 'result', component: MatchResultView },
    { path: '/history', name: 'history', component: HistoryView },
    { path: '/:pathMatch(.*)*', redirect: '/' },
  ],
})

// Игровые экраны доступны только при активной cookie-сессии.
router.beforeEach(async (to) => {
  if (to.name === 'title') return true

  const auth = useAuthStore()
  await auth.initialize()
  if (auth.user) return true

  return { name: 'title', query: { redirect: to.fullPath } }
})
