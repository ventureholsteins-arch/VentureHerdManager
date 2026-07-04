<template>
  <section class="quick-actions">
    <button>Record Heat</button>
    <button>Breed</button>
    <button>Add Note</button>
    <button>Calved</button>
  </section>
</template>

<style scoped>
.quick-actions {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  margin-bottom: 18px;
}

.quick-actions button {
  min-height: 58px;
  border-radius: 18px;
  background: #142033;
  color: white;
  font-size: 16px;
  font-weight: 800;
  border: none;
}
</style>