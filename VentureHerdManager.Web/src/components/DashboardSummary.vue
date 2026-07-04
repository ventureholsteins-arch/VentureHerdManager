<template>
  <section class="summary">
    <button class="summary-card">
      <span>❤️</span>
      <div>
        <strong>2</strong>
        <small>Recent heats</small>
      </div>
    </button>

    <button class="summary-card">
      <span>🤰</span>
      <div>
        <strong>0</strong>
        <small>Preg checks</small>
      </div>
    </button>

    <button class="summary-card">
      <span>🧬</span>
      <div>
        <strong>1</strong>
        <small>Breedings</small>
      </div>
    </button>
  </section>
</template>

<style scoped>
.summary {
  display: grid;
  gap: 12px;
  margin-bottom: 16px;
}

.summary-card {
  display: flex;
  align-items: center;
  gap: 14px;
  min-height: 82px;
  padding: 16px;
  border-radius: 22px;
  background: white;
  color: #142033;
  text-align: left;
  box-shadow: 0 10px 28px rgba(15, 23, 42, 0.08);
  border: 1px solid #e2e8f0;
}

.summary-card span {
  width: 46px;
  height: 46px;
  display: grid;
  place-items: center;
  border-radius: 16px;
  background: #eef2ff;
  font-size: 24px;
}

.summary-card strong {
  display: block;
  font-size: 28px;
}

.summary-card small {
  color: #64748b;
  font-size: 15px;
}
</style>