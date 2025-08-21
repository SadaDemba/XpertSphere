<template>
  <q-card class="application-filters">
    <q-card-section>
      <div class="row q-gutter-md">
        <div class="col-12 col-md-3">
          <q-input
            v-model="localFilters.search"
            outlined
            dense
            placeholder="Rechercher une candidature..."
            clearable
            aria-label="Rechercher parmi les candidatures"
            @keydown.enter="emitSearch"
          >
            <template #prepend>
              <q-icon name="search" />
            </template>
          </q-input>
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.jobId"
            outlined
            dense
            placeholder="Offre d'emploi"
            clearable
            :options="jobOptions"
            option-label="title"
            option-value="id"
            emit-value
            map-options
            aria-label="Filtrer par offre d'emploi"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.status"
            outlined
            dense
            placeholder="Statut"
            clearable
            :options="statusOptions"
            aria-label="Filtrer par statut"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.source"
            outlined
            dense
            placeholder="Source"
            clearable
            :options="sourceOptions"
            aria-label="Filtrer par source"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.rating"
            outlined
            dense
            placeholder="Note"
            clearable
            :options="ratingOptions"
            aria-label="Filtrer par note"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-1">
          <q-btn
            flat
            dense
            color="grey-7"
            icon="clear"
            label="Effacer"
            aria-label="Effacer tous les filtres"
            @click="clearAllFilters"
          />
        </div>
      </div>

      <div class="row q-gutter-md q-mt-sm">
        <div class="col-12 col-md-4">
          <q-input
            v-model="localFilters.dateRange!.from"
            outlined
            dense
            type="date"
            label="Date de début"
            aria-label="Filtrer à partir de cette date"
            @update:model-value="emitSearch"
          />
        </div>
        <div class="col-12 col-md-4">
          <q-input
            v-model="localFilters.dateRange!.to"
            outlined
            dense
            type="date"
            label="Date de fin"
            aria-label="Filtrer jusqu'à cette date"
            @update:model-value="emitSearch"
          />
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { statusOptions } from 'src/enums/ApplicationStatus';
import { ref, watch } from 'vue';

interface ApplicationFilters {
  search: string;
  jobId: string;
  status: number;
  source: number;
  dateRange: { from: string; to: string } | null;
  rating: number | null;
}

interface Props {
  filters: ApplicationFilters;
}

interface Emits {
  (e: 'update:filters', filters: ApplicationFilters): void;
  (e: 'search'): void;
  (e: 'clear'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localFilters = ref<ApplicationFilters>({
  ...props.filters,
  dateRange: props.filters.dateRange || { from: '', to: '' },
});

const jobOptions = [
  { id: '1', title: 'Développeur Full Stack Senior' },
  { id: '2', title: 'Designer UX/UI' },
  { id: '3', title: 'Product Manager' },
  { id: '4', title: 'Data Scientist' },
];

const sourceOptions = [
  { label: 'Site web', value: 'website' },
  { label: 'LinkedIn', value: 'linkedin' },
  { label: 'Email', value: 'email' },
  { label: 'Recommandation', value: 'referral' },
  { label: 'Direct', value: 'direct' },
];

const ratingOptions = [
  { label: '5 étoiles', value: 5 },
  { label: '4 étoiles', value: 4 },
  { label: '3 étoiles', value: 3 },
  { label: '2 étoiles', value: 2 },
  { label: '1 étoile', value: 1 },
];

watch(
  localFilters,
  (newFilters) => {
    emit('update:filters', { ...newFilters });
  },
  { deep: true },
);

function emitSearch() {
  emit('search');
}

function clearAllFilters() {
  localFilters.value = {
    search: '',
    jobId: '',
    status: 0,
    source: 0,
    dateRange: { from: '', to: '' },
    rating: null,
  };
  emit('clear');
}
</script>

<style scoped>
.application-filters {
  border: 1px solid #e0e0e0;
}

@media (max-width: 599px) {
  .row {
    flex-direction: column;
  }

  .col-12 {
    margin-bottom: 8px;
  }
}
</style>
