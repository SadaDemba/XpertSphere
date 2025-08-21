<template>
  <q-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)">
    <q-card style="min-width: 600px; max-width: 800px">
      <q-card-section class="row items-center justify-between">
        <div class="text-h6">Historique de la candidature</div>
        <q-btn flat round dense icon="close" @click="$emit('update:modelValue', false)" />
      </q-card-section>

      <q-separator />

      <q-card-section>
        <div v-if="application" class="q-mb-md">
          <div class="text-weight-medium">{{ application.candidateName }}</div>
          <div class="text-caption text-grey-6">{{ application.jobOfferTitle }}</div>
        </div>

        <div v-if="applicationStore.statusHistory.length > 0">
          <q-timeline color="primary">
            <q-timeline-entry
              v-for="entry in sortedHistory"
              :key="entry.id"
              :color="getStatusConfig(entry.status).color"
              :icon="getStatusConfig(entry.status).icon"
            >
              <template #title>
                <div class="text-weight-medium">
                  {{ getStatusConfig(entry.status).label }}
                </div>
              </template>

              <template #subtitle>
                <div class="text-caption text-grey-6">
                  {{ formatDate(entry.updatedAt) }} par {{ entry.updatedByUserName }}
                </div>
              </template>

              <div v-if="entry.comment" class="q-mt-sm">
                <div class="text-body2 q-pa-sm bg-grey-1 rounded-borders">
                  {{ entry.comment }}
                </div>
              </div>

              <div v-if="entry.hasRating && entry.rating" class="q-mt-sm">
                <div class="text-caption text-grey-6 q-mb-xs">Évaluation :</div>
                <q-rating :model-value="entry.rating" :max="5" size="sm" color="amber" readonly />
                <span class="text-caption q-ml-sm">{{ entry.rating }}/5</span>
                <div v-if="entry.ratingDescription" class="text-caption text-grey-6">
                  {{ entry.ratingDescription }}
                </div>
              </div>
            </q-timeline-entry>
          </q-timeline>
        </div>

        <div v-else class="text-center q-pa-lg">
          <q-icon name="history" size="lg" color="grey-5" />
          <div class="text-h6 text-grey-6 q-mt-md">Aucun historique disponible</div>
          <div class="text-body2 text-grey-5">
            L'historique des changements de statut apparaîtra ici.
          </div>
        </div>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn flat label="Fermer" @click="$emit('update:modelValue', false)" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { date } from 'quasar';
import { useApplicationStore } from 'src/stores/applicationStore';
import { applicationStatusConfig } from 'src/models/application';
import type { ApplicationDto, ApplicationStatusHistoryDto } from 'src/models/application';
import type { ApplicationStatus } from 'src/enums/ApplicationStatus';

interface Props {
  modelValue: boolean;
  application: ApplicationDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
}

defineProps<Props>();
defineEmits<Emits>();

const applicationStore = useApplicationStore();

const sortedHistory = computed(() => {
  return [...applicationStore.statusHistory].sort(
    (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
  );
});

const getStatusConfig = (status: ApplicationStatus) => {
  return (
    applicationStatusConfig[status] || {
      color: 'grey',
      textColor: 'white',
      icon: 'help',
      label: 'Inconnu',
    }
  );
};

const formatDate = (dateString: string) => {
  return date.formatDate(dateString, 'DD/MM/YYYY HH:mm');
};
</script>
