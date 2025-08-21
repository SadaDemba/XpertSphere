<template>
  <q-layout view="lHh Lpr lFf">
    <app-header :drawer-open="leftDrawerOpen" @toggle-drawer="toggleLeftDrawer" />

    <app-navigation v-model="leftDrawerOpen" />

    <q-page-container>
      <main id="main-content">
        <router-view />
      </main>
    </q-page-container>

    <app-footer />
  </q-layout>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import AppHeader from 'components/AppHeader.vue';
import AppNavigation from 'components/AppNavigation.vue';
import AppFooter from 'components/AppFooter.vue';

const leftDrawerOpen = ref(false);

function toggleLeftDrawer() {
  leftDrawerOpen.value = !leftDrawerOpen.value;
}
</script>

<style scoped>
#main-content {
  min-height: calc(100vh - 64px - 160px);
}

#main-content:focus {
  outline: none;
}

/* Responsive adjustments */
@media (max-width: 599px) {
  #main-content {
    min-height: calc(100vh - 56px - 140px);
  }
}
</style>
