<template>
  <q-page class="job-detail-page">
    <!-- Breadcrumbs -->
    <div class="breadcrumb-container q-pa-md q-mb-lg">
      <div class="breadcrumb-content">
        <q-breadcrumbs class="text-grey-7" active-color="primary">
          <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
          <q-breadcrumbs-el label="Offres d'emploi" icon="work" to="/jobs" />
          <q-breadcrumbs-el v-if="jobOffer" :label="jobOffer.title" icon="description" />
        </q-breadcrumbs>
      </div>
    </div>

    <div v-if="jobStore.isLoading" class="row justify-center q-mt-lg">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <div v-else-if="jobStore.hasError" class="row justify-center q-mt-lg">
      <q-card class="q-pa-md">
        <q-card-section>
          <div class="text-h6 text-negative">Erreur</div>
          <p>{{ jobStore.errorMessage }}</p>
        </q-card-section>
      </q-card>
    </div>

    <div v-else-if="jobOffer" class="job-content q-pa-lg">
      <!-- Header -->
      <q-card class="job-header-card q-mb-lg">
        <q-card-section>
          <div class="row items-center justify-between">
            <div>
              <h4 class="text-h4 q-my-none">{{ jobOffer.title }}</h4>
              <p class="text-h6 text-grey-7 q-my-sm">{{ jobOffer.organizationName }}</p>

              <div class="row q-gutter-sm q-mt-md">
                <q-chip
                  :color="jobStatusConfig[jobOffer.status]?.color"
                  :text-color="jobStatusConfig[jobOffer.status]?.textColor"
                  :icon="jobStatusConfig[jobOffer.status]?.icon"
                  size="md"
                >
                  {{ jobStatusConfig[jobOffer.status]?.label }}
                </q-chip>

                <q-chip
                  v-if="jobOffer.location"
                  color="blue-1"
                  text-color="blue-10"
                  icon="location_on"
                  size="md"
                >
                  {{ jobOffer.location }}
                </q-chip>

                <q-chip color="green-1" text-color="green-10" icon="business_center" size="md">
                  {{ contractTypeLabels[jobOffer.contractType] }}
                </q-chip>

                <q-chip color="purple-1" text-color="purple-10" icon="home_work" size="md">
                  {{ workModeLabels[jobOffer.workMode] }}
                </q-chip>
              </div>
            </div>

            <div class="row q-gutter-sm">
              <q-btn
                v-if="!editMode"
                outline
                color="primary"
                icon="edit"
                label="Modifier"
                @click="toggleEditMode"
              />
              <q-btn
                v-else
                unelevated
                color="positive"
                icon="save"
                label="Enregistrer"
                :loading="jobStore.isLoading"
                @click="saveChanges"
              />
              <q-btn
                v-if="editMode"
                flat
                color="negative"
                icon="cancel"
                label="Annuler"
                @click="cancelEdit"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>

      <!-- Main Content -->
      <div class="row q-col-gutter-lg">
        <!-- Left Column - Job Details -->
        <div class="col-12 col-md-8">
          <!-- Description -->
          <q-card class="info-card q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="description" color="primary" size="sm" class="q-mr-sm" />
                Description du poste
              </div>
              <div v-if="!editMode" class="info-content q-pa-md">
                <!-- eslint-disable-next-line vue/no-v-html -->
                <div class="text-body1" v-html="sanitizedDescription"></div>
              </div>
              <div v-else class="info-content q-pa-md">
                <q-editor
                  :model-value="editedJob.description || ''"
                  :toolbar="editorToolbar"
                  min-height="200px"
                  @update:model-value="(val) => (editedJob.description = val)"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Requirements -->
          <q-card class="info-card q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="checklist" color="primary" size="sm" class="q-mr-sm" />
                Exigences du poste
              </div>
              <div v-if="!editMode" class="info-content q-pa-md">
                <!-- eslint-disable-next-line vue/no-v-html -->
                <div class="text-body1" v-html="sanitizedRequirements"></div>
              </div>
              <div v-else class="info-content q-pa-md">
                <q-editor
                  :model-value="editedJob.requirements || ''"
                  :toolbar="editorToolbar"
                  min-height="150px"
                  @update:model-value="(val) => (editedJob.requirements = val)"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Benefits -->
          <q-card class="info-card">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="card_giftcard" color="primary" size="sm" class="q-mr-sm" />
                Avantages et bénéfices
              </div>
              <div v-if="!editMode" class="info-content q-pa-md">
                <!-- eslint-disable-next-line vue/no-v-html -->
                <div class="text-body1" v-html="sanitizedBenefits"></div>
              </div>
              <div v-else class="info-content q-pa-md">
                <q-editor
                  :model-value="editedJob.benefits || ''"
                  :toolbar="editorToolbar"
                  min-height="150px"
                  placeholder="Décrivez les avantages : mutuelle, tickets resto, télétravail, formation..."
                  @update:model-value="(val) => (editedJob.benefits = val)"
                />
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Right Column - Additional Info -->
        <div class="col-12 col-md-4">
          <!-- Job Information -->
          <q-card class="info-card q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="info" color="primary" size="sm" class="q-mr-sm" />
                Informations du poste
              </div>
              <div class="info-content q-pa-md">
                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Organisation</div>
                  <div class="text-subtitle2">{{ jobOffer.organizationName }}</div>
                </div>

                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Localisation</div>
                  <div v-if="!editMode" class="text-subtitle2">
                    {{ jobOffer.location || 'Non spécifié' }}
                  </div>
                  <q-input v-else v-model="editedJob.location" dense outlined />
                </div>

                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Type de contrat</div>
                  <div v-if="!editMode" class="text-subtitle2">
                    {{ contractTypeLabels[jobOffer.contractType] }}
                  </div>
                  <q-select
                    v-else
                    v-model="editedJob.contractType"
                    :options="contractTypeSelectOptions"
                    dense
                    outlined
                    emit-value
                    map-options
                  />
                </div>

                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Mode de travail</div>
                  <div v-if="!editMode" class="text-subtitle2">
                    {{ workModeLabels[jobOffer.workMode] }}
                  </div>
                  <q-select
                    v-else
                    v-model="editedJob.workMode"
                    :options="workModeSelectOptions"
                    dense
                    outlined
                    emit-value
                    map-options
                  />
                </div>

                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Salaire</div>
                  <div v-if="!editMode" class="text-subtitle2">
                    {{
                      formatSalary(jobOffer.salaryMin, jobOffer.salaryMax, jobOffer.salaryCurrency)
                    }}
                  </div>
                  <div v-else>
                    <div class="row q-gutter-sm">
                      <q-input
                        v-model.number="editedJob.salaryMin"
                        type="number"
                        dense
                        outlined
                        label="Min"
                        class="col"
                      />
                      <q-input
                        v-model.number="editedJob.salaryMax"
                        type="number"
                        dense
                        outlined
                        label="Max"
                        class="col"
                      />
                    </div>
                    <!-- La devise n'est plus modifiable : stampée par le backend à la création,
                         figée pour la durée de vie de l'offre (voir
                         configurable-salary-currency.md, décision 3). -->
                    <div class="q-mt-sm text-caption text-grey-7">
                      Devise : <span class="text-weight-medium">{{ jobOffer.salaryCurrency }}</span>
                    </div>
                  </div>
                </div>

                <div class="info-group">
                  <div class="text-caption text-grey-7">Date de création</div>
                  <div class="text-subtitle2">
                    {{ formatDate(jobOffer.createdAt) }}
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Dates -->
          <q-card class="info-card q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="event" color="primary" size="sm" class="q-mr-sm" />
                Dates importantes
              </div>
              <div class="info-content q-pa-md">
                <div class="info-group q-mb-md">
                  <div class="text-caption text-grey-7">Date de publication</div>
                  <div class="text-subtitle2">
                    {{ formatDate(jobOffer.publishedAt) }}
                  </div>
                </div>

                <div class="info-group">
                  <div class="text-caption text-grey-7">Date d'expiration</div>
                  <div v-if="!editMode" class="text-subtitle2">
                    {{ formatDate(jobOffer.expiresAt) }}
                  </div>
                  <q-input v-else v-model="editedJob.expiresAt" type="date" dense outlined />
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Actions -->
          <q-card class="info-card">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="flash_on" color="primary" size="sm" class="q-mr-sm" />
                Actions rapides
              </div>
              <div class="q-gutter-sm">
                <q-btn
                  unelevated
                  color="primary"
                  icon="people"
                  label="Voir les candidatures"
                  class="full-width"
                  @click="viewApplications"
                />
                <q-btn
                  outline
                  color="secondary"
                  icon="share"
                  label="Partager l'offre"
                  class="full-width"
                  @click="shareJob"
                />
                <q-btn
                  outline
                  color="info"
                  icon="content_copy"
                  label="Dupliquer l'offre"
                  class="full-width"
                  @click="duplicateJob"
                />
                <q-separator class="q-my-md" />
                <q-btn
                  flat
                  color="negative"
                  icon="delete"
                  label="Supprimer l'offre"
                  class="full-width"
                  @click="confirmDelete"
                />
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>

    <div v-else class="text-center q-pa-lg">
      <q-icon name="work_off" size="xl" color="grey-5" />
      <div class="text-h6 text-grey-6 q-mt-md">Offre d'emploi non trouvée</div>
      <p class="text-grey-6">L'offre d'emploi demandée n'existe pas ou n'est plus disponible.</p>
      <q-btn
        outline
        color="primary"
        icon="arrow_back"
        label="Retour aux offres"
        to="/jobs"
        class="q-mt-md"
      />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { date } from 'quasar';
