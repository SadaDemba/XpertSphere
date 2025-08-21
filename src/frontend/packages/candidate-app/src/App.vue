<template>
  <div id="app">
    <!-- Loading Screen -->
    <div v-if="isInitializing" class="full-screen-loading">
      <div class="flex flex-center full-height">
        <div class="text-center">
          <q-spinner size="50px" color="primary" />
          <p class="q-mt-md text-grey-7">Chargement...</p>
        </div>
      </div>
    </div>

    <!-- Main App -->
    <router-view v-else />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from './stores/authStore';

// Store
const authStore = useAuthStore();
const { isLoading } = storeToRefs(authStore);

// State
const isInitializing = ref(true);

// Initialize application
onMounted(async () => {
  try {
    // Initialize authentication
    await authStore.initialize();
  } catch (error) {
    console.error('Failed to initialize app:', error);
  } finally {
    isInitializing.value = false;
  }
});
</script>

<style>
.full-screen-loading {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: white;
  z-index: 9999;
}

.full-height {
  height: 100vh;
}

/* Global app styles */
#app {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}
</style>
