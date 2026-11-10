// Render the real Vue templates without a browser or a running game backend.
import assert from 'node:assert/strict'
import { createServer } from 'vite'
import { createSSRApp, h } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { createPinia } from 'pinia'
import { createRouter, createMemoryHistory, RouterView } from 'vue-router'
const server = await createServer({ server: { middlewareMode:true }, appType:'custom' })
try {
  const { useAuthStore } = await server.ssrLoadModule('/src/stores/auth.ts')
  const { useHeroesStore } = await server.ssrLoadModule('/src/stores/heroes.ts')
  const { useLobbyStore } = await server.ssrLoadModule('/src/stores/lobby.ts')
  const pages = ['TitleView','LobbyView','HistoryView','MatchResultView','MatchView']
  for (const [index,page] of pages.entries()) {
    const component = (await server.ssrLoadModule(`/src/views/${page}.vue`)).default
    const pinia = createPinia(), router = createRouter({ history:createMemoryHistory(), routes:[{path:'/',name:page,component,props:{id:'preview'}}] })
    const app = createSSRApp({ render:() => h(RouterView) }).use(pinia).use(router)
    const auth = useAuthStore(pinia); auth.initialized = true
    const heroes = useHeroesStore(pinia)
    heroes.items = ['Common Frog','Heavy Frog','Sticky Frog'].map((name,i)=>({id:String(i),name,jumpDistance:10,tongueLength:8,size:4,mass:1,speed:6}))
    const lobby = useLobbyStore(pinia); lobby.phase = 'lobby'; lobby.lobbyName = 'Тестовый пруд'
    await router.push('/'); await router.isReady()
    const html = await renderToString(app)
    assert.ok(html.includes('<main'), `${page} has no main element`)
    assert.ok(!html.includes('undefined'), `${page} has an undefined binding`)
    if (index === 0) assert.ok(html.includes('Вход в игру') && html.includes('Вход') && html.includes('Регистрация'))
    if (index === 1) {
      assert.equal((html.match(/class="frog-idle"/g)||[]).length,3,'Three actual hero cards should render')
      assert.ok(html.includes('Я готов!'))
    }
    console.log(`PASS: ${page} renders with its actual stores and components`)
  }
} finally { await server.close() }
