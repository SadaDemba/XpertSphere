<template>
  <q-page class="auth-page flex flex-center">
    <div class="auth-container">
      <div class="auth-card">
        <q-card class="login-card" flat>
          <q-card-section class="text-center q-pb-none">
            <app-logo variant="full" size="large" class="q-mb-md" />
            <h1 class="text-h5 text-weight-medium q-mb-xs">Connexion Recruiter ATS</h1>
            <p class="text-body2 text-grey-7 q-ma-none">
              Connectez-vous à votre espace de recrutement
            </p>
          </q-card-section>

          <q-card-section class="q-pt-md">
            <q-form class="q-gutter-md" @submit="handleLogin">
              <div class="form-group">
                <q-input
                  v-model="loginForm.email"
                  outlined
                  type="email"
                  label="Adresse email"
                  :rules="[
                    (val) => !!val || 'L\'email est obligatoire',
                    (val) => /.+@.+\..+/.test(val) || 'Veuillez saisir un email valide',
                  ]"
                  aria-label="Adresse email de connexion"
                  autocomplete="email"
                  required
                >
                  <template #prepend>
                    <q-icon name="email" />
                  </template>
                </q-input>
              </div>

              <div class="form-group">
                <q-input
                  v-model="loginForm.password"
                  outlined
                  :type="showPassword ? 'text' : 'password'"
                  label="Mot de passe"
                  :rules="[(val) => !!val || 'Le mot de passe est obligatoire']"
                  aria-label="Mot de passe de connexion"
                  autocomplete="current-password"
                  required
                >
                  <template #prepend>
                    <q-icon name="lock" />
                  </template>
                  <template #append>
                    <q-btn
                      flat
                      dense
                      round
                      :icon="showPassword ? 'visibility_off' : 'visibility'"
                      :aria-label="
                        showPassword ? 'Masquer le mot de passe' : 'Afficher le mot de passe'
                      "
                      tabindex="-1"
                      @click="showPassword = !showPassword"
                    />
                  </template>
                </q-input>
              </div>

              <div class="form-options row items-center justify-between">
                <q-checkbox
                  v-model="loginForm.rememberMe"
                  label="Se souvenir de moi"
                  aria-label="Rester connecté sur cet appareil"
                />
                <router-link
                  to="/auth/forgot-password"
                  class="forgot-password-link"
                  aria-label="Mot de passe oublié"
                >
                  Mot de passe oublié ?
                </router-link>
              </div>

              <!-- Error Display -->
              <q-banner
                v-if="authStore.hasError"
                class="bg-negative text-white rounded-borders q-mb-md"
              >
                <template #avatar>
                  <q-icon name="error" />
                </template>
                {{ authStore.error }}
                <template #action>
                  <q-btn flat icon="close" @click="authStore.clearError" />
                </template>
              </q-banner>

              <q-btn
                type="submit"
                color="primary"
                size="lg"
                :loading="authStore.isLoading"
                class="full-width login-button"
                aria-label="Se connecter"
              >
                Se connecter
              </q-btn>
            </q-form>
          </q-card-section>

          <q-separator class="q-my-md" />

          <q-card-section class="text-center">
            <p class="text-body2 text-grey-7 q-mb-sm">Pas encore de compte ?</p>
            <router-link to="/auth/register" class="register-link" aria-label="Créer un compte">
              Créer un compte
            </router-link>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../../stores/authStore';
import AppLogo from '../../components/AppLogo.vue';

interface LoginForm {
  email: string;
  password: string;
  rememberMe: boolean;
}

const router = useRouter();
const authStore = useAuthStore();
const showPassword = ref(false);

const loginForm = ref<LoginForm>({
  email: '',
  password: '',
  rememberMe: false,
});

async function handleLogin() {
  // Clear any previous errors
  authStore.clearError();

  const success = await authStore.login({
    email: loginForm.value.email,
    password: loginForm.value.password,
  });

  if (success) {
    // Check if user is authorized for recruiter app (exclude candidates)
    if (authStore.isInternalUser && !authStore.isCandidate) {
      // Determine redirect based on role
      let redirectTo = router.currentRoute.value.query.redirect as string;

      if (!redirectTo) {
        // Default redirect based on role
        if (authStore.hasAnyRole(['XpertSphere.SuperAdmin', 'XpertSphere.Admin'])) {
          // Platform admins go to admin users page
          redirectTo = '/admin/users';
        } else {
          // Organization users go to job offers
          redirectTo = '/jobs';
        }
      }

      router.push(redirectTo);
    } else {
      // User doesn't have permission for recruiter app
      await authStore.logout();
      authStore.setError("Vous n'avez pas les permissions pour accéder au portail recruteur");
    }
  }
}
</script>

<style scoped>
.auth-page {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  min-height: 100vh;
}

.auth-container {
  width: 100%;
  max-width: 500px;
  padding: 20px;
}

.login-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  padding: 20px;
}

.form-group {
  margin-bottom: 16px;
}

.form-options {
  margin: 16px 0;
}

.forgot-password-link {
  color: var(--q-primary);
  text-decoration: none;
  font-size: 14px;
}

.forgot-password-link:hover {
  text-decoration: underline;
}

.login-button {
  margin-top: 20px;
  height: 48px;
  font-weight: 600;
}

.register-link {
  color: var(--q-primary);
  text-decoration: none;
  font-weight: 500;
}

.register-link:hover {
  text-decoration: underline;
}

@media (max-width: 599px) {
  .auth-container {
    padding: 16px;
  }

  .login-card {
    padding: 16px;
  }
}
</style>
