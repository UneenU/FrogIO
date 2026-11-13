<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from 'vue'
import { useRouter, onBeforeRouteLeave } from 'vue-router'
import { MatchRenderer } from '@/game/renderer'
import { useLobbyStore } from '@/stores/lobby'
import { useMatchStore } from '@/stores/match'
import type { Input } from '@/types/contract'
import { weatherLabel } from '@/game/weather'
const lobby=useLobbyStore(), match=useMatchStore(), router=useRouter()
const canvas=ref<HTMLCanvasElement|null>(null), zoom=ref(100)
let engine:MatchRenderer|null=null
let lastInput:Input={x:0,y:0,buttons:[],choiceNumber:-1}
const selfId=computed(()=>match.init?.players.find(p=>p.userId===lobby.selfId)?.frogId??null)
const self=computed(()=>match.state?.frogs.find(f=>f.id===selfId.value))
const effects=computed(()=>self.value?.effects??[])
const choices=computed(()=>match.pendingLevelUps[0]??[])
const time=computed(()=>{const n=Math.max(0,Math.ceil(match.state?.timeLeft??0));return `${Math.floor(n/60)}:${String(n%60).padStart(2,'0')}`})
const labels:Record<string,string>={Mass:'Масса',JumpSpeed:'Скорость прыжка',MaxJumpDistance:'Дальность прыжка',SwimSpeed:'Скорость плавания',Hp:'Здоровье',Radius:'Размер',TongueLength:'Длина языка'}
const scores=computed(()=>{
  const raw=match.state?.scores
  const entries=raw instanceof Map?[...raw.entries()]:Object.entries(raw??{})
  return entries.map(([id,value])=>({id:Number(id),score:Number(value),name:lobby.members.find(m=>m.userId===Number(id))?.nickname??`Игрок ${id}`})).sort((a,b)=>b.score-a.score)
})
function upgrade(index:number){if(index<0||index>=choices.value.length)return;lobby.sendInput({...lastInput,buttons:[],choiceNumber:index});match.consumeLevelUp()}
onMounted(()=>{
  if(!canvas.value||!match.init||lobby.phase!=='match'){void router.replace('/');return}
  engine=new MatchRenderer(canvas.value,{onInput:input=>{lastInput=input;lobby.sendInput(input)},onZoom:value=>zoom.value=Math.round(value*100)})
  engine.setInit(match.init);engine.setSelfFrog(selfId.value);if(match.state)engine.setState(match.state);engine.start()
})
watch(()=>match.state,state=>{if(state)engine?.setState(state)})
watch(selfId,id=>engine?.setSelfFrog(id))
watch(()=>lobby.phase,phase=>{if(phase==='idle')void router.replace('/');else if(phase==='result')void router.replace('/result')})
onBeforeRouteLeave(async to=>{if(to.name!=='result')await lobby.leave()})
onBeforeUnmount(()=>{engine?.destroy();engine=null})
</script>

<template>
  <main class="match-screen">
    <canvas ref="canvas" aria-label="Игровое поле" />
    <div class="match-ui match-clock" aria-label="Оставшееся время">{{ time }}</div>
    <div class="match-ui match-weather" role="status" aria-live="polite">{{ weatherLabel(match.state?.weather) }}</div>
    <aside v-if="effects.length" class="match-ui match-effects" aria-label="Активные эффекты">
      <strong>Эффекты</strong><ul><li v-for="(effect,i) in effects" :key="`${i}-${effect}`">{{ effect }}</li></ul>
    </aside>
    <aside class="match-ui match-scores" aria-label="Очки игроков">
      <strong>Очки</strong>
      <ol><li v-for="row in scores" :key="row.id" :class="{self:row.id===lobby.selfId}"><span>{{ row.name }}</span><b>{{ row.score }}</b></li></ol>
      <button @click="lobby.leave()">Выйти</button>
    </aside>
    <button class="match-ui match-zoom" title="Колесо — масштаб; нажмите для сброса" @click="engine?.resetView()">{{ zoom }}%</button>
    <section v-if="choices.length" class="match-ui match-upgrades" aria-label="Выбор улучшения">
      <strong>Новый уровень</strong><p>Выберите улучшение · в очереди {{ match.pendingLevelUpCount }}</p>
      <div><button v-for="(choice,i) in choices" :key="i" @click="upgrade(i)"><span>{{ labels[choice.name]??choice.name }}</span><b>{{ choice.value>=0?'+':'' }}{{ choice.value }}</b></button></div>
    </section>
    <p v-if="!match.state" class="match-ui match-wait" role="status">Ожидаем данные матча…</p>
  </main>
</template>

<style scoped>
.match-screen{position:fixed;inset:0;overflow:hidden;background:#a6dbe9;color:#172523}
canvas{display:block;width:100%;height:100%;touch-action:none;cursor:crosshair}
.match-ui{position:absolute;z-index:1;background:#fffbed;border:2px solid #111b1b;border-radius:13px;box-shadow:3px 4px 0 #111b1b;padding:12px}
.match-clock{top:16px;left:16px;background:#ffdb54;font:900 25px/1 ui-monospace,monospace;font-variant-numeric:tabular-nums}
.match-weather{top:16px;left:125px;font-size:13px;font-weight:800}
.match-effects{left:16px;top:78px;max-width:230px;max-height:30vh;overflow:auto;font-size:12px}.match-effects ul{padding-left:18px;margin-bottom:0}
.match-scores{top:16px;right:16px;width:190px;font-size:12px}.match-scores ol{list-style:none;padding:0;display:grid;gap:4px}.match-scores li{display:flex;justify-content:space-between;gap:10px;padding:6px;border-radius:6px}.match-scores li span{overflow-wrap:anywhere;min-width:0}.self{background:#d8ef9d}
button{font:inherit;font-weight:800;border:2px solid #111b1b;border-radius:8px;padding:7px 12px;background:#f5e7c6;cursor:pointer}button:hover{background:#ffdf63}button:focus-visible{outline:3px solid #1763a8;outline-offset:3px}.match-scores button{width:100%}
.match-zoom{bottom:16px;left:16px;font-size:12px}.match-upgrades{bottom:16px;left:50%;transform:translateX(-50%);width:min(440px,calc(100% - 32px));z-index:2}.match-upgrades p{font-size:12px;color:#53685d;margin:5px 0 10px}.match-upgrades>div{display:flex;gap:8px}.match-upgrades button{flex:1;min-width:0;font-size:12px;background:#e5f5ac;display:grid;gap:8px}.match-wait{left:50%;top:50%;transform:translate(-50%,-50%)}
@media(max-width:520px){.match-clock{left:10px;top:10px;font-size:20px;padding:10px}.match-weather{top:65px;left:10px;padding:7px;font-size:11px}.match-scores{right:10px;top:10px;width:145px;padding:8px;font-size:11px}.match-effects{left:10px;top:110px;max-width:calc(100% - 175px);padding:8px}.match-upgrades{bottom:60px;padding:10px}.match-upgrades>div{flex-direction:column}.match-upgrades button{display:flex;justify-content:space-between;padding:8px}}
</style>