import { useJobOfferStore } from 'src/stores/jobOfferStore';
import { useNotification } from 'src/composables/notification';
import { useDialog } from 'src/composables/dialog';
import { SanitizerService } from 'src/services/sanitizer';
import type { JobOfferDto, CreateJobOfferDto } from '../../models/job';
import { jobStatusConfig } from '../../models/job';
import { contractTypeLabels } from '../../enums/ContractType';
import { workModeLabels } from '../../enums/WorkMode';

const route = useRoute();
const router = useRouter();
const notification = useNotification();
const dialog = useDialog();
const jobStore = useJobOfferStore();

const jobOffer = ref<JobOfferDto | null>(null);
const editMode = ref(false);
const editedJob = reactive<Partial<JobOfferDto>>({});

const contractTypeSelectOptions = Object.entries(contractTypeLabels).map(([value, label]) => ({
  label,
  value: Number(value),
}));

const workModeSelectOptions = Object.entries(workModeLabels).map(([value, label]) => ({
  label,
  value: Number(value),
}));

// Sanitized content using the centralized sanitizer service
const sanitizedDescription = computed(() => {
  return SanitizerService.sanitizeRichText(jobOffer.value?.description || '');
});

const sanitizedRequirements = computed(() => {
  return SanitizerService.sanitizeRichText(jobOffer.value?.requirements || '');
});

