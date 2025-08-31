<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon name="analytics" size="100px" color="accent" />
      <h1 class="text-h4 q-mb-md q-mt-lg">Rapports</h1>
      <p class="text-subtitle1 text-grey-6">Cette fonctionnalité sera bientôt disponible</p>
      <p class="text-body2 text-grey-5 q-mt-md">
        Les rapports incluront des analyses détaillées,<br />
        des graphiques de performance et des exports de données.
      </p>
      <q-btn
        color="accent"
        label="Voir les candidats"
        icon="people"
        class="q-mt-xl"
        @click="goToCandidates"
      />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { onMounted } from 'vue';
import { useQuasar } from 'quasar';

const router = useRouter();
const $q = useQuasar();

function goToCandidates() {
  router.push('/candidates');
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
