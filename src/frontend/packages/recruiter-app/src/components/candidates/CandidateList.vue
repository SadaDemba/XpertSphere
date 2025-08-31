<template>
  <div class="candidate-list">
    <div class="list-header q-mb-md">
      <div class="row items-center justify-between">
        <div class="text-h6">
          {{ candidates.length }} candidat{{ candidates.length > 1 ? 's' : '' }} trouvé{{
            candidates.length > 1 ? 's' : ''
          }}
        </div>
        <q-btn-group flat>
          <q-btn
            flat
            dense
            :color="viewMode === 'grid' ? 'primary' : 'grey-7'"
            icon="grid_view"
            aria-label="Affichage en grille"
            @click="viewMode = 'grid'"
          />
          <q-btn
            flat
            dense
            :color="viewMode === 'list' ? 'primary' : 'grey-7'"
            icon="view_list"
            aria-label="Affichage en liste"
            @click="viewMode = 'list'"
          />
        </q-btn-group>
      </div>
    </div>

    <div v-if="loading" class="loading-container">
      <q-spinner-dots size="40px" color="primary" />
      <p class="q-mt-md text-center">Chargement des candidats...</p>
    </div>

    <div v-else-if="candidates.length === 0" class="empty-state text-center q-pa-xl">
      <q-icon name="people_off" size="80px" color="grey-4" />
      <h3 class="text-h6 q-mt-md q-mb-sm">Aucun candidat trouvé</h3>
      <p class="text-body2 text-grey-7">
        Ajoutez votre premier candidat pour commencer à constituer votre base de talents.
      </p>
    </div>

    <div v-else>
      <div v-if="viewMode === 'grid'" class="candidate-grid">
        <candidate-card
          v-for="candidate in candidates"
          :key="candidate.id"
          :candidate="candidate"
          @edit="$emit('edit', candidate)"
          @delete="$emit('delete', candidate)"
          @view-profile="$emit('viewProfile', candidate)"
          @view-applications="$emit('viewApplications', candidate)"
        />
      </div>

      <div v-else class="candidate-table">
        <candidate-table
          :candidates="candidates"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
          @view-profile="$emit('viewProfile', $event)"
          @view-applications="$emit('viewApplications', $event)"
        />
      </div>

      <div v-if="!loading" class="q-mt-lg">
        <app-pagination
          :current-page="currentPage || 1"
          :total-pages="totalPages || 0"
          :total-items="totalItems || 0"
          item-name="candidat"
          item-name-plural="candidats"
          @page-change="onPageChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import CandidateCard from './CandidateCard.vue';
import CandidateTable from './CandidateTable.vue';
import AppPagination from '../common/AppPagination.vue';

interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  position: string;
  experience: string;
  location: string;
  skills: string[];
  status: 'active' | 'inactive' | 'blacklisted';
  createdAt: string;
  updatedAt: string;
  lastActivity: string;
}

interface Props {
  candidates: Candidate[];
  loading?: boolean;
  currentPage?: number;
  totalPages?: number;
  totalItems?: number;
}

interface Emits {
  (e: 'edit', candidate: Candidate): void;
  (e: 'delete', candidate: Candidate): void;
  (e: 'viewProfile', candidate: Candidate): void;
  (e: 'viewApplications', candidate: Candidate): void;
  (e: 'page-change', page: number): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

const viewMode = ref<'grid' | 'list'>('grid');

const onPageChange = (page: number) => {
  emit('page-change', page);
};
</script>

<style scoped>
.candidate-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
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
  .candidate-grid {
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
