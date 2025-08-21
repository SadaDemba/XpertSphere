<template>
  <q-header elevated class="bg-primary text-white" role="banner">
    <q-toolbar>
      <q-btn
        flat
        dense
        round
        icon="menu"
        aria-label="Toggle navigation menu"
        :aria-expanded="drawerOpen"
        aria-controls="navigation-drawer"
        @click="toggleDrawer"
      />

      <q-toolbar-title class="q-ml-sm">
        <div class="header-brand row items-center q-gutter-sm">
          <app-logo variant="icon" size="small" class="header-logo" />
          <div class="brand-text">
            <h1 class="text-h6 q-ma-none text-white">
              <router-link
                to="/"
                class="text-white no-decoration"
                aria-label="XpertSphere Recruiter ATS - Return to homepage"
              >
                Recruiter ATS
              </router-link>
            </h1>
          </div>
        </div>
      </q-toolbar-title>

      <q-space />

      <div class="q-gutter-sm row items-center">
        <q-btn
          flat
          round
          dense
          icon="notifications"
          aria-label="Notifications"
          @click="showNotifications"
        >
          <q-badge
            v-if="notificationCount > 0"
            color="red"
            floating
            :aria-label="`You have ${notificationCount} new notifications`"
          >
            {{ notificationCount }}
          </q-badge>
        </q-btn>

        <!-- User menu with dropdown -->
        <q-btn-dropdown flat round dense icon="account_circle" aria-label="User menu">
          <q-list style="min-width: 200px">
            <!-- User info -->
            <q-item class="text-center q-pb-sm">
              <q-item-section>
                <q-item-label class="text-weight-medium">
                  {{ authStore.user?.firstName }} {{ authStore.user?.lastName }}
                </q-item-label>
                <q-item-label caption>
                  {{ authStore.user?.email }}
                </q-item-label>
                <q-item-label caption class="text-primary text-weight-medium">
                  {{ userRoleDisplay }}
                </q-item-label>
              </q-item-section>
            </q-item>

            <q-separator />

            <!-- Profile link -->
            <q-item clickable @click="goToProfile">
              <q-item-section avatar>
                <q-icon name="person" />
              </q-item-section>
              <q-item-section>Mon profil</q-item-section>
            </q-item>

            <!-- Preferences link -->
            <q-item clickable @click="goToPreferences">
              <q-item-section avatar>
                <q-icon name="settings" />
              </q-item-section>
              <q-item-section>Préférences</q-item-section>
            </q-item>

            <!-- Tutorials link -->
            <q-item clickable @click="showComingSoon">
              <q-item-section avatar>
                <q-icon name="school" />
              </q-item-section>
              <q-item-section>Tutoriels</q-item-section>
            </q-item>

            <q-separator />

            <!-- Logout -->
            <q-item clickable @click="handleLogout">
              <q-item-section avatar>
                <q-icon name="logout" />
              </q-item-section>
              <q-item-section>Déconnexion</q-item-section>
            </q-item>
          </q-list>
        </q-btn-dropdown>
      </div>
    </q-toolbar>
  </q-header>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useQuasar } from 'quasar';
import AppLogo from './AppLogo.vue';

interface Props {
  drawerOpen: boolean;
}

interface Emits {
  (e: 'toggle-drawer'): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

const router = useRouter();
const authStore = useAuthStore();
const $q = useQuasar();
const notificationCount = ref(3);

// Format user roles for display
const userRoleDisplay = computed(() => {
  if (!authStore.user?.roles || authStore.user.roles.length === 0) {
    return 'Aucun rôle';
  }
  // Display the first role or multiple roles
  const roles = authStore.user.roles;
  if (roles.length === 1) {
    return formatRoleName(roles[0] ?? '');
  }
  return `${formatRoleName(roles[0] ?? '')} et ${roles.length - 1} autres`;
});

function formatRoleName(role: string): string {
  const roleNames: Record<string, string> = {
    'XpertSphere.SuperAdmin': 'Super Admin',
    'XpertSphere.Admin': 'Admin Plateforme',
    'Organization.Admin': 'Admin Organisation',
    'Organization.Manager': 'Manager',
    'Organization.Recruiter': 'Recruteur',
    'Organization.TechnicalEvaluator': 'Évaluateur Technique',
    Candidate: 'Candidat',
  };
  return roleNames[role] || role;
}

function toggleDrawer() {
  emit('toggle-drawer');
}

function showNotifications() {
  // Notifications functionality to be implemented
  console.log('Show notifications');
}

function goToProfile() {
  router.push('/profile');
}

function goToPreferences() {
  showComingSoon();
}

function showComingSoon() {
  $q.notify({
    type: 'info',
    message: 'Cette fonctionnalité sera bientôt disponible',
    position: 'top',
    timeout: 3000,
    actions: [{ label: 'OK', color: 'white', handler: () => {} }],
  });
}

async function handleLogout() {
  await authStore.logout();
  router.push('/auth/login');
}
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
}
</style>
