<template>
  <q-card class="job-card" tabindex="0" @keydown.enter="$emit('edit', job)">
    <q-card-section>
      <div class="row items-start justify-between">
        <div class="job-info flex-1">
          <h3 class="job-title text-h6 q-mb-xs">
            <button
              class="title-link"
              :aria-label="`Modifier l'offre ${job.title}`"
              @click="$emit('edit', job)"
            >
              {{ job.title }}
            </button>
          </h3>
          <div class="job-meta text-body2 text-grey-7 q-mb-sm">
            <div class="meta-item">
              <q-icon name="business" size="16px" />
              {{ job.organizationName }}
            </div>
            <div class="meta-item">
              <q-icon name="location_on" size="16px" />
              {{ job.location }}
            </div>
            <div class="meta-item">
              <q-icon name="schedule" size="16px" />
              {{ contractTypeLabels[job.contractType] }}
            </div>
          </div>
        </div>

        <q-btn flat dense round icon="more_vert" :aria-label="`Actions pour l'offre ${job.title}`">
          <q-menu>
            <q-list style="min-width: 100px">
              <q-item clickable @click="$emit('edit', job)">
                <q-item-section avatar>
                  <q-icon name="edit" />
                </q-item-section>
                <q-item-section>Modifier</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('duplicate', job)">
                <q-item-section avatar>
                  <q-icon name="content_copy" />
                </q-item-section>
                <q-item-section>Dupliquer</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('viewApplications', job)">
                <q-item-section avatar>
                  <q-icon name="assignment" />
                </q-item-section>
                <q-item-section>Candidatures</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable class="text-negative" @click="$emit('delete', job)">
                <q-item-section avatar>
                  <q-icon name="delete" />
                </q-item-section>
                <q-item-section>Supprimer</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </div>
    </q-card-section>

    <q-card-section class="card-footer">
      <div class="row items-center justify-between">
        <div class="status-info">
          <q-select
            :model-value="job.status"
            :options="statusOptions"
            dense
            borderless
            behavior="menu"
            class="status-select"
            :aria-label="`Modifier le statut de l'offre ${job.title}`"
            @update:model-value="updateStatus"
          >
            <template #selected>
              <q-chip
                :color="jobOfferStatusConfig[job.status]?.color || 'grey'"
                :text-color="jobOfferStatusConfig[job.status]?.textColor || 'black'"
                dense
              >
                <q-icon
                  :name="jobOfferStatusConfig[job.status]?.icon || 'help'"
                  size="14px"
                  class="q-mr-xs"
                />
                {{ jobOfferStatusConfig[job.status]?.label || 'Statut inconnu' }}
              </q-chip>
            </template>
            <template #option="scope">
              <q-item v-bind="scope.itemProps">
                <q-item-section avatar>
                  <q-icon :name="jobOfferStatusConfig[scope.opt.value as JobOfferStatus]?.icon" />
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ scope.opt.label }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
          </q-select>
        </div>

        <div class="applications-count">
          <q-btn
            flat
            dense
            label="Candidatures"
            icon="assignment"
            color="primary"
            :aria-label="`Voir les candidatures pour ${job.title}`"
            @click="$emit('viewApplications', job)"
          />
        </div>
      </div>

      <div class="dates text-caption text-grey-6 q-mt-sm">
        Créée le {{ formatDate(job.createdAt)
        }}{{ job.updatedAt ? ` • Modifiée le ${formatDate(job.updatedAt)}` : '' }}
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import type { JobOffer } from '../../models/job';
import { jobOfferStatusConfig, contractTypeLabels } from '../../models/job';
import { JobOfferStatus } from '../../enums';
import { formatDate } from '../../helpers';
import { computed } from 'vue';

interface Props {
  job: JobOffer;
}

interface Emits {
  (e: 'edit', job: JobOffer): void;
  (e: 'delete', job: JobOffer): void;
  (e: 'duplicate', job: JobOffer): void;
  (e: 'viewApplications', job: JobOffer): void;
  (e: 'updateStatus', job: JobOffer, status: JobOfferStatus): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const statusOptions = computed(() => [
  { label: 'Brouillon', value: JobOfferStatus.Draft },
  { label: 'Publiée', value: JobOfferStatus.Published },
  { label: 'Fermée', value: JobOfferStatus.Closed },
]);

const updateStatus = (newStatus: JobOfferStatus) => {
  emit('updateStatus', props.job, newStatus);
};
</script>

<style scoped>
.job-card {
  border: 1px solid #e0e0e0;
  transition: all 0.2s ease;
  cursor: pointer;
}

.job-card:hover {
  border-color: var(--q-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.job-card:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
}

.title-link {
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

.title-link:hover {
  color: var(--q-primary);
}

.title-link:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
  border-radius: 4px;
}

.job-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 4px;
}

.card-footer {
  background-color: #f8f9fa;
  border-top: 1px solid #e0e0e0;
}

@media (max-width: 599px) {
  .job-meta {
    flex-direction: column;
    gap: 6px;
  }

  .card-footer .row {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
}

.status-select {
  min-width: 120px;
}
</style>
