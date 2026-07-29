<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { getSharedHerd, type SharedHerdData } from '../api/shareLinks'
import HerdLoadingScene from '../components/HerdLoadingScene.vue'

const route = useRoute()
const loading = ref(true)
const error = ref('')
const data = ref<SharedHerdData | null>(null)

onMounted(async () => {
  try {
    data.value = await getSharedHerd(String(route.params.token ?? ''))
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : 'This link is unavailable.'
  } finally {
    loading.value = false
  }
})

function value(row: Record<string, unknown>, key: string) {
  const result = row[key]
  return result === null || result === undefined || result === '' ? '—' : String(result)
}
</script>

<template>
  <main class="shared-page">
    <header>
      <p>VENTURE HERD MANAGER</p>
      <h1>Shared herd view</h1>
      <span>Private read-only information</span>
    </header>
    <HerdLoadingScene v-if="loading" message="Opening shared herd records..." />
    <section v-else-if="error" class="message">{{ error }}</section>
    <template v-else-if="data">
      <section v-if="data.animals.length" class="card">
        <h2>Animals</h2>
        <article v-for="animal in data.animals" :key="value(animal, 'animalId')" class="record">
          <h3>{{ value(animal, 'name') }}</h3>
          <p>{{ value(animal, 'registeredName') }}</p>
          <div><span>Registration</span><b>{{ value(animal, 'registrationNumber') }}</b></div>
          <div><span>Pedigree</span><b>{{ value(animal, 'sireName') }} × {{ value(animal, 'damName') }}</b></div>
        </article>
      </section>
      <section v-if="data.embryos.length" class="card">
        <h2>Embryos</h2>
        <article v-for="embryo in data.embryos" :key="value(embryo, 'embryoRecordId')" class="record">
          <h3>{{ value(embryo, 'donor') }} × {{ value(embryo, 'sire') }}</h3>
          <div><span>Code / Grade</span><b>{{ value(embryo, 'code') }} · {{ value(embryo, 'grade') }}</b></div>
          <div><span>Implanted</span><b>{{ value(embryo, 'implantDate') }}</b></div>
          <div v-if="embryo.outcome"><span>Outcome</span><b>{{ value(embryo, 'outcome') }}</b></div>
        </article>
      </section>
      <p class="expires">Read-only link · Expires {{ new Date(data.expiresAt).toLocaleDateString() }}</p>
    </template>
  </main>
</template>

<style scoped>
.shared-page{max-width:760px;margin:auto;padding:20px;background:#f4f7f2;min-height:100vh;color:#122419}.shared-page>header{padding:22px;border-radius:14px;background:#10261a;color:#fff}.shared-page>header p{margin:0;color:#8dc29a;font-size:.72rem;font-weight:900;letter-spacing:.16em}.shared-page h1{margin:5px 0}.shared-page>header span{color:#d8e6dc}.card{margin-top:16px;padding:16px;border:1px solid #d2ddd4;border-radius:12px;background:#fff}.card h2{margin:0 0 12px}.record{padding:13px 0;border-top:1px solid #e6ece7}.record:first-of-type{border-top:0}.record h3{margin:0 0 3px}.record p{margin:0 0 8px;color:#647269}.record div{display:flex;justify-content:space-between;gap:15px;margin-top:6px;font-size:.88rem}.record span{color:#647269}.record b{text-align:right}.message{margin-top:20px;padding:18px;background:#fff;border-radius:10px}.expires{text-align:center;color:#68776c;font-size:.8rem}@media(max-width:540px){.shared-page{padding:10px}.record div{align-items:flex-start}.record b{max-width:62%}}
</style>
