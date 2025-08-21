<template>
  <q-page class="login-page flex flex-center">
    <div class="login-container">
      <q-card class="login-card q-pa-lg">
        <q-card-section class="text-center">
          <div class="login-header q-mb-lg">
            <div class="logo-container q-mb-md">
              <app-logo variant="full" size="large" :clickable="false" />
            </div>
            <h5 class="text-h5 q-mt-none q-mb-sm">Connexion Candidat</h5>
            <p class="text-grey-7">Connectez-vous pour accéder aux offres d'emploi</p>
          </div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="handleLogin">
            <q-input
              v-model="loginForm.email"
              type="email"
              label="Email"
              filled
              :rules="[(val) => !!val || 'Email requis', validateEmail]"
              :disable="isLoading"
            >
              <template #prepend>
                <q-icon name="email" />
              </template>
            </q-input>

            <q-input
              v-model="loginForm.password"
              :type="showPassword ? 'text' : 'password'"
              label="Mot de passe"
              filled
              :rules="[(val) => !!val || 'Mot de passe requis']"
              :disable="isLoading"
              @keyup.enter="handleLogin"
            >
              <template #prepend>
                <q-icon name="lock" />
              </template>
              <template #append>
                <q-icon
                  :name="showPassword ? 'visibility_off' : 'visibility'"
                  class="cursor-pointer"
                  @click="showPassword = !showPassword"
                />
              </template>
            </q-input>

            <div class="row justify-between items-center">
              <q-checkbox
                v-model="loginForm.rememberMe"
                label="Se souvenir de moi"
                :disable="isLoading"
              />
              <q-btn
                flat
                no-caps
                color="primary"
                label="Mot de passe oublié ?"
                :disable="isLoading"
                @click="forgotPassword"
              />
            </div>

            <q-banner v-if="hasError" class="text-white bg-negative q-mb-md rounded-borders">
              <template #avatar>
                <q-icon name="error" />
              </template>
              {{ error }}
            </q-banner>

            <q-btn
              type="submit"
              color="primary"
              label="Se connecter"
              class="full-width q-mt-lg"
              size="lg"
              :loading="isLoading"
              :disable="isLoading"
            />
          </q-form>
        </q-card-section>

        <q-card-section class="text-center">
          <q-separator class="q-mb-md" />
          <p class="text-grey-7 q-mb-sm">Pas encore de compte ?</p>
          <q-btn
            flat
            color="primary"
            label="Créer un compte candidat"
            :disable="isLoading"
            @click="goToRegister"
          />
        </q-card-section>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '../stores/authStore';
import AppLogo from '../components/AppLogo.vue';
import type { LoginDto } from '../models/auth';

// Router
const router = useRouter();

// Store
const authStore = useAuthStore();
const { isLoading, error, hasError } = storeToRefs(authStore);

// State
const showPassword = ref(false);
const loginForm = reactive<LoginDto>({
  email: '',
  password: '',
  rememberMe: false,
});

// Methods
const validateEmail = (email: string) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email) || 'Email invalide';
};

const handleLogin = async () => {
  authStore.clearError();

  const success = await authStore.login(loginForm);
  if (success) {
    // Redirect to job listings or the intended page
    const redirect = (router.currentRoute.value.query.redirect as string) || '/';
    router.push(redirect);
  }
};

const goToRegister = () => {
  router.push('/register');
};

const forgotPassword = () => {
  // TODO: Implement forgot password functionality
  console.log('Forgot password clicked');
};
</script>

<style lang="scss" scoped>
.login-page {
  min-height: calc(100vh - 64px - 80px);
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 40px 20px;
}

.login-container {
  width: 100%;
  max-width: 450px;
}

.login-card {
  border-radius: 16px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
}

.logo-container {
  display: flex;
  justify-content: center;
}

.login-header {
  color: #2c3e50;
}
</style>
