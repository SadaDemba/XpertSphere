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
                aria-label="Salaire minimum"
              />
            </div>
            <div class="col-12 col-md-4">
              <q-input
                v-model="formData.salaryMax"
                outlined
                type="number"
                label="Salaire maximum"
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
              <div class="editor-group">
                <label class="editor-label" for="description-editor">
                  Description du poste *
                </label>
                <q-editor
                  id="description-editor"
                  v-model="formData.description"
                  :toolbar="editorToolbar"
                  height="200px"
                  placeholder="Décrivez le poste, les missions, l'environnement de travail..."
                  class="editor-field"
                  aria-label="Description détaillée du poste"
                />
              </div>
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <div class="editor-group">
                <label class="editor-label" for="requirements-editor">
                  Exigences et qualifications *
                </label>
                <q-editor
                  id="requirements-editor"
                  v-model="formData.requirements"
                  :toolbar="editorToolbar"
                  height="150px"
                  placeholder="Listez les compétences requises, l'expérience nécessaire, les diplômes..."
                  class="editor-field"
                  aria-label="Exigences et qualifications requises"
                />
              </div>
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <div class="editor-group">
                <label class="editor-label" for="benefits-editor"> Avantages et bénéfices * </label>
                <q-editor
                  id="benefits-editor"
                  v-model="formData.benefits"
                  :toolbar="editorToolbar"
                  height="120px"
                  placeholder="Décrivez les avantages : mutuelle, tickets resto, télétravail, formation..."
                  class="editor-field"
                  aria-label="Avantages et bénéfices offerts"
                />
              </div>
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
import { useNotification } from 'src/composables/notification';

const notification = useNotification();
const jobOfferStore = useJobOfferStore();

// Editor toolbar configuration
const editorToolbar = [
  ['bold', 'italic', 'underline', 'strike'],
  ['unordered', 'ordered'],
  [
    {
      label: 'Taille',
      icon: 'format_size',
      list: 'no-icons',
      options: ['size-1', 'size-2', 'size-3', 'size-4', 'size-5', 'size-6'],
    },
  ],
  ['quote', 'link'],
  ['fullscreen'],
];

interface JobFormData {
  title: string;
  description: string;
  requirements: string;
  benefits: string;
  location: string;
  workMode: WorkMode;
  contractType: ContractType;
  salaryMin: number | undefined;
  salaryMax: number | undefined;
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
  benefits: '',
  location: '',
  workMode: WorkMode.OnSite,
  contractType: ContractType.FullTime,
  salaryMin: undefined,
  salaryMax: undefined,
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
        benefits: newJob.benefits || '',
        location: newJob.location || '',
        workMode: newJob.workMode || WorkMode.OnSite,
        contractType: newJob.contractType || ContractType.FullTime,
        salaryMin: newJob.salaryMin,
        salaryMax: newJob.salaryMax,
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
    benefits: '',
    location: '',
    workMode: WorkMode.OnSite,
    contractType: ContractType.FullTime,
    salaryMin: undefined,
    salaryMax: undefined,
    expiresAt: '',
  };
}

function closeDialog() {
  isOpen.value = false;
  resetForm();
}

async function saveJob() {
  // Validation des champs obligatoires
  if (!formData.value.title.trim()) {
    notification.showErrorNotification('Le titre est obligatoire');
    return;
  }
  if (!formData.value.description.trim()) {
    notification.showErrorNotification('La description est obligatoire');
    return;
  }
  if (!formData.value.requirements.trim()) {
    notification.showErrorNotification('Les exigences sont obligatoires');
    return;
  }
  if (!formData.value.benefits.trim()) {
    notification.showErrorNotification('Les avantages sont obligatoires');
    return;
  }

  saving.value = true;
  try {
    let result;

    if (isEditing.value && props.job) {
      // Mode édition : appeler updateJobOffer (ne met à jour que le contenu, pas le statut)
      const updateData: UpdateJobOfferDto = {
        title: formData.value.title,
        description: formData.value.description,
        requirements: formData.value.requirements,
        benefits: formData.value.benefits,
        location: formData.value.location,
        workMode: formData.value.workMode,
        contractType: formData.value.contractType,
        salaryMin: formData.value.salaryMin,
        salaryMax: formData.value.salaryMax,
        expiresAt: formData.value.expiresAt || undefined,
      };

      result = await jobOfferStore.updateJobOffer(props.job.id, updateData);

      if (result) {
        notification.showSuccessNotification("Offre d'emploi mise à jour avec succès");
      }
    } else {
      // Mode création : appeler createJobOffer
      const createData: CreateJobOfferDto = {
        title: formData.value.title,
        description: formData.value.description,
        requirements: formData.value.requirements,
        benefits: formData.value.benefits,
        location: formData.value.location,
        workMode: formData.value.workMode,
        contractType: formData.value.contractType,
        salaryMin: formData.value.salaryMin,
        salaryMax: formData.value.salaryMax,
        expiresAt: formData.value.expiresAt || undefined,
      };

      result = await jobOfferStore.createJobOffer(createData);

      if (result) {
        notification.showSuccessNotification("Offre d'emploi créée avec succès");
      }
    }

    if (result) {
      emit('saved');
      closeDialog();
    } else {
      notification.showErrorNotification(
        `Erreur lors de ${isEditing.value ? 'la mise à jour' : 'la création'} de l'offre`,
      );
    }
  } catch (error) {
    console.error('Erreur lors de la sauvegarde:', error);
    notification.showErrorNotification(
      `Erreur: ${error instanceof Error ? error.message : 'Une erreur est survenue'}`,
    );
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

.editor-group {
  margin-bottom: 16px;
}

.editor-label {
  display: block;
  font-size: 14px;
  font-weight: 500;
  color: #1f2937;
  margin-bottom: 8px;
}

.editor-field {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  overflow: hidden;
}

.editor-field:focus-within {
  border-color: var(--q-primary);
  box-shadow: 0 0 0 2px rgba(var(--q-primary-rgb), 0.2);
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
