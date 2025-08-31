<template>
  <q-page class="auth-page flex flex-center">
    <div class="auth-container">
      <div class="auth-card">
        <q-card class="register-card" flat>
          <q-card-section class="text-center q-pb-none">
            <app-logo variant="full" size="large" class="q-mb-md" />
            <h1 class="text-h5 text-weight-medium q-mb-xs">Créer un compte</h1>
            <p class="text-body2 text-grey-7 q-ma-none">Rejoignez XpertSphere Recruiter ATS</p>
          </q-card-section>

          <q-card-section class="q-pt-md">
            <q-form class="q-gutter-md" @submit="handleRegister">
              <div class="row q-gutter-md">
                <div class="col">
                  <q-input
                    v-model="registerForm.firstName"
                    outlined
                    label="Prénom"
                    :rules="[(val) => !!val || 'Le prénom est obligatoire']"
                    aria-label="Prénom"
                    autocomplete="given-name"
                    required
                  />
                </div>
                <div class="col">
                  <q-input
                    v-model="registerForm.lastName"
                    outlined
                    label="Nom"
                    :rules="[(val) => !!val || 'Le nom est obligatoire']"
                    aria-label="Nom de famille"
                    autocomplete="family-name"
                    required
                  />
                </div>
              </div>

              <div class="form-group">
                <q-input
                  v-model="registerForm.email"
                  outlined
                  type="email"
                  label="Adresse email"
                  :rules="[
                    (val) => !!val || 'L\'email est obligatoire',
                    (val) => /.+@.+\..+/.test(val) || 'Veuillez saisir un email valide',
                  ]"
                  aria-label="Adresse email professionnelle"
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
                  v-model="registerForm.company"
                  outlined
                  label="Entreprise"
                  :rules="[(val) => !!val || 'Le nom de l\'entreprise est obligatoire']"
                  aria-label="Nom de l'entreprise"
                  autocomplete="organization"
                  required
                >
                  <template #prepend>
                    <q-icon name="business" />
                  </template>
                </q-input>
              </div>

              <div class="form-group">
                <q-input
                  v-model="registerForm.jobTitle"
                  outlined
                  label="Fonction"
                  :rules="[(val) => !!val || 'Votre fonction est obligatoire']"
                  aria-label="Votre fonction dans l'entreprise"
                  autocomplete="organization-title"
                  required
                >
                  <template #prepend>
                    <q-icon name="work" />
                  </template>
                </q-input>
              </div>

              <div class="form-group">
                <q-input
                  v-model="registerForm.password"
                  outlined
                  :type="showPassword ? 'text' : 'password'"
                  label="Mot de passe"
                  :rules="[
                    (val) => !!val || 'Le mot de passe est obligatoire',
                    (val) =>
                      val.length >= 8 || 'Le mot de passe doit contenir au moins 8 caractères',
                  ]"
                  aria-label="Mot de passe (minimum 8 caractères)"
                  autocomplete="new-password"
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

              <div class="form-group">
                <q-input
                  v-model="registerForm.confirmPassword"
                  outlined
                  :type="showConfirmPassword ? 'text' : 'password'"
                  label="Confirmer le mot de passe"
                  :rules="[
                    (val) => !!val || 'Veuillez confirmer le mot de passe',
                    (val) =>
                      val === registerForm.password || 'Les mots de passe ne correspondent pas',
                  ]"
                  aria-label="Confirmer le mot de passe"
                  autocomplete="new-password"
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
                      :icon="showConfirmPassword ? 'visibility_off' : 'visibility'"
                      :aria-label="
                        showConfirmPassword ? 'Masquer le mot de passe' : 'Afficher le mot de passe'
                      "
                      tabindex="-1"
                      @click="showConfirmPassword = !showConfirmPassword"
                    />
                  </template>
                </q-input>
              </div>

              <div class="form-group">
                <q-checkbox
                  v-model="registerForm.acceptTerms"
                  :rules="[
                    (val: boolean) => !!val || 'Vous devez accepter les conditions d\'utilisation',
                  ]"
                  aria-label="Accepter les conditions d'utilisation et la politique de confidentialité"
                  required
                >
                  <span class="terms-text">
                    J'accepte les
                    <a href="/terms" target="_blank" class="terms-link">conditions d'utilisation</a>
                    et la
                    <a href="/privacy" target="_blank" class="terms-link"
                      >politique de confidentialité</a
                    >
                  </span>
                </q-checkbox>
              </div>

              <q-btn
                type="submit"
                color="primary"
                size="lg"
                :loading="isLoading"
                class="full-width register-button"
                aria-label="Créer mon compte"
              >
                Créer mon compte
              </q-btn>
            </q-form>
          </q-card-section>

          <q-separator class="q-my-md" />

          <q-card-section class="text-center">
            <p class="text-body2 text-grey-7 q-mb-sm">Vous avez déjà un compte ?</p>
            <router-link to="/auth/login" class="login-link" aria-label="Se connecter">
              Se connecter
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
import AppLogo from '../../components/AppLogo.vue';

interface RegisterForm {
  firstName: string;
  lastName: string;
  email: string;
  company: string;
  jobTitle: string;
  password: string;
  confirmPassword: string;
  acceptTerms: boolean;
}

const router = useRouter();
const isLoading = ref(false);
const showPassword = ref(false);
const showConfirmPassword = ref(false);

const registerForm = ref<RegisterForm>({
  firstName: '',
  lastName: '',
  email: '',
  company: '',
  jobTitle: '',
  password: '',
  confirmPassword: '',
  acceptTerms: false,
});

async function handleRegister() {
  isLoading.value = true;
  try {
    await new Promise((resolve) => setTimeout(resolve, 2000));

    console.log('Registration attempt:', {
      firstName: registerForm.value.firstName,
      lastName: registerForm.value.lastName,
      email: registerForm.value.email,
      company: registerForm.value.company,
      jobTitle: registerForm.value.jobTitle,
    });

    router.push('/auth/login?registered=true');
  } catch (error) {
    console.error('Registration failed:', error);
  } finally {
    isLoading.value = false;
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

.register-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  padding: 20px;
}

.form-group {
  margin-bottom: 16px;
}

.terms-text {
  font-size: 14px;
  line-height: 1.4;
}

.terms-link {
  color: var(--q-primary);
  text-decoration: none;
}

.terms-link:hover {
  text-decoration: underline;
}

.register-button {
  margin-top: 20px;
  height: 48px;
  font-weight: 600;
}

.login-link {
  color: var(--q-primary);
  text-decoration: none;
  font-weight: 500;
}

.login-link:hover {
  text-decoration: underline;
}

@media (max-width: 599px) {
  .auth-container {
    padding: 16px;
  }

  .register-card {
    padding: 16px;
  }

  .row {
    flex-direction: column;
  }
}
</style>
