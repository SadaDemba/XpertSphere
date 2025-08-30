<template>
  <q-page padding>
    <div class="row q-gutter-lg">
      <!-- Informations personnelles -->
      <div class="col-12 col-md-8">
        <q-card>
          <q-card-section>
            <div class="text-h6 q-mb-md">Informations personnelles</div>

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

              <q-input v-model="profileForm.title" label="Titre/Poste" outlined />

              <q-separator class="q-my-md" />
              <div class="text-subtitle2 q-mb-md">Adresse</div>

              <q-input v-model="profileForm.address.street" label="Rue" outlined />

              <q-input
                v-model="profileForm.address.complement"
                label="Complément d'adresse"
                outlined
                hint="Bâtiment, étage, appartement..."
              />

              <div class="row q-gutter-md">
                <q-input v-model="profileForm.address.city" label="Ville" outlined class="col" />
                <q-input
                  v-model="profileForm.address.postalCode"
                  label="Code Postal"
                  outlined
                  class="col-3"
                />
              </div>

              <div class="row q-gutter-md">
                <q-input v-model="profileForm.address.region" label="Région" outlined class="col" />
                <q-input v-model="profileForm.address.country" label="Pays" outlined class="col" />
              </div>

              <div class="row justify-end q-mt-lg">
                <q-btn type="submit" color="primary" label="Mettre à jour" :loading="loading" />
              </div>
            </q-form>
          </q-card-section>
        </q-card>

        <!-- Changement de mot de passe -->
        <q-card v-if="showPasswordSection" class="q-mt-lg">
          <q-card-section>
            <div class="text-h6 q-mb-md">Changer le mot de passe</div>

            <q-form class="q-gutter-md" @submit="changePassword">
              <q-input
                v-model="passwordForm.newPassword"
                label="Nouveau mot de passe *"
                type="password"
                outlined
                :rules="[
                  (val) => !!val || 'Le nouveau mot de passe est requis',
                  (val) => val.length >= 8 || 'Le mot de passe doit contenir au moins 8 caractères',
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
                  color="primary"
                  label="Changer le mot de passe"
                  :loading="loadingPassword"
                />
              </div>
            </q-form>
          </q-card-section>
        </q-card>
      </div>

      <!-- Informations du compte -->
      <div class="col-12 col-md-4">
        <q-card>
          <q-card-section>
            <div class="text-h6 q-mb-md">Informations du compte</div>

            <div class="q-gutter-sm">
              <div class="text-body2">
                <strong>Statut:</strong>
                <q-chip
                  :color="user?.isActive ? 'positive' : 'negative'"
                  text-color="white"
                  size="sm"
                  class="q-ml-sm"
                >
                  {{ user?.isActive ? 'Actif' : 'Inactif' }}
                </q-chip>
              </div>

              <div class="text-body2">
                <strong>Rôles:</strong>
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
                </div>
              </div>

              <div class="text-body2">
                <strong>Organisation:</strong>
                <br />
                {{ user?.organizationName || 'Aucune' }}
              </div>

              <div class="text-body2">
                <strong>Membre depuis:</strong>
                <br />
                {{ user?.createdAt ? new Date(user.createdAt).toLocaleDateString('fr-FR') : '-' }}
              </div>

              <div class="text-body2">
                <strong>Dernière connexion:</strong>
                <br />
                {{
                  user?.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString('fr-FR') : '-'
                }}
              </div>
            </div>
          </q-card-section>
        </q-card>

        <!-- Actions du compte -->
        <q-card class="q-mt-lg">
          <q-card-section>
            <div class="text-h6 q-mb-md">Actions du compte</div>

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
import { useQuasar } from 'quasar';
import { settings } from 'src/settings';
import { authService } from '../services/authService';
import type { ChangePasswordDto } from '../models/auth';

const $q = useQuasar();
const router = useRouter();
const authStore = useAuthStore();

const loading = ref(false);
const loadingPassword = ref(false);
const loadingLogout = ref(false);
const showLogoutDialog = ref(false);

const user = computed(() => authStore.user);
const showPasswordSection = computed(() => settings.auth.mode === 'jwt');

const profileForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  title: '',
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
      phoneNumber: user.value.phoneNumber || '',
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

const updateProfile = async () => {
  try {
    loading.value = true;

    // TODO: Implémenter l'appel API pour mettre à jour le profil
    // await authStore.updateProfile(profileForm);

    $q.notify({
      type: 'positive',
      message: 'Profil mis à jour avec succès',
    });
  } catch (error: any) {
    console.log(error.message);
    $q.notify({
      type: 'negative',
      message: 'Erreur lors de la mise à jour du profil',
    });
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
    // await authStore.logoutUser();

    $q.notify({
      type: 'positive',
      message: 'Déconnexion réussie',
    });

    router.push('/auth/login');
  } catch (error: any) {
    console.log(error.message);
    $q.notify({
      type: 'negative',
      message: 'Erreur lors de la déconnexion',
    });
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
