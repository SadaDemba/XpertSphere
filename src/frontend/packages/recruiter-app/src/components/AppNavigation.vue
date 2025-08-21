<template>
  <q-drawer
    id="navigation-drawer"
    v-model="isOpen"
    show-if-above
    bordered
    class="bg-grey-1"
    :width="280"
    role="navigation"
    aria-label="Main navigation"
  >
    <div class="drawer-container">
      <q-scroll-area class="drawer-content">
        <nav role="navigation" aria-label="Main navigation">
          <!-- Main Menu (only for Organization.* roles) -->
          <q-list v-if="showMainMenu">
            <q-item-label
              header
              class="text-grey-8 text-weight-bold q-pa-md"
              role="heading"
              aria-level="2"
            >
              Menu Principal
            </q-item-label>

            <nav-item
              v-for="item in navigationItems"
              :key="item.name"
              v-bind="item"
              @navigate="handleNavigation"
            />
          </q-list>

          <!-- Admin Section (for Platform roles and Organization.Admin) -->
          <template v-if="showAdminSection">
            <q-separator v-if="showMainMenu" class="q-my-md" />

            <q-list>
              <q-item-label
                header
                class="text-grey-8 text-weight-bold q-pa-md"
                role="heading"
                aria-level="2"
              >
                Administration
              </q-item-label>

              <nav-item
                v-for="item in filteredAdminItems"
                :key="item.name"
                v-bind="item"
                @navigate="handleNavigation"
              />
            </q-list>
          </template>
        </nav>
      </q-scroll-area>

      <!-- Logout button pinned at the bottom -->
      <div class="drawer-footer">
        <q-separator />
        <q-list>
          <nav-item
            name="logout"
            title="Se déconnecter"
            icon="logout"
            action="logout"
            description="Se déconnecter de l'application"
            @navigate="handleLogout"
          />
        </q-list>
      </div>
    </div>
  </q-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { UserRole, PlatformRoles, OrganizationRoles } from '../models/auth';
import NavItem from './NavItem.vue';

interface Props {
  modelValue: boolean;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();
const router = useRouter();
const authStore = useAuthStore();

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

// Check if user has platform roles (SuperAdmin or Admin)
const hasPlatformRole = computed(() => {
  return authStore.hasAnyRole(PlatformRoles);
});

// Check if user has organization roles
const hasOrganizationRole = computed(() => {
  return authStore.hasAnyRole(OrganizationRoles);
});

// Check if user is Organization Admin
const isOrganizationAdmin = computed(() => {
  return authStore.hasRole(UserRole.OrganizationAdmin);
});

// Show main menu only for Organization.* roles
const showMainMenu = computed(() => {
  return hasOrganizationRole.value;
});

// Show admin section for Platform roles and Organization.Admin
const showAdminSection = computed(() => {
  return hasPlatformRole.value || isOrganizationAdmin.value;
});

// Filter admin items based on role
const filteredAdminItems = computed(() => {
  if (hasPlatformRole.value) {
    // Platform admins see all admin items
    return adminItems;
  } else if (isOrganizationAdmin.value) {
    // Organization admin doesn't see Organizations management
    return adminItems.filter((item) => item.name !== 'organizations');
  }
  return [];
});

const navigationItems = [
  {
    name: 'dashboard',
    title: 'Tableau de bord',
    icon: 'dashboard',
    route: '/',
    description: 'Voir la vue d’ensemble et les statistiques du recrutement',
  },
  {
    name: 'jobs',
    title: 'Offres d’emploi',
    icon: 'work',
    route: '/jobs',
    description: 'Gérer les offres et les besoins en recrutement',
  },
  {
    name: 'candidates',
    title: 'Candidats',
    icon: 'people',
    route: '/candidates',
    description: 'Consulter et gérer les profils des candidats',
  },
  {
    name: 'applications',
    title: 'Candidatures',
    icon: 'assignment',
    route: '/applications',
    description: 'Examiner et traiter les candidatures',
  },
  {
    name: 'interviews',
    title: 'Entretiens',
    icon: 'event',
    route: '/interviews',
    description: 'Planifier et gérer les entretiens',
  },
  {
    name: 'reports',
    title: 'Rapports',
    icon: 'analytics',
    route: '/reports',
    description: 'Voir les analyses et rapports de recrutement',
  },
];

const adminItems = [
  {
    name: 'users',
    title: 'Utilisateurs',
    icon: 'people',
    route: '/admin/users',
    description: 'Gérer les comptes utilisateur et leurs permissions',
  },
  {
    name: 'organizations',
    title: 'Organisations',
    icon: 'business',
    route: '/admin/organizations',
    description: 'Gérer les organisations de la plateforme',
  },
  {
    name: 'roles',
    title: 'Rôles',
    icon: 'admin_panel_settings',
    route: '/admin/roles',
    description: 'Gérer les rôles et permissions',
  },
];

function handleNavigation(routePath?: string) {
  if (routePath) {
    router.push(routePath);
  }
}

async function handleLogout() {
  try {
    await authStore.logout();
    router.push('/auth/login');
  } catch (error) {
    console.error('Erreur lors de la déconnexion:', error);
  }
}
</script>

<style scoped>
.drawer-container {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.drawer-content {
  flex: 1;
  overflow-y: auto;
}

.drawer-footer {
  background: white;
  box-shadow: 0 -2px 4px rgba(0, 0, 0, 0.05);
}
</style>
