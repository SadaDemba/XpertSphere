<template>
  <q-card class="application-card" @click="handleCardClick">
    <q-card-section>
      <div class="row items-start justify-between q-mb-md">
        <div class="col">
          <h6 class="application-title q-mt-none q-mb-sm">{{ application.jobOfferTitle }}</h6>
          <p class="company-name text-primary q-mb-sm">{{ application.organizationName }}</p>
        </div>
        <div class="col-auto">
          <q-chip
            :color="statusConfig!.color"
            :text-color="statusConfig!.textColor"
            :icon="statusConfig!.icon"
            size="sm"
          >
            {{ statusConfig!.label }}
          </q-chip>
        </div>
      </div>

      <div class="application-meta q-mb-md">
        <div class="row items-center q-gutter-md text-grey-7">
          <div class="flex items-center">
            <q-icon name="access_time" size="16px" class="q-mr-xs" />
            <span class="text-caption"
              >Candidature déposée <b>{{ formatDate(application.appliedAt) }}</b></span
            >
          </div>

          <div v-if="application.rating" class="flex items-center">
            <q-icon name="star" size="16px" class="q-mr-xs" color="amber" />
            <span class="text-caption">{{ application.rating }}/5</span>
          </div>
        </div>
      </div>

      <div v-if="application.coverLetter" class="cover-letter-preview q-mb-md">
        <p class="text-caption text-grey-7 q-mb-xs">Lettre de motivation :</p>
        <p class="text-body2 text-grey-8">{{ truncatedCoverLetter }}</p>
      </div>

      <!-- Progress Indicator -->
      <div v-if="application.isInProgress" class="progress-section q-mb-md">
        <div class="text-caption text-grey-7 q-mb-xs">Progression :</div>
        <q-linear-progress :value="progressValue" color="primary" size="8px" rounded />
        <div class="text-caption text-grey-7 q-mt-xs">
          {{ getProgressLabel() }}
        </div>
      </div>

      <!-- Status History Preview -->
      <div
        v-if="application.statusHistory && application.statusHistory.length > 0"
        class="history-preview q-mb-md"
      >
        <div class="text-caption text-grey-7 q-mb-xs">Historique récent :</div>
        <div class="row q-gutter-xs">
          <q-chip
            v-for="status in recentHistory"
            :key="status.id"
            size="xs"
            outline
            :color="getStatusColor(status.status)"
            :icon="getStatusIcon(status.status)"
          >
            {{ status.statusDisplayName }}
          </q-chip>
        </div>
      </div>

      <div class="application-actions">
        <div class="row justify-between items-center">
          <div v-if="application.lastUpdatedAt" class="flex items-center">
            <q-icon name="update" size="16px" class="q-mr-xs" />
            <span class="text-caption">Mis à jour {{ formatDate(application.lastUpdatedAt) }}</span>
          </div>

          <div class="action-buttons row q-gutter-xs">
            <q-btn
              flat
              round
              size="sm"
              icon="visibility"
              color="primary"
              @click.stop="$emit('view', application.id)"
            >
              <q-tooltip>Consulter les détails</q-tooltip>
            </q-btn>

            <q-btn
              v-if="canWithdraw"
              flat
              round
              size="sm"
              icon="cancel"
              color="negative"
              @click.stop="$emit('withdraw', application.id)"
            >
              <q-tooltip>Retirer la candidature</q-tooltip>
            </q-btn>

            <q-btn flat round size="sm" icon="more_vert" color="grey-7" @click.stop>
              <q-tooltip>Plus d'options</q-tooltip>
              <q-menu>
                <q-list style="min-width: 150px">
                  <q-item v-close-popup clickable @click="copyApplicationId">
                    <q-item-section avatar>
                      <q-icon name="content_copy" />
                    </q-item-section>
                    <q-item-section>Copier l'ID</q-item-section>
                  </q-item>

                  <q-item v-close-popup clickable @click="downloadApplication">
                    <q-item-section avatar>
                      <q-icon name="download" />
                    </q-item-section>
                    <q-item-section>Télécharger</q-item-section>
                  </q-item>
                </q-list>
              </q-menu>
            </q-btn>
          </div>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { computed } from 'vue';
