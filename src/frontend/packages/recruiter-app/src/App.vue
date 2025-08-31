<template>
  <router-view />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useAuthStore } from './stores/authStore';

const authStore = useAuthStore();

// Initialize authentication when app starts
onMounted(async () => {
  await authStore.initialize();
});
</script>

<style scoped></style>