const sanitizedBenefits = computed(() => {
  return SanitizerService.sanitizeRichText(jobOffer.value?.benefits || '');
});

const editorToolbar = [
  ['bold', 'italic', 'underline', 'strike'],
  ['unordered', 'ordered'],
  [
    {
      label: 'Taille',
      icon: 'format_size',
      list: 'no-icons',
      options: ['size-1', 'size-2', 'size-3', 'size-4', 'size-5', 'size-6', 'size-7'],
    },
  ],
  ['quote', 'link'],
  ['fullscreen'],
];

const formatDate = (dateString: string | null | undefined) => {
  if (!dateString) return 'Non définie';
  return date.formatDate(dateString, 'DD/MM/YYYY');
};

const formatSalary = (min?: number, max?: number, currency?: string) => {
  if (!min && !max) return 'Non spécifié';
  const curr = currency || 'EUR';
  if (min && max) {
    return `${min.toLocaleString()} - ${max.toLocaleString()} ${curr}`;
  } else if (min) {
    return `À partir de ${min.toLocaleString()} ${curr}`;
  } else {
    return `Jusqu'à ${max?.toLocaleString()} ${curr}`;
  }
};

const toggleEditMode = () => {
  if (!editMode.value && jobOffer.value) {
    Object.assign(editedJob, jobOffer.value);
    // <input type="date"> exige strictement le format YYYY-MM-DD : la valeur ISO
    // retournée par le backend (ex. "2026-07-25T00:00:00") est silencieusement
    // rejetée par le navigateur sans cette conversion (voir bug 3 de la spec).
    if (editedJob.expiresAt) {
      editedJob.expiresAt = date.formatDate(editedJob.expiresAt, 'YYYY-MM-DD');
    } else {
      delete editedJob.expiresAt;
    }
  }
  editMode.value = !editMode.value;
};

const cancelEdit = () => {
  editMode.value = false;
  Object.keys(editedJob).forEach((key) => delete editedJob[key as keyof JobOfferDto]);
};

const saveChanges = async () => {
  if (!jobOffer.value) return;

  await jobStore.updateJobOffer(jobOffer.value.id, editedJob);
  jobOffer.value = { ...jobOffer.value, ...editedJob };
  editMode.value = false;
};

const viewApplications = () => {
  router.push(`/applications?jobId=${jobOffer.value?.id}`);
};

const shareJob = () => {
  if (jobOffer.value) {
    const url = `${window.location.origin}/jobs/${jobOffer.value.id}`;
    navigator.clipboard.writeText(url);
    notification.showSuccessNotification('Lien copié dans le presse-papier');
  }
};

const duplicateJob = async () => {
  if (!jobOffer.value) return;

  const duplicated: Partial<JobOfferDto> = {
    ...jobOffer.value,
    title: `${jobOffer.value.title} (Copie)`,
  };
  delete duplicated.id;
  await jobStore.createJobOffer(duplicated as CreateJobOfferDto);
  notification.showSuccessNotification('Offre dupliquée avec succès');
  router.push('/jobs');
};

const confirmDelete = async () => {
  if (!jobOffer.value) return;

  try {
    await dialog.confirmDelete(jobOffer.value.title, 'offre');
  } catch {
    // Annulé par l'utilisateur — ne rien faire
    return;
  }

  const success = await jobStore.deleteJobOffer(jobOffer.value.id);
  if (success) {
    notification.showSuccessNotification('Offre supprimée avec succès');
    router.push('/jobs');
  }
};

onMounted(async () => {
  const jobId = route.params.id as string;
  if (jobId) {
    try {
      await jobStore.fetchJobOfferById(jobId);
      jobOffer.value = jobStore.currentJobOffer;
    } catch (error) {
      console.error('Error fetching job offer:', error);
    }
  }
});
</script>

<style lang="scss" scoped>
.job-detail-page {
  background-color: #f5f5f5;
  min-height: 100vh;
}

.breadcrumb-container {
  background: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.breadcrumb-content {
  max-width: 1200px;
  margin: 0 auto;
}

.job-content {
  max-width: 1200px;
  margin: 0 auto;
}

.job-header-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.info-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
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
}

.info-group {
  padding: 8px 0;
}
</style>
