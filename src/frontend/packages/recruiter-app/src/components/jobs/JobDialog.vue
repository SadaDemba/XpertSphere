<template>
  <q-dialog v-model="isOpen" persistent max-width="800px">
    <q-card class="job-dialog">
      <q-card-section class="row items-center">
        <div class="text-h6">
          {{ isEditing ? "Modifier l'offre" : "Nouvelle offre d'emploi" }}
        </div>
        <q-space />
        <q-btn
          flat
          round
          dense
          icon="close"
          aria-label="Fermer la boîte de dialogue"
          @click="closeDialog"
        />
      </q-card-section>

      <q-separator />

      <q-card-section class="q-pa-md">
        <q-form class="q-gutter-md" @submit="saveJob">
          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.title"
                outlined
                label="Titre de l'offre *"
                :rules="[(val) => !!val || 'Le titre est obligatoire']"
                aria-label="Titre de l'offre d'emploi"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-6">
              <q-input
                v-model="formData.location"
                outlined
                label="Localisation"
                aria-label="Localisation"
              />
            </div>
            <div class="col-12 col-md-3">
              <q-select
                v-model="formData.workMode"
                outlined
                label="Mode de travail *"
                :options="workModeOptions"
                :rules="[(val) => val !== undefined || 'Le mode de travail est obligatoire']"
                aria-label="Mode de travail"
                required
              />
            </div>
            <div class="col-12 col-md-3">
              <q-select
                v-model="formData.contractType"
                outlined
                label="Type de contrat *"
                :options="contractTypeOptions"
                :rules="[(val) => val !== undefined || 'Le type de contrat est obligatoire']"
                aria-label="Type de contrat"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-4">
              <q-input
                v-model="formData.salaryMin"
                outlined
                type="number"
                label="Salaire minimum"
                suffix="€"
                aria-label="Salaire minimum"
              />
            </div>
            <div class="col-12 col-md-4">
              <q-input
                v-model="formData.salaryMax"
                outlined
                type="number"
                label="Salaire maximum"
                suffix="€"
                aria-label="Salaire maximum"
              />
            </div>
            <div class="col-12 col-md-4">
              <q-input
                v-model="formData.expiresAt"
                outlined
                type="date"
                label="Date d'expiration"
                aria-label="Date d'expiration de l'offre"
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.description"
                outlined
                type="textarea"
                label="Description du poste *"
                :rules="[(val) => !!val || 'La description est obligatoire']"
                rows="6"
                aria-label="Description détaillée du poste"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.requirements"
                outlined
                type="textarea"
                label="Exigences et qualifications *"
                :rules="[(val) => !!val || 'Les exigences sont obligatoires']"
                rows="4"
                aria-label="Exigences et qualifications requises"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-6">
              <q-select
                v-model="formData.status"
                outlined
                label="Statut *"
                :options="statusOptions"
                :rules="[(val) => val !== undefined || 'Le statut est obligatoire']"
                aria-label="Statut de publication"
                required
              />
            </div>
          </div>
        </q-form>
      </q-card-section>

      <q-separator />

      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="Annuler" aria-label="Annuler et fermer" @click="closeDialog" />
        <q-btn
          color="primary"
          label="Enregistrer"
          :loading="saving"
          :aria-label="isEditing ? 'Enregistrer les modifications' : 'Créer l\'offre'"
          @click="saveJob"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { JobOffer } from '../../models/job';
import { WorkMode, ContractType, JobOfferStatus } from '../../enums';
import { workModeLabels, contractTypeLabels } from '../../models/job';

interface JobFormData {
  title: string;
  description: string;
  requirements: string;
  location: string;
  workMode: WorkMode;
  contractType: ContractType;
  salaryMin: number | undefined;
  salaryMax: number | undefined;
  salaryCurrency: string;
  status: JobOfferStatus;
  expiresAt: string;
}

interface Props {
  modelValue: boolean;
  job?: JobOffer | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'saved'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const saving = ref(false);

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

const isEditing = computed(() => !!props.job);

const formData = ref<JobFormData>({
  title: '',
  description: '',
  requirements: '',
  location: '',
  workMode: WorkMode.OnSite,
  contractType: ContractType.FullTime,
  salaryMin: undefined,
  salaryMax: undefined,
  salaryCurrency: 'EUR',
  status: JobOfferStatus.Draft,
  expiresAt: '',
});

const workModeOptions = Object.entries(workModeLabels).map(([value, label]) => ({
  label,
  value: parseInt(value) as WorkMode,
}));

const contractTypeOptions = Object.entries(contractTypeLabels).map(([value, label]) => ({
  label,
  value: parseInt(value) as ContractType,
}));

const statusOptions = [
  { label: 'Brouillon', value: JobOfferStatus.Draft },
  { label: 'Publiée', value: JobOfferStatus.Published },
  { label: 'Fermée', value: JobOfferStatus.Closed },
];

watch(
  () => props.job,
  (newJob) => {
    if (newJob) {
      formData.value = {
        title: newJob.title || '',
        description: newJob.description || '',
        requirements: newJob.requirements || '',
        location: newJob.location || '',
        workMode: newJob.workMode || WorkMode.OnSite,
        contractType: newJob.contractType || ContractType.FullTime,
        salaryMin: newJob.salaryMin,
        salaryMax: newJob.salaryMax,
        salaryCurrency: newJob.salaryCurrency || 'EUR',
        status: newJob.status || JobOfferStatus.Draft,
        expiresAt: newJob.expiresAt || '',
      };
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
  formData.value = {
    title: '',
    description: '',
    requirements: '',
    location: '',
    workMode: WorkMode.OnSite,
    contractType: ContractType.FullTime,
    salaryMin: undefined,
    salaryMax: undefined,
    salaryCurrency: 'EUR',
    status: JobOfferStatus.Draft,
    expiresAt: '',
  };
}

function closeDialog() {
  isOpen.value = false;
  resetForm();
}

async function saveJob() {
  saving.value = true;
  try {
    await new Promise((resolve) => setTimeout(resolve, 1000));

    console.log('Saving job:', formData.value);

    emit('saved');
    closeDialog();
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.job-dialog {
  width: 100%;
  max-width: 800px;
}

@media (max-width: 768px) {
  .job-dialog {
    margin: 16px;
    max-width: calc(100vw - 32px);
  }

  .row .col-12 {
    padding: 0;
  }
}
</style>
