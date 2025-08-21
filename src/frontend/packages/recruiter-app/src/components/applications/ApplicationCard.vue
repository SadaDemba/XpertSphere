<template>
  <q-card class="application-card" tabindex="0" @keydown.enter="$emit('view', application)">
    <q-card-section>
      <div class="row items-start justify-between q-mb-sm">
        <div class="candidate-info flex-1">
          <div class="candidate-header row items-center q-mb-sm">
            <q-avatar size="40px" color="primary" text-color="white" class="q-mr-sm">
              {{
                application.candidateName
                  .split(' ')
                  .map((n) => n[0])
                  .join('')
              }}
            </q-avatar>
            <div>
              <h3 class="candidate-name text-h6 q-mb-none">
                <button
                  class="name-link"
                  :aria-label="`Voir la candidature de ${application.candidateName}`"
                  @click="$emit('view', application)"
                >
                  {{ application.candidateName }}
                </button>
              </h3>
              <div class="candidate-email text-body2 text-grey-7">
                {{ application.candidateEmail }}
              </div>
            </div>
          </div>

          <div class="job-info q-mb-sm">
            <h4 class="job-title text-subtitle1 q-mb-xs">
              {{ application.jobTitle }}
            </h4>
            <div class="application-meta text-body2 text-grey-7">
              <div class="meta-item">
                <q-icon name="schedule" size="16px" />
                Postulé le {{ formatDate(application.appliedAt) }}
              </div>
              <div class="meta-item">
                <q-icon name="source" size="16px" />
                {{ applicationSourceLabels[application.source] }}
              </div>
            </div>
          </div>

          <div v-if="application.rating" class="rating q-mb-sm">
            <q-rating
              :model-value="application.rating"
              size="18px"
              color="amber"
              readonly
              :aria-label="`Note: ${application.rating} étoiles`"
            />
          </div>
        </div>

        <q-btn
          flat
          dense
          round
          icon="more_vert"
          :aria-label="`Actions pour la candidature de ${application.candidateName}`"
        >
          <q-menu>
            <q-list style="min-width: 150px">
              <q-item clickable @click="$emit('view', application)">
                <q-item-section avatar>
                  <q-icon name="visibility" />
                </q-item-section>
                <q-item-section>Voir détails</q-item-section>
              </q-item>
              <q-item clickable @click="viewResume">
                <q-item-section avatar>
                  <q-icon name="description" />
                </q-item-section>
                <q-item-section>Voir le CV</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('scheduleInterview', application)">
                <q-item-section avatar>
                  <q-icon name="event" />
                </q-item-section>
                <q-item-section>Planifier entretien</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('sendEmail', application)">
                <q-item-section avatar>
                  <q-icon name="email" />
                </q-item-section>
                <q-item-section>Envoyer email</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable class="text-negative" @click="$emit('delete', application)">
                <q-item-section avatar>
                  <q-icon name="delete" />
                </q-item-section>
                <q-item-section>Supprimer</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </div>

      <div v-if="application.coverLetter" class="cover-letter q-mb-sm">
        <p class="text-body2 text-grey-8">
          {{ application.coverLetter.substring(0, 150)
          }}{{ application.coverLetter.length > 150 ? '...' : '' }}
        </p>
      </div>
    </q-card-section>

    <q-card-section class="card-footer">
      <div class="row items-center justify-between">
        <div class="status-section">
          <q-chip
            :color="applicationStatusConfig[application.status].color"
            :text-color="applicationStatusConfig[application.status].textColor"
            dense
            :aria-label="`Statut: ${applicationStatusConfig[application.status].label}`"
          >
            <q-icon
              :name="applicationStatusConfig[application.status].icon"
              size="14px"
              class="q-mr-xs"
            />
            {{ applicationStatusConfig[application.status].label }}
          </q-chip>
        </div>
      </div>

      <div class="update-info text-caption text-grey-6 q-mt-sm">
        Mis à jour le {{ formatDate(application.updatedAt) }}
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import type { Application } from '../../models';
import { applicationStatusConfig, applicationSourceLabels } from '../../models';
import { formatDate } from '../../helpers';

interface Props {
  application: Application;
}

interface Emits {
  (e: 'view', application: Application): void;
  (e: 'scheduleInterview', application: Application): void;
  (e: 'sendEmail', application: Application): void;
  (e: 'delete', application: Application): void;
}

defineProps<Props>();
defineEmits<Emits>();

function viewResume() {
  window.open('/resume-viewer', '_blank');
}
</script>

<style scoped>
.application-card {
  border: 1px solid #e0e0e0;
  transition: all 0.2s ease;
  cursor: pointer;
}

.application-card:hover {
  border-color: var(--q-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.application-card:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
}

.name-link {
  background: none;
  border: none;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  padding: 0;
  margin: 0;
  text-decoration: none;
}

.name-link:hover {
  color: var(--q-primary);
}

.name-link:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
  border-radius: 4px;
}

.application-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.cover-letter {
  background-color: #f8f9fa;
  padding: 12px;
  border-radius: 6px;
  border-left: 3px solid var(--q-primary);
}

.card-footer {
  background-color: #f8f9fa;
  border-top: 1px solid #e0e0e0;
}

.status-section {
  flex: 1;
}

@media (max-width: 599px) {
  .candidate-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .card-footer .row {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }

  .status-section {
    width: 100%;
  }
}
</style>
