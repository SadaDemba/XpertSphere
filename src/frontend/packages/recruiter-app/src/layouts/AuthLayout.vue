<template>
  <q-layout view="lHh Lpr lFf">
    <q-page-container>
      <main id="auth-content">
        <router-view />
      </main>
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts"></script>

<style scoped>
#auth-content {
  min-height: 100vh;
}

#auth-content:focus {
  outline: none;
}
</style>
