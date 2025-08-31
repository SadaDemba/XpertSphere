<template>
  <q-layout view="hHh lpr fFf">
    <app-header />

    <q-page-container>
      <main id="main-content">
        <router-view />
      </main>
    </q-page-container>

    <app-footer />
  </q-layout>
</template>

<script setup lang="ts">
import AppHeader from 'components/AppHeader.vue';
import AppFooter from 'components/AppFooter.vue';
</script>

<style scoped>
#main-content {
  min-height: calc(100vh - 64px - 80px); /* Header (64px) + Compact Footer (~80px) */
}

#main-content:focus {
  outline: none;
}

/* Responsive adjustments */
@media (max-width: 599px) {
  #main-content {
    min-height: calc(100vh - 56px - 70px); /* Smaller header (56px) + Mobile footer (~70px) */
  }
}
</style>
