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
                emit-value
                map-options
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
                emit-value
                map-options
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
import type { JobOffer, CreateJobOfferDto, UpdateJobOfferDto } from '../../models/job';
import { WorkMode, ContractType } from '../../enums';
import { workModeLabels, contractTypeLabels } from '../../models/job';
import { useJobOfferStore } from '../../stores/jobOfferStore';
import { useQuasar } from 'quasar';

const $q = useQuasar();
const jobOfferStore = useJobOfferStore();

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
    let result;

    if (isEditing.value && props.job) {
      // Mode édition : appeler updateJobOffer (ne met à jour que le contenu, pas le statut)
      const updateData: UpdateJobOfferDto = {
        title: formData.value.title,
        description: formData.value.description,
        requirements: formData.value.requirements,
        location: formData.value.location,
        workMode: formData.value.workMode,
        contractType: formData.value.contractType,
        salaryMin: formData.value.salaryMin,
        salaryMax: formData.value.salaryMax,
        salaryCurrency: formData.value.salaryCurrency,
        expiresAt: formData.value.expiresAt || undefined,
      };

      result = await jobOfferStore.updateJobOffer(props.job.id, updateData);

      if (result) {
        $q.notify({
          type: 'positive',
          message: "Offre d'emploi mise à jour avec succès",
          position: 'top',
        });
      }
    } else {
      // Mode création : appeler createJobOffer
      const createData: CreateJobOfferDto = {
        title: formData.value.title,
        description: formData.value.description,
        requirements: formData.value.requirements,
        location: formData.value.location,
        workMode: formData.value.workMode,
        contractType: formData.value.contractType,
        salaryMin: formData.value.salaryMin,
        salaryMax: formData.value.salaryMax,
        salaryCurrency: formData.value.salaryCurrency,
        expiresAt: formData.value.expiresAt || undefined,
      };

      result = await jobOfferStore.createJobOffer(createData);

      if (result) {
        $q.notify({
          type: 'positive',
          message: "Offre d'emploi créée avec succès",
          position: 'top',
        });
      }
    }

    if (result) {
      emit('saved');
      closeDialog();
    } else {
      $q.notify({
        type: 'negative',
        message: `Erreur lors de ${isEditing.value ? 'la mise à jour' : 'la création'} de l'offre`,
        position: 'top',
      });
    }
  } catch (error) {
    console.error('Erreur lors de la sauvegarde:', error);
    $q.notify({
      type: 'negative',
      message: `Erreur: ${error instanceof Error ? error.message : 'Une erreur est survenue'}`,
      position: 'top',
    });
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
