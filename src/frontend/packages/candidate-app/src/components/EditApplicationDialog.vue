<template>
  <q-dialog v-model="isOpen" persistent max-width="600px">
    <q-card>
      <q-card-section class="row items-center q-pb-none">
        <div class="text-h6">Modifier ma candidature</div>
        <q-space />
        <q-btn icon="close" flat round dense @click="closeDialog" />
      </q-card-section>

      <q-card-section v-if="application">
        <div class="application-summary q-mb-md q-pa-md bg-grey-1 rounded-borders">
          <div class="text-weight-medium">{{ application.jobOfferTitle }}</div>
          <div class="text-primary">{{ application.organizationName }}</div>
          <div class="text-caption text-grey-7">
            Candidaté {{ formatDate(application.appliedAt) }}
          </div>
        </div>

        <q-form class="q-gutter-md" @submit="submitUpdate">
          <q-input
            v-model="updatedApplication.coverLetter"
            type="textarea"
            label="Lettre de motivation"
            placeholder="Expliquez pourquoi vous êtes le candidat idéal pour ce poste..."
            rows="6"
            filled
            :rules="[(val) => !!val || 'Une lettre de motivation est requise']"
            counter
            maxlength="2000"
          />

          <q-input
            v-model="updatedApplication.additionalNotes"
            type="textarea"
            label="Notes additionnelles (optionnel)"
            placeholder="Informations complémentaires, disponibilité, références..."
            rows="4"
            filled
            counter
            maxlength="1000"
          />

          <div class="text-caption text-grey-7 q-mb-md">
            <q-icon name="info" class="q-mr-xs" />
            Les modifications seront enregistrées et le recruteur sera notifié.
          </div>

          <div class="row justify-end q-gutter-sm">
            <q-btn flat label="Annuler" :disable="isSubmitting" @click="closeDialog" />
            <q-btn
              type="submit"
              color="primary"
              label="Enregistrer les modifications"
              icon="save"
              :loading="isSubmitting"
            />
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { date } from 'quasar';
import { useApplicationStore } from '../stores/applicationStore';
import { useNotification } from '../composables/notification';
import type { ApplicationDto } from '../models/application';

interface Props {
  modelValue: boolean;
  application: ApplicationDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'updated'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Composables
const { showSuccessNotification, showErrorNotification } = useNotification();
const applicationStore = useApplicationStore();

// State
const isOpen = ref(props.modelValue);
const isSubmitting = ref(false);

const updatedApplication = reactive({
  coverLetter: '',
  additionalNotes: '',
});

// Methods
const initializeForm = () => {
  if (props.application) {
    updatedApplication.coverLetter = props.application.coverLetter || '';
    updatedApplication.additionalNotes = props.application.additionalNotes || '';
  }
};

const resetForm = () => {
  updatedApplication.coverLetter = '';
  updatedApplication.additionalNotes = '';
};

const closeDialog = () => {
  emit('update:modelValue', false);
  resetForm();
};

const submitUpdate = async () => {
  if (!props.application) return;

  isSubmitting.value = true;

  try {
    const success = await applicationStore.updateApplication(
      props.application.id,
      updatedApplication.coverLetter,
      updatedApplication.additionalNotes || undefined,
    );

    if (success) {
      showSuccessNotification('Candidature mise à jour avec succès !');

      emit('updated');
      closeDialog();
    } else {
      showErrorNotification(applicationStore.error || 'Erreur lors de la mise à jour');
    }
  } catch (error) {
    showErrorNotification('Une erreur inattendue est survenue');
  } finally {
    isSubmitting.value = false;
  }
};

const formatDate = (dateString: string): string => {
  return date.formatDate(new Date(dateString), 'DD/MM/YYYY');
};

// Watchers
watch(
  () => props.modelValue,
  (newValue) => {
    isOpen.value = newValue;
    if (newValue) {
      initializeForm();
    }
  },
);

watch(isOpen, (newValue) => {
  emit('update:modelValue', newValue);
});

watch(
  () => props.application,
  () => {
    if (isOpen.value) {
      initializeForm();
    }
  },
);
</script>

<style lang="scss" scoped>
.application-summary {
  border: 1px solid #e0e0e0;
}
</style>
