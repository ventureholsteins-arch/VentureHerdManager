<script setup lang="ts">
import { ref } from 'vue'

const selectedMessage = ref('')

function chooseAnimalFor(
  action: 'heat' | 'breeding' | 'calving' | 'note'
) {
  sessionStorage.setItem('pendingAnimalAction', action)

  selectedMessage.value =
    action === 'heat'
      ? 'Select an animal below to record a heat.'
      : action === 'breeding'
        ? 'Select an animal below to record a breeding.'
        : action === 'calving'
          ? 'Select an animal below to record a calving.'
          : 'Select an animal below to add a note.'

  const herdSection = document.querySelector('.herd-section')
  herdSection?.scrollIntoView({
    behavior: 'smooth',
    block: 'start'
  })

  window.setTimeout(() => {
    const searchInput = document.querySelector<HTMLInputElement>(
      '.herd-section .search input'
    )

    searchInput?.focus()
  }, 500)
}
</script>

<template>
  <section class="quick-action-area">
    <div class="heading">
      <div>
        <p class="eyebrow">QUICK ACTIONS</p>
        <h2>What do you need to record?</h2>
      </div>
    </div>

    <div class="quick-actions">
      <button @click="chooseAnimalFor('heat')">
        <span>❤️</span>

        <div>
          <strong>Record Heat</strong>
          <small>Select an animal</small>
        </div>
      </button>

      <button @click="chooseAnimalFor('breeding')">
        <span>🧬</span>

        <div>
          <strong>Breed</strong>
          <small>Select an animal</small>
        </div>
      </button>

      <button @click="chooseAnimalFor('note')">
        <span>📝</span>

        <div>
          <strong>Add Note</strong>
          <small>Select an animal</small>
        </div>
      </button>

      <button @click="chooseAnimalFor('calving')">
        <span>🐄</span>

        <div>
          <strong>Calved</strong>
          <small>Select an animal</small>
        </div>
      </button>
    </div>

    <p v-if="selectedMessage" class="action-message">
      {{ selectedMessage }}
    </p>
  </section>
</template>

<style scoped>
.quick-action-area {
  margin: 26px 0;
}

.heading {
  margin-bottom: 13px;
}

.heading h2 {
  margin: 4px 0 0;
  color: #142033;
  font-size: 22px;
}

.eyebrow {
  margin: 0;
  color: #31572c;
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.quick-actions {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.quick-actions button {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 78px;
  padding: 14px;
  border: none;
  border-radius: 18px;
  background: #142033;
  color: white;
  text-align: left;
  cursor: pointer;
}

.quick-actions button:hover:not(:disabled) {
  transform: translateY(-1px);
  background: #1d2c42;
}

.quick-actions span {
  display: grid;
  flex-shrink: 0;
  place-items: center;
  width: 42px;
  height: 42px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.12);
  font-size: 21px;
}

.quick-actions strong,
.quick-actions small {
  display: block;
}

.quick-actions strong {
  font-size: 15px;
}

.quick-actions small {
  margin-top: 4px;
  color: #cbd5e1;
  font-size: 12px;
}

.quick-actions .disabled-action {
  background: #64748b;
  cursor: not-allowed;
  opacity: 0.65;
}

.action-message {
  margin: 12px 0 0;
  padding: 12px 14px;
  border-radius: 14px;
  background: #eef4ef;
  color: #31572c;
  font-weight: 700;
}

@media (max-width: 520px) {
  .quick-actions {
    grid-template-columns: 1fr;
  }
}
</style>