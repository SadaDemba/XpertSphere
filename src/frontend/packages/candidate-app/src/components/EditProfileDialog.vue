<template>
  <q-dialog v-model="showDialog" persistent>
    <q-card style="min-width: 500px">
      <q-card-section>
        <div class="text-h6">Modifier le profil</div>
      </q-card-section>

      <q-card-section class="q-pt-none">
        <q-form class="q-gutter-md" @submit="onSubmit">
          <!-- Personal Information -->
          <div class="row q-gutter-md">
            <q-input
              v-model="formData.firstName"
              label="Prénom"
              filled
              class="col"
              :rules="[(val) => !!val || 'Le prénom est requis']"
            />
            <q-input
              v-model="formData.lastName"
              label="Nom"
              filled
              class="col"
              :rules="[(val) => !!val || 'Le nom est requis']"
            />
          </div>

          <q-input
            v-model="formData.phoneNumber"
            label="Téléphone"
            filled
            mask="## ## ## ## ##"
            hint="Format: 06 12 34 56 78"
          />

          <!-- Address Information -->
          <q-separator />
          <div class="text-subtitle1 q-mt-md q-mb-sm">Adresse</div>

          <div class="row q-gutter-md">
            <q-input v-model="formData.streetNumber" label="Numéro" filled class="col-2" />
            <q-input v-model="formData.street" label="Rue" filled class="col" />
          </div>

          <div class="row q-gutter-md">
            <q-input v-model="formData.city" label="Ville" filled class="col" />
            <q-input v-model="formData.postalCode" label="Code postal" filled class="col-3" />
          </div>

          <div class="row q-gutter-md">
            <q-input v-model="formData.region" label="Région" filled class="col" />
            <q-input v-model="formData.country" label="Pays" filled class="col" />
          </div>

          <q-input v-model="formData.addressLine2" label="Complément d'adresse" filled />

          <!-- Professional Information -->
          <q-separator />
          <div class="text-subtitle1 q-mt-md q-mb-sm">Informations professionnelles</div>

          <div class="row q-gutter-md">
            <q-input
              v-model.number="formData.yearsOfExperience"
              label="Années d'expérience"
              type="number"
              filled
              class="col"
              min="0"
              max="50"
            />
            <q-input
              v-model.number="formData.desiredSalary"
              label="Salaire souhaité (€)"
              type="number"
              filled
              class="col"
              min="0"
            />
          </div>

          <q-input v-model="formData.availability" label="Disponibilité" filled type="date" />

          <q-input
            v-model="formData.linkedInProfile"
            label="Profil LinkedIn"
            filled
            placeholder="https://linkedin.com/in/votre-profil"
          />
        </q-form>
      </q-card-section>

      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="Annuler" color="negative" @click="onCancel" />
        <q-btn label="Enregistrer" color="primary" :loading="isLoading" @click="onSubmit" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useUserStore } from '../stores/userStore';
import { useAuthStore } from '../stores/authStore';
import { useNotification } from 'src/composables/notification';
import type { User } from '../models/auth';
import type { UpdateUserProfileDto } from '../services/userService';

// Props
interface Props {
  modelValue: boolean;
  user?: User | null;
}

const props = withDefaults(defineProps<Props>(), {
  user: null,
});

// Emits
const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  'profile-updated': [user: User];
}>();

// Stores and composables
const userStore = useUserStore();
const notification = useNotification();

// Reactive data
const showDialog = ref(props.modelValue);
const isLoading = ref(false);
const formData = ref<UpdateUserProfileDto>({
  firstName: '',
  lastName: '',
  phoneNumber: '',
  streetNumber: '',
  street: '',
  city: '',
  postalCode: '',
  region: '',
  country: '',
  addressLine2: '',
  yearsOfExperience: undefined,
  desiredSalary: undefined,
  availability: '',
  linkedInProfile: '',
});

// Watch for dialog visibility changes
watch(
  () => props.modelValue,
  (newValue) => {
    showDialog.value = newValue;
    if (newValue && props.user) {
      loadUserData();
    }
  },
);

watch(showDialog, (newValue) => {
  emit('update:modelValue', newValue);
});

// Load user data into form
const loadUserData = () => {
  if (!props.user) return;

  formData.value = {
    firstName: props.user.firstName || '',
    lastName: props.user.lastName || '',
    phoneNumber: props.user.phoneNumber || '',
    streetNumber: props.user.address?.streetNumber || '',
    street: props.user.address?.streetName || '',
    city: props.user.address?.city || '',
    postalCode: props.user.address?.postalCode || '',
    region: props.user.address?.region || '',
    country: props.user.address?.country || '',
    addressLine2: props.user.address?.addressLine2 || '',
    yearsOfExperience: props.user.yearsOfExperience || undefined,
    desiredSalary: props.user.desiredSalary || undefined,
    availability: props.user.availability || '',
    linkedInProfile: props.user.linkedInProfile || '',
  };
};

// Handle form submission
const onSubmit = async () => {
  if (!props.user?.id) return;

  // Show confirmation dialog
  const confirmed = await notification.showConfirmDialog(
    'Confirmer la modification',
    'Êtes-vous sûr de vouloir modifier votre profil ?',
  );

  if (!confirmed) return;

  try {
    isLoading.value = true;

    const success = await userStore.updateUserProfile(props.user.id, formData.value);

    if (success) {
      // Use authStore.user which is always up-to-date after the update
      const authStore = useAuthStore();
      if (authStore.user) {
        emit('profile-updated', authStore.user);
      }
      showDialog.value = false;
    }
  } finally {
    isLoading.value = false;
  }
};

// Handle cancel
const onCancel = () => {
  showDialog.value = false;
};
</script>

<style scoped>
.q-dialog__inner {
  padding: 16px;
}
</style>
