<template>
  <div class="application-list">
    <div class="list-header q-mb-md">
      <div class="row items-center justify-between">
        <div class="text-h6">
          {{ applications.length }} candidature{{ applications.length > 1 ? 's' : '' }} trouvée{{
            applications.length > 1 ? 's' : ''
          }}
        </div>
        <q-btn-group flat>
          <q-btn
            flat
            dense
            :color="viewMode === 'cards' ? 'primary' : 'grey-7'"
            icon="view_module"
            aria-label="Affichage en cartes"
            @click="viewMode = 'cards'"
          />
          <q-btn
            flat
            dense
            :color="viewMode === 'table' ? 'primary' : 'grey-7'"
            icon="view_list"
            aria-label="Affichage en tableau"
            @click="viewMode = 'table'"
          />
        </q-btn-group>
      </div>
    </div>

    <div v-if="loading" class="loading-container">
      <q-spinner-dots size="40px" color="primary" />
      <p class="q-mt-md text-center">Chargement des candidatures...</p>
    </div>

    <div v-else-if="applications.length === 0" class="empty-state text-center q-pa-xl">
      <q-icon name="assignment_off" size="80px" color="grey-4" />
      <h3 class="text-h6 q-mt-md q-mb-sm">Aucune candidature trouvée</h3>
      <p class="text-body2 text-grey-7">
        Les candidatures apparaîtront ici lorsque des candidats postuleront à vos offres.
      </p>
    </div>

    <div v-else>
      <div v-if="viewMode === 'cards'" class="application-cards">
        <application-card
          v-for="application in applications"
          :key="application.id"
          :application="application"
          @view="$emit('view', application)"
          @update-status="$emit('updateStatus', application, $event)"
          @schedule-interview="$emit('scheduleInterview', application)"
          @send-email="$emit('sendEmail', application)"
          @delete="$emit('delete', application)"
        />
      </div>

      <div v-else class="application-table">
        <application-table
          :applications="applications"
          @view="$emit('view', $event)"
          @update-status="$emit('updateStatus', $event.application, $event.status)"
          @schedule-interview="$emit('scheduleInterview', $event)"
          @send-email="$emit('sendEmail', $event)"
          @delete="$emit('delete', $event)"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import ApplicationCard from './ApplicationCard.vue';
import ApplicationTable from './ApplicationTable.vue';
import type { ApplicationSource, ApplicationStatus } from 'src/enums';

interface Application {
  id: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  jobId: string;
  jobTitle: string;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string;
  source: ApplicationSource;
  coverLetter: string;
  resumeUrl: string;
  rating: number | null;
  notes: string;
}

interface Props {
  applications: Application[];
  loading?: boolean;
}

interface Emits {
  (e: 'view', application: Application): void;
  (e: 'updateStatus', application: Application, status: string): void;
  (e: 'scheduleInterview', application: Application): void;
  (e: 'sendEmail', application: Application): void;
  (e: 'delete', application: Application): void;
}

defineProps<Props>();
defineEmits<Emits>();

const viewMode = ref<'cards' | 'table'>('cards');
</script>

<style scoped>
.application-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(400px, 1fr));
  gap: 20px;
}

.loading-container {
  text-align: center;
  padding: 60px 20px;
}

.empty-state {
  background: white;
  border-radius: 8px;
  border: 1px solid #e0e0e0;
}

@media (max-width: 599px) {
  .application-cards {
    grid-template-columns: 1fr;
    gap: 16px;
  }

  .list-header .row {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
}
</style>
