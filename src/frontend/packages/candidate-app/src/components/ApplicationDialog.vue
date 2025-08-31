<template>
  <q-dialog v-model="isOpen" persistent max-width="600px">
    <q-card>
      <q-card-section class="row items-center q-pb-none">
        <div class="text-h6">Candidater à cette offre</div>
        <q-space />
        <q-btn icon="close" flat round dense @click="closeDialog" />
      </q-card-section>

      <q-card-section v-if="job">
        <div class="job-summary q-mb-md q-pa-md bg-grey-1 rounded-borders">
          <div class="text-weight-medium">{{ job.title }}</div>
          <div class="text-primary">{{ job.organizationName }}</div>
          <div class="row q-gutter-sm q-mt-sm">
            <q-chip size="sm" outline icon="place">{{ job.location || 'Non spécifiée' }}</q-chip>
            <q-chip size="sm" outline icon="work">{{ workModeLabels[job.workMode] }}</q-chip>
          </div>
        </div>

        <q-form class="q-gutter-md" @submit="submitApplication">
          <q-input
            v-model="application.coverLetter"
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
            v-model="application.additionalNotes"
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
            Votre candidature sera envoyée avec les informations de votre profil.
          </div>

          <div class="row justify-end q-gutter-sm">
            <q-btn flat label="Annuler" :disable="isSubmitting" @click="closeDialog" />
            <q-btn
              type="submit"
              color="positive"
              label="Envoyer la candidature"
              icon="send"
              :loading="isSubmitting"
            />
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, reactive, watch } from 'vue';
import { useApplicationStore } from '../stores/applicationStore';
import { useNotification } from '../composables/notification';
import type { JobOfferDto } from '../models/job';
import type { CreateApplicationDto } from '../models/application';
import { workModeLabels } from '../models/job';

interface Props {
  modelValue: boolean;
  job: JobOfferDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'submitted'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Composables
const { showErrorNotification } = useNotification();
const applicationStore = useApplicationStore();

// State
const isOpen = ref(props.modelValue);
const isSubmitting = ref(false);

const application = reactive<Omit<CreateApplicationDto, 'jobOfferId'>>({
  coverLetter: '',
  additionalNotes: '',
});

// Methods
const resetForm = () => {
  application.coverLetter = '';
  application.additionalNotes = '';
};

const closeDialog = () => {
  emit('update:modelValue', false);
  resetForm();
};

const submitApplication = async () => {
  if (!props.job) return;

  isSubmitting.value = true;

  try {
    const createApplicationDto: CreateApplicationDto = {
      jobOfferId: props.job.id,
      coverLetter: application.coverLetter ?? '',
      additionalNotes: application.additionalNotes ?? '',
    };

    await applicationStore.applyToJob(createApplicationDto);

    emit('submitted');
    closeDialog();
  } catch (error: any) {
    console.log(error.message);
    showErrorNotification('Une erreur inattendue est survenue');
  } finally {
    isSubmitting.value = false;
  }
};

// Watchers
watch(
  () => props.modelValue,
  (newValue) => {
    isOpen.value = newValue;
  },
);

watch(isOpen, (newValue) => {
  emit('update:modelValue', newValue);
});
</script>

<style lang="scss" scoped>
.job-summary {
  border: 1px solid #e0e0e0;
}
</style>
