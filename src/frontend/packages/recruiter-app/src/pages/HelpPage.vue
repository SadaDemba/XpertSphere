<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon name="help" size="100px" color="info" />
      <h1 class="text-h4 q-mb-md q-mt-lg">Aide</h1>
      <p class="text-subtitle1 text-grey-6">Cette fonctionnalité sera bientôt disponible</p>
      <p class="text-body2 text-grey-5 q-mt-md">
        Cette section contiendra la documentation complète,<br />
        les guides d'utilisation et la FAQ.
      </p>
      <q-btn color="info" label="Retour" icon="arrow_back" class="q-mt-xl" @click="goBack" />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { onMounted } from 'vue';
import { useQuasar } from 'quasar';

const router = useRouter();
const $q = useQuasar();

function goBack() {
  router.back();
}

onMounted(() => {
  $q.notify({
    type: 'info',
    message: 'Cette fonctionnalité sera bientôt disponible',
    position: 'top',
    timeout: 3000,
  });
});
</script>
