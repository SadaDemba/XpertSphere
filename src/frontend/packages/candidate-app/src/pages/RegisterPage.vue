<template>
  <q-page class="register-page">
    <div class="register-container q-pa-lg">
      <div class="row justify-center">
        <div class="col-12 col-lg-11 col-xl-10">
          <q-card class="register-card">
            <q-card-section class="text-center q-pb-none">
              <div class="register-header q-mb-lg">
                <h5 class="text-h5 q-mt-md q-mb-sm">Créer un compte candidat</h5>
                <p class="text-grey-7">
                  Rejoignez notre plateforme et trouvez votre prochain emploi
                </p>
              </div>
            </q-card-section>

            <q-card-section>
              <multi-step-register-form @submit="handleRegister" />

              <q-banner v-if="hasError" class="text-white bg-negative q-mt-md rounded-borders">
                <template #avatar>
                  <q-icon name="error" />
                </template>
                {{ error }}
              </q-banner>
            </q-card-section>

            <q-card-section class="text-center">
              <q-separator class="q-mb-md" />
              <p class="text-grey-7 q-mb-sm">Vous avez déjà un compte ?</p>
              <q-btn flat color="primary" label="Se connecter" to="/login" />
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '../stores/authStore';
import { useNotification } from '../composables/notification';
import MultiStepRegisterForm from '../components/register/MultiStepRegisterForm.vue';
import type { RegisterCandidateDto } from '../models/auth';

// Composables
const router = useRouter();
const { showSuccessNotification } = useNotification();

// Store
const authStore = useAuthStore();
const { error, hasError } = storeToRefs(authStore);

// Methods
const handleRegister = async (formData: RegisterCandidateDto, cvFile: File | null) => {
  authStore.clearError();

  console.log('Registering with CV file:', cvFile?.name);

  const success = await authStore.registerCandidate(formData, cvFile || undefined);
  if (success) {
    showSuccessNotification('Compte créé avec succès ! Vous êtes maintenant connecté.');
    // Redirect to job listings
    router.push('/');
  }
};
</script>

<style lang="scss" scoped>
.register-page {
  min-height: calc(100vh - 64px - 80px);
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.register-container {
  padding-top: 40px;
  padding-bottom: 40px;
}

.register-card {
  border-radius: 16px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  background: white;
}

.logo-container {
  display: flex;
  justify-content: center;
}

.register-header {
  color: #2c3e50;
}
</style>
