<template>
  <q-page class="profile-page">
    <!-- Breadcrumbs -->
    <div class="breadcrumb-container q-pa-md q-ma-lg">
      <div class="breadcrumb-content">
        <q-breadcrumbs class="text-grey-7" active-color="primary">
          <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
          <q-breadcrumbs-el label="Mon Profil" icon="person" />
        </q-breadcrumbs>
      </div>
    </div>

    <div class="profile-content q-pa-lg">
      <!-- Header Profile -->
      <q-card class="profile-header-card q-mb-lg">
        <q-card-section>
          <div class="row items-center q-gutter-lg">
            <q-avatar size="100px" color="primary" text-color="white">
              <span class="text-h3">{{ initials }}</span>
            </q-avatar>

            <div class="col">
              <h4 class="text-h4 q-my-none">{{ displayName }}</h4>
              <p class="text-h6 text-grey-7 q-my-sm">{{ user?.email }}</p>
              <p v-if="profileForm.department" class="text-subtitle1 text-grey-8 q-my-sm">
                {{ profileForm.department }}
              </p>

              <div class="q-mt-md">
                <q-chip
                  :color="user?.isActive ? 'positive' : 'negative'"
                  text-color="white"
                  icon="circle"
                  size="md"
                >
                  {{ user?.isActive ? 'Profil Actif' : 'Profil Inactif' }}
                </q-chip>
                <q-chip
                  v-for="role in user?.roles"
                  :key="role"
                  color="primary"
                  text-color="white"
                  icon="security"
                  size="md"
                  class="q-ml-sm"
                >
                  {{ role }}
                </q-chip>
              </div>
            </div>

            <div class="col-auto">
              <q-btn
                :color="isEditing ? 'negative' : 'primary'"
                :icon="isEditing ? 'close' : 'edit'"
                :label="isEditing ? 'Annuler' : 'Modifier'"
                unelevated
                @click="toggleEdit"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>

      <div class="row q-col-gutter-lg">
        <!-- Colonne gauche -->
        <div class="col-12 col-md-8">
          <!-- Informations personnelles -->
          <q-card class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="person" color="primary" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Informations personnelles</div>
                  <div class="text-caption text-grey-7">Coordonnées et informations de base</div>
                </div>
              </div>

              <div v-if="!isEditing" class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div class="row q-col-gutter-lg">
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="badge" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Prénom</div>
                        <div class="text-body1 text-weight-medium">
                          {{ profileForm.firstName || '-' }}
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="badge" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Nom</div>
                        <div class="text-body1 text-weight-medium">
                          {{ profileForm.lastName || '-' }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="email" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Email</div>
                        <div class="text-body1">{{ profileForm.email || '-' }}</div>
                      </div>
                    </div>
                  </div>
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="phone" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Téléphone</div>
                        <div class="text-body1">{{ profileForm.phone || '-' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="work" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Département</div>
                        <div class="text-body1">{{ profileForm.department || '-' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div v-else>
                <q-form class="q-gutter-md" @submit="updateProfile">
                  <div class="row q-gutter-md">
                    <q-input
                      v-model="profileForm.firstName"
                      label="Prénom *"
                      outlined
                      class="col"
                      :rules="[(val) => !!val || 'Le prénom est requis']"
                    />
                    <q-input
                      v-model="profileForm.lastName"
                      label="Nom *"
                      outlined
                      class="col"
                      :rules="[(val) => !!val || 'Le nom est requis']"
                    />
                  </div>

                  <q-input
                    v-model="profileForm.email"
                    label="Email *"
                    type="email"
                    outlined
                    readonly
                    hint="L'email ne peut pas être modifié"
                  />

                  <q-input v-model="profileForm.phone" label="Téléphone" outlined />

                  <q-input v-model="profileForm.department" label="Département" outlined />

                  <div class="row justify-end q-mt-lg">
                    <q-btn
                      type="submit"
                      color="primary"
                      label="Mettre à jour"
                      :loading="loading"
                      icon="save"
                      unelevated
                    />
                  </div>
                </q-form>
              </div>
            </q-card-section>
          </q-card>

          <!-- Adresse -->
          <q-card class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="location_on" color="secondary" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Adresse</div>
                  <div class="text-caption text-grey-7">Localisation personnelle</div>
                </div>
              </div>

              <div v-if="!isEditing" class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div class="row q-col-gutter-lg">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="home" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Rue</div>
                        <div class="text-body1">{{ profileForm.address.street || '-' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div v-if="profileForm.address.complement" class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="apartment" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Complément d'adresse</div>
                        <div class="text-body1">{{ profileForm.address.complement }}</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="location_city" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Ville</div>
                        <div class="text-body1">{{ profileForm.address.city || '-' }}</div>
                      </div>
                    </div>
                  </div>
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="pin_drop" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Code Postal</div>
                        <div class="text-body1">{{ profileForm.address.postalCode || '-' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="map" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Région</div>
                        <div class="text-body1">{{ profileForm.address.region || '-' }}</div>
                      </div>
                    </div>
                  </div>
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="public" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Pays</div>
                        <div class="text-body1">{{ profileForm.address.country || '-' }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div v-else>
                <div class="text-subtitle2 q-mb-md">Adresse</div>
                <div class="q-gutter-md">
                  <q-input v-model="profileForm.address.street" label="Rue" outlined />
                  <q-input
                    v-model="profileForm.address.complement"
                    label="Complément d'adresse"
                    outlined
                    hint="Bâtiment, étage, appartement..."
                  />
                  <div class="row q-gutter-md">
                    <q-input
                      v-model="profileForm.address.city"
                      label="Ville"
                      outlined
                      class="col"
                    />
                    <q-input
                      v-model="profileForm.address.postalCode"
                      label="Code Postal"
                      outlined
                      class="col-3"
                    />
                  </div>
                  <div class="row q-gutter-md">
                    <q-input
                      v-model="profileForm.address.region"
                      label="Région"
                      outlined
                      class="col"
                    />
                    <q-input
                      v-model="profileForm.address.country"
                      label="Pays"
                      outlined
                      class="col"
                    />
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Changement de mot de passe -->
          <q-card v-if="showPasswordSection" class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="lock" color="accent" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Changer le mot de passe</div>
                  <div class="text-caption text-grey-7">Sécurité du compte</div>
                </div>
              </div>

              <q-form class="q-gutter-md" @submit="changePassword">
                <q-input
                  v-model="passwordForm.newPassword"
                  label="Nouveau mot de passe *"
                  type="password"
                  outlined
                  :rules="[
                    (val) => !!val || 'Le nouveau mot de passe est requis',
                    (val) =>
                      val.length >= 8 || 'Le mot de passe doit contenir au moins 8 caractères',
                  ]"
                />

                <q-input
                  v-model="passwordForm.confirmPassword"
                  label="Confirmer le nouveau mot de passe *"
                  type="password"
                  outlined
                  :rules="[
                    (val) => !!val || 'La confirmation est requise',
                    (val) =>
                      val === passwordForm.newPassword || 'Les mots de passe ne correspondent pas',
                  ]"
                />

                <div class="row justify-end q-mt-lg">
                  <q-btn
                    type="submit"
                    color="accent"
                    label="Changer le mot de passe"
                    :loading="loadingPassword"
                    icon="vpn_key"
                    unelevated
                  />
                </div>
              </q-form>
            </q-card-section>
          </q-card>
        </div>

        <!-- Colonne droite -->
        <div class="col-12 col-md-4">
          <!-- Informations du compte -->
          <q-card class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="account_circle" color="primary" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Informations du compte</div>
                  <div class="text-caption text-grey-7">Statut et permissions</div>
                </div>
              </div>

              <div class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div class="info-group q-mb-md">
                  <q-icon name="verified" color="primary" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Statut</div>
                    <q-chip
                      :color="user?.isActive ? 'positive' : 'negative'"
                      text-color="white"
                      size="sm"
                      class="q-mt-xs"
                    >
                      {{ user?.isActive ? 'Actif' : 'Inactif' }}
                    </q-chip>
                  </div>
                </div>

                <div class="info-group q-mb-md">
                  <q-icon name="group" color="primary" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Rôles</div>
                    <div class="q-mt-xs">
                      <q-chip
                        v-for="role in user?.roles"
                        :key="role"
                        color="primary"
                        text-color="white"
                        size="sm"
                        class="q-mr-xs q-mb-xs"
                      >
                        {{ role }}
                      </q-chip>
                      <div
                        v-if="!user?.roles || user.roles.length === 0"
                        class="text-body2 text-grey-6"
                      >
                        Aucun rôle assigné
                      </div>
                    </div>
                  </div>
                </div>

                <div class="info-group q-mb-md">
                  <q-icon name="business" color="primary" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Organisation</div>
                    <div class="text-body1">{{ user?.organizationName || 'Aucune' }}</div>
                  </div>
                </div>

                <div class="info-group q-mb-md">
                  <q-icon name="schedule" color="primary" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Membre depuis</div>
                    <div class="text-body1">
                      {{
                        user?.createdAt ? new Date(user.createdAt).toLocaleDateString('fr-FR') : '-'
                      }}
                    </div>
                  </div>
                </div>

                <div class="info-group">
                  <q-icon name="login" color="primary" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Dernière connexion</div>
                    <div class="text-body1">
                      {{
                        user?.lastLoginAt
                          ? new Date(user.lastLoginAt).toLocaleDateString('fr-FR')
                          : '-'
                      }}
                    </div>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Actions du compte -->
          <q-card class="info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="settings" color="negative" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Actions du compte</div>
                  <div class="text-caption text-grey-7">Gestion de session</div>
                </div>
              </div>

              <div class="q-gutter-sm">
                <q-btn
                  flat
                  color="negative"
                  icon="logout"
                  label="Se déconnecter"
                  class="full-width"
                  @click="confirmLogout"
                />
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>

    <!-- Dialog de confirmation de déconnexion -->
    <q-dialog v-model="showLogoutDialog" persistent>
      <q-card>
        <q-card-section>
          <div class="text-h6">Confirmer la déconnexion</div>
        </q-card-section>

        <q-card-section> Êtes-vous sûr de vouloir vous déconnecter ? </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Annuler" @click="showLogoutDialog = false" />
          <q-btn color="negative" label="Se déconnecter" :loading="loadingLogout" @click="logout" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, onMounted, reactive, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useUserStore } from '../stores/userStore';
import { useQuasar } from 'quasar';
import { settings } from 'src/settings';
import { authService } from '../services/authService';
import type { ChangePasswordDto } from '../models/auth';
import type { UpdateUserDto } from '../models/user';

const $q = useQuasar();
const router = useRouter();
const authStore = useAuthStore();
const userStore = useUserStore();

const loading = ref(false);
const loadingPassword = ref(false);
const loadingLogout = ref(false);
const showLogoutDialog = ref(false);
const isEditing = ref(false);

const user = computed(() => authStore.user);
const showPasswordSection = computed(() => settings.auth.mode === 'jwt');

const displayName = computed(() => {
  const firstName = profileForm.firstName || user.value?.firstName || '';
  const lastName = profileForm.lastName || user.value?.lastName || '';
  return `${firstName} ${lastName}`.trim() || user.value?.email || 'Utilisateur';
});

const initials = computed(() => {
  const firstName = profileForm.firstName || user.value?.firstName || '';
  const lastName = profileForm.lastName || user.value?.lastName || '';
  const firstInitial = firstName.charAt(0).toUpperCase();
  const lastInitial = lastName.charAt(0).toUpperCase();
  return firstInitial + lastInitial || user.value?.email?.charAt(0).toUpperCase() || '?';
});

const profileForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  department: '',
  address: {
    street: '',
    complement: '',
    city: '',
    postalCode: '',
    region: '',
    country: '',
  },
});

const passwordForm = reactive({
  newPassword: '',
  confirmPassword: '',
});

const loadUserProfile = () => {
  if (user.value) {
    Object.assign(profileForm, {
      firstName: user.value.firstName || '',
      lastName: user.value.lastName || '',
      email: user.value.email || '',
      phone: user.value.phoneNumber || '',
      department: user.value.department || '',
      roles: user.value.roles || [],
      address: {
        street: user.value.address?.streetName || '',
        complement: user.value.address?.addressLine2 || '',
        city: user.value.address?.city || '',
        postalCode: user.value.address?.postalCode || '',
        region: user.value.address?.region || '',
        country: user.value.address?.country || '',
      },
    });
  }
};

const toggleEdit = () => {
  if (isEditing.value) {
    // Cancel edit - reload original data
    loadUserProfile();
  }
  isEditing.value = !isEditing.value;
};

const updateProfile = async () => {
  if (!user.value) {
    return;
  }

  try {
    loading.value = true;

    const payload: UpdateUserDto = {
      firstName: profileForm.firstName,
      lastName: profileForm.lastName,
      phoneNumber: profileForm.phone,
      department: profileForm.department,
      address: {
        // On reprend explicitement la valeur actuelle : ce champ est absent du
        // formulaire, mais le mapping backend (CreateMap<Address, AddressDto>) écrase
        // sans condition de null, donc l'omettre viderait ce champ en base à chaque
        // sauvegarde.
        ...(user.value?.address?.streetNumber !== undefined
          ? { streetNumber: user.value.address.streetNumber }
          : {}),
        streetName: profileForm.address.street,
        addressLine2: profileForm.address.complement,
        city: profileForm.address.city,
        postalCode: profileForm.address.postalCode,
        region: profileForm.address.region,
        country: profileForm.address.country,
      },
    };

    const response = await userStore.updateUser(user.value.id, payload);

    if (response?.isSuccess) {
      await authStore.loadCurrentUser();
      loadUserProfile();
      isEditing.value = false;
    }
    // Pas de $q.notify ici : userStore.updateUser notifie déjà (succès et échec).
  } finally {
    loading.value = false;
  }
};

const changePassword = async () => {
  try {
    loadingPassword.value = true;

    const changeData: ChangePasswordDto = {
      email: authStore.user?.email || '',
      token: authStore.token || '',
      newPassword: passwordForm.newPassword,
      confirmPassword: passwordForm.confirmPassword,
    };

    console.log(changeData);
    const result = await authService.changePassword(changeData);
    console.log(result);
    if (result?.isSuccess) {
      $q.notify({
        type: 'positive',
        message: 'Mot de passe changé avec succès',
      });

      // Réinitialiser le formulaire
      Object.assign(passwordForm, {
        newPassword: '',
        confirmPassword: '',
      });
    } else {
      $q.notify({
        type: 'negative',
        message: result?.message || 'Erreur lors du changement de mot de passe',
      });
    }
  } catch (error: any) {
    console.log(error.message);
    $q.notify({
      type: 'negative',
      message: 'Erreur lors du changement de mot de passe',
    });
  } finally {
    loadingPassword.value = false;
  }
};

const confirmLogout = () => {
  showLogoutDialog.value = true;
};

const logout = async () => {
  try {
    loadingLogout.value = true;
    await authStore.logout();

    $q.notify({
      type: 'positive',
      message: 'Déconnexion réussie',
    });

    router.push('/auth/login');
  } finally {
    loadingLogout.value = false;
    showLogoutDialog.value = false;
  }
};

onMounted(async () => {
  // Charger les informations utilisateur si pas déjà présentes
  if (!user.value) {
    // await authStore.getCurrentUser();
  }
  loadUserProfile();
});
</script>

<style lang="scss" scoped>
.breadcrumb-container {
  background: white;
  border: 1px solid #e0e0e0;
  width: fit-content;
  border-radius: 22px;
}

.breadcrumb-content {
  max-width: 1400px;
  margin: 0 auto;
}

.profile-content {
  flex: 1;
}

.profile-header-card {
  border-left: 4px solid var(--q-primary);
  background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
}

.info-card {
  border-left: 4px solid var(--q-primary);

  .info-content {
    border-left: 2px solid var(--q-primary);
    margin-left: 20px;
    position: relative;

    &::before {
      content: '';
      position: absolute;
      left: -7px;
      top: 20px;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--q-primary);
      border: 2px solid white;
    }
  }

  .info-group {
    display: flex;
    align-items: flex-start;
    gap: 8px;
  }
}

.info-card:nth-child(2) {
  border-left-color: var(--q-secondary);

  .info-content {
    border-left-color: var(--q-secondary);

    &::before {
      background: var(--q-secondary);
    }
  }
}

.info-card:nth-child(3) {
  border-left-color: var(--q-accent);

  .info-content {
    border-left-color: var(--q-accent);

    &::before {
      background: var(--q-accent);
    }
  }
}

.rounded-borders {
  border-radius: 8px;
}

.q-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.q-chip {
  font-size: 12px;
}

@media (max-width: 768px) {
  .profile-content {
    padding: 16px;
  }

  .breadcrumb-container {
    margin: 16px;
  }

  .row .col-12 {
    padding: 0;
  }
}
</style>
