<template>
  <q-page class="application-details-page">
    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center items-center" style="height: 50vh">
      <q-spinner size="50px" color="primary" />
    </div>

    <!-- Error -->
    <q-banner v-else-if="hasError" class="text-white bg-negative">
      <template #avatar>
        <q-icon name="error" />
      </template>
      {{ error }}
      <template #action>
        <q-btn flat color="white" label="Retour" @click="goBack" />
      </template>
    </q-banner>

    <!-- Application Details -->
    <div v-else-if="currentApplication" class="application-details-content">
      <!-- Header -->
      <div class="application-header q-pa-lg">
        <div class="header-content">
          <div class="row items-center q-mb-md">
            <div class="col">
              <div class="breadcrumb-container q-pa-md">
                <div class="breadcrumb-content">
                  <q-breadcrumbs class="text-grey-7" active-color="primary">
                    <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
                    <q-breadcrumbs-el label="Mes candidatures" icon="work" to="/applications" />
                    <q-breadcrumbs-el
                      :label="currentApplication.jobOfferTitle"
                      icon="description"
                    />
                  </q-breadcrumbs>
                </div>
              </div>

              <p class="text-h5 q-my-sm q-mx-md text-grey-2">
                {{ currentApplication.organizationName }}
              </p>
            </div>
          </div>

          <div class="application-meta-header row q-gutter-md items-center">
            <q-chip
              v-if="statusConfig"
              :color="statusConfig.color"
              :text-color="statusConfig.textColor"
              :icon="statusConfig.icon"
              size="md"
            >
              {{ statusConfig.label }}
            </q-chip>

            <q-chip color="white" text-color="primary" icon="access_time">
              Candidaté {{ formatDate(currentApplication.appliedAt) }}
            </q-chip>

            <q-chip v-if="currentApplication.rating" color="white" text-color="amber" icon="star">
              {{ currentApplication.rating }}/5
            </q-chip>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="application-content-container">
        <div class="application-content">
          <div class="content-grid q-pa-lg">
            <!-- Main Content -->
            <div class="main-content">
              <!-- Cover Letter -->
              <q-card v-if="currentApplication.coverLetter" class="q-mb-lg">
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">
                    <q-icon name="email" class="q-mr-sm" />
                    Ma lettre de motivation
                  </h6>
                  <div class="cover-letter-content">
                    {{ currentApplication.coverLetter }}
                  </div>
                  <div class="q-mt-md">
                    <q-btn
                      flat
                      color="primary"
                      icon="edit"
                      label="Modifier"
                      :disable="!canEdit"
                      @click="editCoverLetter"
                    />
                  </div>
                </q-card-section>
              </q-card>

              <!-- Additional Notes -->
              <q-card v-if="currentApplication.additionalNotes" class="q-mb-lg">
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">
                    <q-icon name="notes" class="q-mr-sm" />
                    Notes additionnelles
                  </h6>
                  <div class="notes-content">
                    {{ currentApplication.additionalNotes }}
                  </div>
                </q-card-section>
              </q-card>

              <!-- Status History -->
              <q-card class="q-mb-lg">
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">
                    <q-icon name="history" class="q-mr-sm" />
                    Historique de la candidature
                  </h6>
                  <q-timeline color="primary">
                    <q-timeline-entry
                      v-for="historyItem in sortedHistory"
                      :key="historyItem.id"
                      :color="getTimelineColor(historyItem.status)"
                      :icon="getTimelineIcon(historyItem.status)"
                      :title="historyItem.statusDisplayName"
                      :subtitle="formatDate(historyItem.updatedAt)"
                    >
                      <div v-if="historyItem.comment" class="timeline-comment q-mt-sm">
                        <p class="text-body2 q-mb-none">{{ historyItem.comment }}</p>
                      </div>

                      <div v-if="historyItem.hasRating" class="timeline-rating q-mt-sm">
                        <div class="row items-center">
                          <q-rating
                            v-model="historyItem.rating!"
                            readonly
                            size="18px"
                            color="amber"
                          />
                          <span class="q-ml-sm text-caption">{{
                            historyItem.ratingDescription
                          }}</span>
                        </div>
                      </div>

                      <div class="timeline-author text-caption text-grey-6 q-mt-sm">
                        Par {{ historyItem.updatedByUserName }}
                      </div>
                    </q-timeline-entry>
                  </q-timeline>
                </q-card-section>
              </q-card>
            </div>

            <!-- Sidebar -->
            <div class="sidebar">
              <!-- Actions Card -->
              <q-card class="actions-card q-mb-lg">
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">Actions</h6>

                  <div class="q-gutter-sm">
                    <q-btn
                      v-if="canWithdraw"
                      color="negative"
                      outline
                      icon="cancel"
                      label="Retirer ma candidature"
                      class="full-width"
                      @click="confirmWithdraw"
                    />

                    <q-btn
                      v-if="canEdit"
                      color="primary"
                      outline
                      icon="edit"
                      label="Modifier ma candidature"
                      class="full-width"
                      @click="editApplication"
                    />

                    <q-btn
                      flat
                      icon="content_copy"
                      label="Copier l'ID"
                      class="full-width"
                      @click="copyApplicationId"
                    />

                    <q-btn
                      flat
                      icon="download"
                      label="Télécharger"
                      class="full-width"
                      @click="downloadApplication"
                    />
                  </div>
                </q-card-section>
              </q-card>

              <!-- Application Info -->
              <q-card class="q-mb-lg">
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">Informations</h6>

                  <div class="info-item q-mb-md">
                    <div class="text-weight-medium">ID de candidature</div>
                    <div class="text-grey-8 text-mono">
                      {{ currentApplication.id.substring(0, 18) }}...
                    </div>
                  </div>

                  <div class="info-item q-mb-md">
                    <div class="text-weight-medium">Date de candidature</div>
                    <div>{{ formatFullDate(currentApplication.appliedAt) }}</div>
                  </div>

                  <div v-if="currentApplication.lastUpdatedAt" class="info-item q-mb-md">
                    <div class="text-weight-medium">Dernière mise à jour</div>
                    <div>{{ formatFullDate(currentApplication.lastUpdatedAt) }}</div>
                  </div>

                  <div class="info-item q-mb-md">
                    <div class="text-weight-medium">Statut actuel</div>
                    <q-chip
                      v-if="statusConfig"
                      :color="statusConfig.color"
                      :text-color="statusConfig.textColor"
                      size="sm"
                      :icon="statusConfig.icon"
                    >
                      {{ statusConfig.label }}
                    </q-chip>
                  </div>

                  <div v-if="currentApplication.rating" class="info-item">
                    <div class="text-weight-medium">Évaluation</div>
                    <div class="row items-center">
                      <q-rating
                        v-model="currentApplication.rating"
                        readonly
                        size="18px"
                        color="amber"
                      />
                      <span class="q-ml-sm">{{ currentApplication.rating }}/5</span>
                    </div>
                  </div>
                </q-card-section>
              </q-card>

              <!-- Contact Info -->
              <q-card
                v-if="
                  currentApplication.assignedManagerName ||
                  currentApplication.assignedTechnicalEvaluatorName
                "
              >
                <q-card-section>
                  <h6 class="text-h6 q-mt-none q-mb-md">Contacts</h6>

                  <div v-if="currentApplication.assignedManagerName" class="contact-item q-mb-md">
                    <div class="text-weight-medium">Manager assigné</div>
                    <div class="text-grey-8">{{ currentApplication.assignedManagerName }}</div>
                  </div>

                  <div
                    v-if="currentApplication.assignedTechnicalEvaluatorName"
                    class="contact-item"
                  >
                    <div class="text-weight-medium">Évaluateur technique</div>
                    <div class="text-grey-8">
                      {{ currentApplication.assignedTechnicalEvaluatorName }}
                    </div>
                  </div>
                </q-card-section>
              </q-card>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Withdraw Confirmation Dialog -->
    <q-dialog v-model="showWithdrawDialog" persistent>
      <q-card style="min-width: 350px">
        <q-card-section>
          <div class="text-h6">Retirer la candidature</div>
        </q-card-section>

        <q-card-section class="q-pt-none">
          <p>Êtes-vous sûr de vouloir retirer cette candidature ?</p>
          <p class="text-caption text-grey-7">
            Cette action est irréversible. Vous ne pourrez plus candidater à cette offre.
          </p>

          <q-input
            v-model="withdrawReason"
            type="textarea"
            label="Raison (optionnelle)"
            filled
            rows="3"
            placeholder="Ex: J'ai trouvé un autre poste, l'offre ne correspond plus à mes attentes..."
          />
        </q-card-section>

        <q-card-actions align="right" class="text-primary">
          <q-btn flat label="Annuler" @click="showWithdrawDialog = false" />
          <q-btn
            flat
            label="Confirmer le retrait"
            :loading="withdrawLoading"
            color="negative"
            @click="handleWithdraw"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Edit Application Dialog -->
    <edit-application-dialog
      v-model="showEditDialog"
      :application="currentApplication"
      @updated="handleApplicationUpdated"
    />
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { copyToClipboard } from 'quasar';
import { formatDate, formatFullDate } from 'src/helpers/DateHelper';
import { storeToRefs } from 'pinia';
import { useApplicationStore } from '../stores/applicationStore';
import { useNotification } from '../composables/notification';
import { applicationStatusConfig } from '../models/application';
import { ApplicationStatus } from '../enums';
import EditApplicationDialog from '../components/EditApplicationDialog.vue';

