<template>
  <q-card class="job-filters">
    <q-card-section>
      <div class="row q-gutter-md">
        <div class="col-12 col-md-3">
          <q-input
            v-model="localFilters.title"
            outlined
            dense
            placeholder="Rechercher une offre..."
            clearable
            aria-label="Rechercher parmi les offres d'emploi"
            @keydown.enter="emitSearch"
          >
            <template #prepend>
              <q-icon name="search" />
            </template>
          </q-input>
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.workMode"
            outlined
            dense
            placeholder="Mode de travail"
            clearable
            :options="workModeOptions"
            aria-label="Filtrer par mode de travail"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-input
            v-model="localFilters.location"
            outlined
            dense
            placeholder="Localisation"
            clearable
            aria-label="Filtrer par localisation"
            @keydown.enter="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.contractType"
            outlined
            dense
            placeholder="Type de contrat"
            clearable
            :options="contractTypeOptions"
            aria-label="Filtrer par type de contrat"
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
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import type { JobOfferFilter } from '../../models/job';
import type { WorkMode, ContractType } from '../../enums';
import { JobOfferStatus } from '../../enums';
import { workModeLabels, contractTypeLabels } from '../../models/job';

interface Props {
  filters: JobOfferFilter;
}

interface Emits {
  (e: 'update:filters', filters: JobOfferFilter): void;
  (e: 'search'): void;
  (e: 'clear'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localFilters = ref<JobOfferFilter>({ ...props.filters });

const workModeOptions = Object.entries(workModeLabels).map(([value, label]) => ({
  label,
  value: parseInt(value) as WorkMode,
}));

const contractTypeOptions = Object.entries(contractTypeLabels).map(([value, label]) => ({
  label,
  value: parseInt(value) as ContractType,
}));

const statusOptions = [
  { label: 'Brouillon', value: JobOfferStatus.Draft },
  { label: 'Publiée', value: JobOfferStatus.Published },
  { label: 'Fermée', value: JobOfferStatus.Closed },
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
  localFilters.value.title = '';
  localFilters.value.location = '';
  delete localFilters.value.workMode;
  delete localFilters.value.contractType;
  delete localFilters.value.status;
  emit('clear');
}
</script>

<style scoped>
.job-filters {
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
