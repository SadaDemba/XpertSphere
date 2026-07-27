<template>
  <q-page class="confirm-email-page flex flex-center">
    <div class="confirm-email-container">
      <q-card class="confirm-email-card q-pa-lg">
        <q-card-section class="text-center">
          <div class="logo-container q-mb-md">
            <app-logo variant="full" size="large" :clickable="false" />
          </div>

          <div v-if="isLoading" class="q-py-lg">
            <q-spinner size="50px" color="primary" />
            <p class="text-grey-7 q-mt-md">Confirmation de votre email en cours...</p>
          </div>

          <div v-else-if="isSuccess">
            <q-icon name="check_circle" color="positive" size="64px" class="q-mb-md" />
            <h5 class="text-h5 q-mt-none q-mb-sm">Email confirmé</h5>
            <p class="text-grey-7 q-mb-lg">
              Votre compte a été activé avec succès. Vous pouvez maintenant vous connecter.
            </p>
            <q-btn color="primary" no-caps label="Se connecter" to="/login" />
          </div>

          <div v-else>
            <q-icon name="error" color="negative" size="64px" class="q-mb-md" />
            <h5 class="text-h5 q-mt-none q-mb-sm">Échec de la confirmation</h5>
            <p class="text-grey-7 q-mb-lg">
              {{
                errorMessage ||
                'Le lien de confirmation est invalide ou a expiré. Vous pouvez demander un nouvel envoi depuis la page de connexion.'
              }}
            </p>
            <q-btn flat color="primary" no-caps label="Retour à la connexion" to="/login" />
          </div>
        </q-card-section>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { authService } from '../services/authService';
import AppLogo from '../components/AppLogo.vue';

const route = useRoute();

const isLoading = ref(true);
const isSuccess = ref(false);
const errorMessage = ref<string | null>(null);

onMounted(async () => {
  const email = route.query.email as string | undefined;
  const token = route.query.token as string | undefined;

  if (!email || !token) {
    isLoading.value = false;
    isSuccess.value = false;
    errorMessage.value = 'Lien de confirmation invalide : email ou jeton manquant.';
    return;
  }

  try {
    const response = await authService.confirmEmail({ email, token });
    isSuccess.value = response?.isSuccess ?? false;
    if (!isSuccess.value) {
      errorMessage.value = response?.message || null;
    }
  } catch (error) {
    isSuccess.value = false;
    errorMessage.value = error instanceof Error ? error.message : null;
  } finally {
    isLoading.value = false;
  }
});
</script>

<style lang="scss" scoped>
.confirm-email-page {
  min-height: calc(100vh - 64px - 80px);
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 40px 20px;
}

.confirm-email-container {
  width: 100%;
  max-width: 450px;
}

.confirm-email-card {
  border-radius: 16px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
}

.logo-container {
  display: flex;
  justify-content: center;
}
</style>