// Composables
const route = useRoute();
const router = useRouter();
const { showSuccessNotification, showErrorNotification, showInfoNotification } = useNotification();

// Store
const applicationStore = useApplicationStore();
const { currentApplication, isLoading, error, hasError } = storeToRefs(applicationStore);

// State
const showWithdrawDialog = ref(false);
const showEditDialog = ref(false);
const withdrawReason = ref('');
const withdrawLoading = ref(false);

// Computed
const statusConfig = computed(() =>
  currentApplication.value ? applicationStatusConfig[currentApplication.value.currentStatus] : null,
);

const canWithdraw = computed(() => {
  if (!currentApplication.value) return false;
  return (
    currentApplication.value.isActive &&
    currentApplication.value.currentStatus !== ApplicationStatus.Withdrawn &&
    currentApplication.value.currentStatus !== ApplicationStatus.Accepted &&
    currentApplication.value.currentStatus !== ApplicationStatus.Rejected
  );
});

const canEdit = computed(() => {
  if (!currentApplication.value) return false;
  return (
    currentApplication.value.isActive &&
    currentApplication.value.currentStatus === ApplicationStatus.Applied
  );
});

const sortedHistory = computed(() => {
  if (!currentApplication.value?.statusHistory) return [];
  return [...currentApplication.value.statusHistory].sort(
    (a, b) => new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime(),
  );
});

