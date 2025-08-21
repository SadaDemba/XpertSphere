<template>
  <q-header elevated class="bg-primary text-white" role="banner">
    <q-toolbar>
      <q-toolbar-title class="q-ml-sm">
        <div class="header-brand row items-center q-gutter-sm">
          <app-logo variant="icon" size="small" class="header-logo" />
          <div class="brand-text">
            <h1 class="text-h6 q-ma-none text-white">
              <router-link
                to="/"
                class="text-white no-decoration"
                aria-label="XpertSphere Candidate Portal - Return to homepage"
              >
                XpertSphere
              </router-link>
            </h1>
          </div>
        </div>
      </q-toolbar-title>

      <!-- Desktop Navigation -->
      <div class="row q-gutter-md gt-sm">
        <q-btn flat label="Offres d'emploi" icon="work" :to="'/'" exact />
        <q-btn
          v-if="isAuthenticated"
          flat
          label="Mes candidatures"
          icon="assignment"
          :to="'/applications'"
        />
      </div>

      <q-space />

      <!-- User Menu -->
      <div v-if="isAuthenticated" class="user-menu">
        <q-btn flat round>
          <q-avatar size="32px" color="white" text-color="primary">
            <q-icon name="person" />
          </q-avatar>

          <q-menu>
            <q-list style="min-width: 200px">
              <q-item-label header>
                {{ userFullName }}
              </q-item-label>

              <q-separator />

              <q-item v-close-popup clickable to="/profile">
                <q-item-section avatar>
                  <q-icon name="account_circle" />
                </q-item-section>
                <q-item-section>Profil</q-item-section>
              </q-item>

              <q-item v-close-popup clickable to="/applications">
                <q-item-section avatar>
                  <q-icon name="assignment" />
                </q-item-section>
                <q-item-section>Mes candidatures</q-item-section>
              </q-item>

              <q-separator />

              <q-item v-close-popup clickable @click="logout">
                <q-item-section avatar>
                  <q-icon name="logout" />
                </q-item-section>
                <q-item-section>Déconnexion</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </div>

      <!-- Login/Register buttons -->
      <div v-else class="auth-buttons row q-gutter-sm">
        <q-btn outline label="Connexion" icon="login" to="/login" color="white" />
        <q-btn unelevated label="Inscription" icon="person_add" to="/register" color="secondary" />
      </div>
    </q-toolbar>
  </q-header>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useNotification } from '../composables/notification';
import AppLogo from './AppLogo.vue';

// No props or emits needed for simplified header

// Composables
const router = useRouter();
const { showSuccessNotification } = useNotification();

// Store
const authStore = useAuthStore();
const { isAuthenticated, userFullName } = storeToRefs(authStore);

// Methods
const logout = async () => {
  authStore.logout();

  showSuccessNotification('Vous avez été déconnecté avec succès');

  router.push('/');
};
</script>

<style scoped>
.no-decoration {
  text-decoration: none;
}

.header-brand {
  align-items: center;
}

.header-logo {
  flex-shrink: 0;
}

.brand-text h1 {
  font-weight: 600;
  letter-spacing: 0.5px;
}

.brand-text a:focus {
  outline: 2px solid white;
  outline-offset: 2px;
  border-radius: 4px;
  padding: 2px 4px;
}

.brand-text a:hover {
  opacity: 0.9;
}

/* Adjust toolbar back to normal size */
.q-toolbar {
  min-height: 64px;
  padding: 8px 16px;
}

.user-menu {
  .q-btn {
    border-radius: 50%;
  }
}

.auth-buttons {
  .q-btn {
    min-width: 100px;
  }
}

/* Responsive adjustments */
@media (max-width: 599px) {
  .q-toolbar {
    min-height: 56px;
    padding: 8px 12px;
  }

  .header-brand {
    gap: 8px;
  }

  .brand-text h1 {
    font-size: 1rem;
  }

  .auth-buttons .q-btn {
    min-width: 80px;
  }
}
</style>
