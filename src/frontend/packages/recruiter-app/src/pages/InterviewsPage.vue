<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon name="event" size="100px" color="secondary" />
      <h1 class="text-h4 q-mb-md q-mt-lg">Entretiens</h1>
      <p class="text-subtitle1 text-grey-6">Cette fonctionnalité sera bientôt disponible</p>
      <p class="text-body2 text-grey-5 q-mt-md">
        Cette section permettra de planifier et gérer les entretiens,<br />
        suivre les disponibilités et envoyer des invitations.
      </p>
      <q-btn
        color="secondary"
        label="Voir les candidatures"
        icon="assignment"
        class="q-mt-xl"
        @click="goToApplications"
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

function goToApplications() {
  router.push('/applications');
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