// Methods
const loadApplication = async () => {
  const applicationId = route.params.id as string;
  await applicationStore.fetchApplicationById(applicationId);
};

const goBack = () => {
  router.push('/applications');
};

const getTimelineColor = (status: ApplicationStatus): string => {
  return applicationStatusConfig[status]?.color || 'grey';
};

const getTimelineIcon = (status: ApplicationStatus): string => {
  return applicationStatusConfig[status]?.icon || 'help';
};

const confirmWithdraw = () => {
  withdrawReason.value = '';
  showWithdrawDialog.value = true;
};

const handleWithdraw = async () => {
  if (!currentApplication.value) return;

  withdrawLoading.value = true;

  await applicationStore.withdrawApplication(
    currentApplication.value.id,
    withdrawReason.value || 'Aucune raison spécifiée',
  );

  showWithdrawDialog.value = false;
  // Reload to update the status
  await loadApplication();
};

const editApplication = () => {
  showEditDialog.value = true;
};

const editCoverLetter = () => {
  showEditDialog.value = true;
};

const handleApplicationUpdated = async () => {
  showEditDialog.value = false;
  await loadApplication();

  //  showSuccessNotification('Candidature mise à jour avec succès');
};

const copyApplicationId = async () => {
  if (!currentApplication.value) return;

  try {
    await copyToClipboard(currentApplication.value.id);
    showSuccessNotification('ID de candidature copié');
  } catch (error: any) {
    console.log(error.message);
    showErrorNotification('Erreur lors de la copie');
  }
};

const downloadApplication = () => {
  // TODO: Implement download functionality
  showInfoNotification('Fonctionnalité de téléchargement bientôt disponible');
};

// Lifecycle
onMounted(async () => {
  await loadApplication();
});
</script>

<style lang="scss" scoped>
.application-details-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.breadcrumb-container {
  background: white;
  border-bottom: 1px solid #e0e0e0;
  width: fit-content;
  border-radius: 22px;
}

.breadcrumb-content {
  max-width: 1400px;
  margin: 0 auto;
}

.application-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.header-content {
  max-width: 1400px;
  margin: 0 auto;
}

.application-content-container {
  width: 100%;
  background-color: rgb(233, 233, 233);
  padding: 0;
}

.application-content {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
}

.content-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;

  @media (min-width: 992px) {
    grid-template-columns: 2fr 1fr;
  }
}

.main-content {
  min-width: 0; // Prevent overflow
}

.sidebar {
  min-width: 0; // Prevent overflow

  @media (min-width: 992px) {
    position: sticky;
    top: 20px;
    height: fit-content;
  }
}

.cover-letter-content,
.notes-content {
  line-height: 1.6;
  white-space: pre-wrap;
  background: #f8f9fa;
  padding: 16px;
  border-radius: 8px;
  border-left: 4px solid var(--q-primary);
}

.timeline-comment {
  background: #f8f9fa;
  padding: 8px 12px;
  border-radius: 6px;
  border-left: 3px solid #e0e0e0;
}

.timeline-rating,
.timeline-author {
  margin-left: 16px;
}

.actions-card {
  border: 2px solid var(--q-primary);
  border-radius: 12px;
}

.info-item,
.contact-item {
  padding-bottom: 8px;
  border-bottom: 1px solid #f0f0f0;

  &:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }
}

.text-mono {
  font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 0.9em;
}
</style>