import { copyToClipboard } from 'quasar';
import { formatDate } from 'src/helpers/DateHelper';
import type { ApplicationDto } from '../models/application';
import { applicationStatusConfig } from '../models/application';
import { ApplicationStatus } from '../enums';
import { useNotification } from '../composables/notification';

interface Props {
  application: ApplicationDto;
}

interface Emits {
  (e: 'view', applicationId: string): void;
  (e: 'withdraw', applicationId: string): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// Composables
const { showSuccessNotification, showErrorNotification, showInfoNotification } = useNotification();

// Computed
const statusConfig = computed(() => applicationStatusConfig[props.application.currentStatus]);

const canWithdraw = computed(() => {
  return (
    props.application.isActive &&
    props.application.currentStatus !== ApplicationStatus.Withdrawn &&
    props.application.currentStatus !== ApplicationStatus.Accepted &&
    props.application.currentStatus !== ApplicationStatus.Rejected
  );
});

const truncatedCoverLetter = computed(() => {
  if (!props.application.coverLetter) return '';
  if (props.application.coverLetter.length <= 120) {
    return props.application.coverLetter;
  }
  return props.application.coverLetter.substring(0, 120) + '...';
});

const progressValue = computed(() => {
  const statusOrder = [
    ApplicationStatus.Applied,
    ApplicationStatus.Reviewed,
    ApplicationStatus.PhoneScreening,
    ApplicationStatus.TechnicalTest,
    ApplicationStatus.TechnicalInterview,
    ApplicationStatus.FinalInterview,
    ApplicationStatus.OfferMade,
  ];

  const currentIndex = statusOrder.indexOf(props.application.currentStatus);
  if (currentIndex === -1) return 0;
  return (currentIndex + 1) / statusOrder.length;
});

const recentHistory = computed(() => {
  if (!props.application.statusHistory) return [];
  return props.application.statusHistory
    .slice(-3) // Last 3 status changes
    .reverse(); // Show most recent first
});

const getProgressLabel = (): string => {
  const statusLabels: Record<ApplicationStatus, string> = {
    [ApplicationStatus.Applied]: 'Candidature reçue',
    [ApplicationStatus.Reviewed]: 'En cours de révision',
    [ApplicationStatus.PhoneScreening]: 'Entretien téléphonique programmé',
    [ApplicationStatus.TechnicalTest]: 'Test technique à effectuer',
    [ApplicationStatus.TechnicalInterview]: 'Entretien technique programmé',
    [ApplicationStatus.FinalInterview]: 'Entretien final programmé',
    [ApplicationStatus.OfferMade]: "Offre d'emploi proposée",
    [ApplicationStatus.Accepted]: 'Candidature acceptée',
    [ApplicationStatus.Rejected]: 'Candidature rejetée',
    [ApplicationStatus.Withdrawn]: 'Candidature retirée',
  };

  return statusLabels[props.application.currentStatus] || 'En cours';
};

const getStatusColor = (status: ApplicationStatus): string => {
  return applicationStatusConfig[status]?.color || 'grey';
};

const getStatusIcon = (status: ApplicationStatus): string => {
  return applicationStatusConfig[status]?.icon || 'help';
};

const copyApplicationId = async () => {
  try {
    await copyToClipboard(props.application.id);
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

const handleCardClick = () => {
  emit('view', props.application.id);
};
</script>

<style lang="scss" scoped>
.application-card {
  border: 1px solid #e0e0e0;
  border-radius: 12px;
  transition: all 0.2s ease;
  cursor: pointer;

  &:hover {
    border-color: var(--q-primary);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
  }
}

.application-title {
  font-size: 1.1rem;
  font-weight: 500;
  color: #2c3e50;
  line-height: 1.3;
}

.company-name {
  font-weight: 500;
}

.cover-letter-preview {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 12px;
  border-left: 3px solid var(--q-primary);
}

.progress-section {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 12px;
}

.history-preview {
  background: #f8f9fa;
  border-radius: 6px;
  padding: 12px;
}

.application-actions {
  padding-top: 12px;
  border-top: 1px solid #f0f0f0;
}

.action-buttons .q-btn {
  margin-left: 4px;
}
</style>
