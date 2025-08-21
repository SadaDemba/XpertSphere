<template>
  <div class="job-list">
    <div class="list-header q-mb-md">
      <div class="row items-center justify-between">
        <div class="text-h6">
          {{ jobs?.length || 0 }} offre{{ (jobs?.length || 0) > 1 ? 's' : '' }} trouvée{{
            (jobs?.length || 0) > 1 ? 's' : ''
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
      <p class="q-mt-md text-center">Chargement des offres...</p>
    </div>

    <div v-else-if="!jobs || jobs.length === 0" class="empty-state text-center q-pa-xl">
      <q-icon name="work_off" size="80px" color="grey-4" />
      <h3 class="text-h6 q-mt-md q-mb-sm">Aucune offre trouvée</h3>
      <p class="text-body2 text-grey-7">
        Créez votre première offre d'emploi pour commencer le recrutement.
      </p>
    </div>

    <div v-else>
      <div v-if="viewMode === 'grid'" class="job-grid">
        <job-card
          v-for="job in jobs || []"
          :key="job.id"
          :job="job"
          @edit="$emit('edit', job)"
          @delete="$emit('delete', job)"
          @duplicate="$emit('duplicate', job)"
          @view-applications="$emit('viewApplications', job)"
        />
      </div>

      <div v-else class="job-table">
        <job-table
          :jobs="jobs || []"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
          @duplicate="$emit('duplicate', $event)"
          @view-applications="$emit('viewApplications', $event)"
        />
      </div>

      <div v-if="!loading" class="q-mt-lg">
        <app-pagination
          :current-page="currentPage || 1"
          :total-pages="totalPages || 0"
          :total-items="totalItems || 0"
          item-name="offre"
          item-name-plural="offres"
          @page-change="onPageChange"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import JobCard from './JobCard.vue';
import JobTable from './JobTable.vue';
import AppPagination from '../common/AppPagination.vue';
import type { JobOffer } from '../../models/job';

interface Props {
  jobs?: JobOffer[];
  loading?: boolean;
  currentPage?: number;
  totalPages?: number;
  totalItems?: number;
}

interface Emits {
  (e: 'edit', job: JobOffer): void;
  (e: 'delete', job: JobOffer): void;
  (e: 'duplicate', job: JobOffer): void;
  (e: 'viewApplications', job: JobOffer): void;
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
.job-grid {
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
  .job-grid {
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
