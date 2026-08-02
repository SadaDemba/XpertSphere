<template>
  <q-page class="auth-page flex flex-center">
    <div class="auth-container">
      <div class="auth-card">
        <q-card class="forgot-password-card" flat>
          <q-card-section class="text-center q-pb-none">
            <app-logo variant="full" size="large" class="q-mb-md" />
            <h1 class="text-h5 text-weight-medium q-mb-xs">Mot de passe oublié</h1>
            <p class="text-body2 text-grey-7 q-ma-none">
              Saisissez votre adresse email pour recevoir un lien de réinitialisation
            </p>
          </q-card-section>

          <q-card-section class="q-pt-md">
            <div v-if="!emailSent">
              <q-form class="q-gutter-md" @submit="handleForgotPassword">
                <div class="form-group">
                  <q-input
                    v-model="email"
                    outlined
                    type="email"
                    label="Adresse email"
                    :rules="[
                      (val) => !!val || 'L\'email est obligatoire',
                      (val) => /.+@.+\..+/.test(val) || 'Veuillez saisir un email valide',
                    ]"
                    aria-label="Adresse email de récupération"
                    autocomplete="email"
                    required
                  >
                    <template #prepend>
                      <q-icon name="email" />
                    </template>
                  </q-input>
                </div>

                <q-btn
                  type="submit"
                  color="primary"
                  size="lg"
                  :loading="isLoading"
                  class="full-width submit-button"
                  aria-label="Envoyer le lien de réinitialisation"
                >
                  Envoyer le lien de réinitialisation
                </q-btn>
              </q-form>
            </div>

            <div v-else class="success-state text-center">
              <q-icon name="mark_email_read" size="80px" color="positive" class="q-mb-md" />
              <h2 class="text-h6 q-mb-sm">Email envoyé !</h2>
              <p class="text-body2 text-grey-7 q-mb-md">
                Nous avons envoyé un lien de réinitialisation à <strong>{{ email }}</strong
                >. Vérifiez votre boîte de réception et suivez les instructions.
              </p>
              <p class="text-caption text-grey-6">
                Vous n'avez pas reçu l'email ? Vérifiez vos spams ou
                <button
                  class="resend-link"
                  :disabled="resendCooldown > 0 || isResending"
                  aria-label="Renvoyer l'email de réinitialisation"
                  @click="resendEmail"
                >
                  renvoyer{{
                    resendCooldown > 0 ? ` (${resendCooldown}s)` : isResending ? '...' : ''
                  }}
                </button>
              </p>
            </div>
          </q-card-section>

          <q-separator class="q-my-md" />

          <q-card-section class="text-center">
            <router-link to="/auth/login" class="back-link" aria-label="Retour à la connexion">
              <q-icon name="arrow_back" class="q-mr-xs" />
              Retour à la connexion
            </router-link>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue';
import AppLogo from '../../components/AppLogo.vue';
import { authService } from '../../services/authService';
import { useNotification } from '../../composables/notification';

// Note (limite backend connue, voir .claude/specifications/wire-forgot-password-page.md) :
// ForgotPasswordAsync génère un token mais n'envoie aujourd'hui aucun email réel côté
// backend — un ticket backend séparé est recommandé pour l'envoi effectif de l'email.
// Le message générique ci-dessous est volontairement identique que le compte existe ou
// non côté backend (la réponse n'est pas enumeration-safe : ne jamais afficher/logger
// `result.message`/`result.data` renvoyés par l'API).

const { showErrorNotification } = useNotification();

const GENERIC_ERROR_MESSAGE = 'Une erreur est survenue. Veuillez réessayer.';

const isLoading = ref(false);
const isResending = ref(false);
const emailSent = ref(false);
const email = ref('');
const resendCooldown = ref(0);
let cooldownInterval: NodeJS.Timeout | null = null;

/**
 * Calls the real password reset endpoint and reports success/failure.
 * Never reads/logs `result.message` or `result.data` (enumeration-safety, UI-side only).
 */
async function requestPasswordResetAndNotify(): Promise<boolean> {
  try {
    const result = await authService.requestPasswordReset(email.value);

    if (result?.isSuccess) {
      return true;
    }

    if (result?.statusCode === 422 && result.errors?.length) {
      showErrorNotification(result.errors.join(' '));
    } else {
      showErrorNotification(GENERIC_ERROR_MESSAGE);
    }

    return false;
  } catch (error) {
    console.error('Password reset request failed:', error);
    showErrorNotification(GENERIC_ERROR_MESSAGE);
    return false;
  }
}

async function handleForgotPassword() {
  isLoading.value = true;
  try {
    const success = await requestPasswordResetAndNotify();
    if (success) {
      emailSent.value = true;
      startResendCooldown();
    }
  } finally {
    isLoading.value = false;
  }
}

async function resendEmail() {
  if (resendCooldown.value > 0 || isResending.value) return;

  isResending.value = true;
  try {
    const success = await requestPasswordResetAndNotify();
    if (success) {
      startResendCooldown();
    }
  } finally {
    isResending.value = false;
  }
}

function startResendCooldown() {
  resendCooldown.value = 60;
  cooldownInterval = setInterval(() => {
    resendCooldown.value--;
    if (resendCooldown.value <= 0 && cooldownInterval) {
      clearInterval(cooldownInterval);
      cooldownInterval = null;
    }
  }, 1000);
}

onUnmounted(() => {
  if (cooldownInterval) {
    clearInterval(cooldownInterval);
  }
});
</script>

<style scoped>
.auth-page {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  min-height: 100vh;
}

.auth-container {
  width: 100%;
  max-width: 450px;
  padding: 20px;
}

.forgot-password-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.1);
  padding: 20px;
}

.form-group {
  margin-bottom: 16px;
}

.submit-button {
  margin-top: 20px;
  height: 48px;
  font-weight: 600;
}

.success-state {
  padding: 20px 0;
}

.resend-link {
  background: none;
  border: none;
  color: var(--q-primary);
  text-decoration: underline;
  cursor: pointer;
  font: inherit;
  padding: 0;
}

.resend-link:hover:not(:disabled) {
  color: var(--q-primary-dark);
}

.resend-link:disabled {
  color: var(--q-grey-6);
  cursor: not-allowed;
  text-decoration: none;
}

.back-link {
  color: var(--q-primary);
  text-decoration: none;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
}

.back-link:hover {
  text-decoration: underline;
}

@media (max-width: 599px) {
  .auth-container {
    padding: 16px;
  }

  .forgot-password-card {
    padding: 16px;
  }
}
</style>
