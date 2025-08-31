<template>
  <q-page class="flex flex-center">
    <div class="text-center">
      <q-icon name="block" size="100px" color="negative" />
      <h4 class="q-mt-lg q-mb-sm">Accès non autorisé</h4>
      <p class="text-grey-6 q-mb-xl">
        Vous n'avez pas les permissions nécessaires pour accéder à cette page.
      </p>
      <q-btn color="primary" :label="backButtonLabel" icon="arrow_back" @click="goToDefaultPage" />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { PlatformRoles } from '../models/auth';

const router = useRouter();
const authStore = useAuthStore();

const backButtonLabel = computed(() => {
  if (authStore.hasAnyRole(PlatformRoles)) {
    return 'Retour à la gestion des utilisateurs';
  } else {
    return "Retour aux offres d'emploi";
  }
});

function goToDefaultPage() {
  // Redirect based on user role
  if (authStore.hasAnyRole(PlatformRoles)) {
    // Platform admins go to admin users page
    router.push('/admin/users');
  } else {
    // Organization users go to job offers
    router.push('/jobs');
  }
}
</script>
