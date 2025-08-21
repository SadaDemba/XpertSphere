<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon name="dashboard" size="100px" color="primary" />
      <h1 class="text-h4 q-mb-md q-mt-lg">Tableau de bord</h1>
      <p class="text-subtitle1 text-grey-6">Cette fonctionnalité sera bientôt disponible</p>
      <p class="text-body2 text-grey-5 q-mt-md">
        Le tableau de bord affichera les statistiques de recrutement,<br />
        les métriques de performance et les indicateurs clés.
      </p>
      <q-btn
        color="primary"
        label="Explorer les offres d'emploi"
        icon="work"
        class="q-mt-xl"
        @click="goToJobs"
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

function goToJobs() {
  router.push('/jobs');
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
