<template>
  <q-card class="job-filters" flat>
    <q-card-section class="q-pa-lg">
      <div class="row q-gutter-md items-center q-mb-md">
        <div class="col-12 col-md-8">
          <q-input
            v-model="localFilters.title"
            filled
            placeholder="Rechercher une offre..."
            clearable
            aria-label="Rechercher parmi les offres d'emploi"
            class="search-input"
            @keydown.enter="emitSearch"
          >
            <template #prepend>
              <q-icon name="search" color="grey-7" />
            </template>
            <template #append>
              <q-btn
                v-if="localFilters.title"
                flat
                dense
                round
                icon="close"
                size="sm"
                @click="localFilters.title = ''"
              />
            </template>
          </q-input>
        </div>

        <div class="col-12 col-md">
          <q-input
            v-model="localFilters.location"
            filled
            placeholder="Localisation"
            clearable
            aria-label="Filtrer par localisation"
            class="filter-input"
            @keydown.enter="emitSearch"
          >
            <template #prepend>
              <q-icon name="place" size="sm" color="grey-7" />
            </template>
          </q-input>
        </div>
      </div>

      <div class="row q-gutter-md items-center">
        <div class="col-12 col-sm-6 col-md">
          <q-select
            v-model="localFilters.workMode"
            filled
            label="Mode de travail"
            clearable
            :options="workModeOptions"
            aria-label="Filtrer par mode de travail"
            class="filter-select"
            emit-value
            map-options
            @update:model-value="handleFilterChange"
          >
            <template #prepend>
              <q-icon name="business_center" size="sm" color="grey-7" />
            </template>
          </q-select>
        </div>

        <div class="col-12 col-sm-6 col-md">
          <q-select
            v-model="localFilters.contractType"
            filled
            label="Type de contrat"
            clearable
            :options="contractTypeOptions"
            aria-label="Filtrer par type de contrat"
            class="filter-select"
            emit-value
            map-options
            @update:model-value="handleFilterChange"
          >
            <template #prepend>
              <q-icon name="description" size="sm" color="grey-7" />
            </template>
          </q-select>
        </div>

        <div class="col-12 col-sm-6 col-md">
          <q-select
            v-model="localFilters.status"
            filled
            label="Statut"
            clearable
            :options="statusOptions"
            aria-label="Filtrer par statut"
            class="filter-select"
            emit-value
            map-options
            @update:model-value="handleFilterChange"
          >
            <template #prepend>
              <q-icon name="flag" size="sm" color="grey-7" />
            </template>
          </q-select>
        </div>

        <div class="col-auto">
          <q-btn
            unelevated
            color="grey-3"
            text-color="grey-8"
            icon="clear_all"
            :aria-label="'Effacer tous les filtres'"
            class="clear-btn"
            @click="clearAllFilters"
          >
            <q-tooltip>Effacer tous les filtres</q-tooltip>
          </q-btn>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import type { JobOfferFilter } from '../../models/job';
import { WorkMode, ContractType } from '../../enums';
import { JobOfferStatus, getJobOfferStatusName } from '../../enums';
import { workModeLabels, contractTypeLabels } from '../../models/job';
import { getWorkModeName } from '../../enums/WorkMode';
import { getContractTypeName } from '../../enums/ContractType';

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
  value: getWorkModeName(parseInt(value) as WorkMode),
}));

const contractTypeOptions = Object.entries(contractTypeLabels).map(([value, label]) => ({
  label,
  value: getContractTypeName(parseInt(value) as ContractType),
}));

const statusOptions = [
  { label: 'Brouillon', value: getJobOfferStatusName(JobOfferStatus.Draft) },
  { label: 'Publiée', value: getJobOfferStatusName(JobOfferStatus.Published) },
  { label: 'Fermée', value: getJobOfferStatusName(JobOfferStatus.Closed) },
];

watch(
  localFilters,
  (newFilters) => {
    emit('update:filters', { ...newFilters });
  },
  { deep: true },
);

watch(
  () => localFilters.value.title,
  async (newTitle) => {
    if (newTitle && newTitle.length >= 3) {
      await nextTick();
      emitSearch();
    } else if (!newTitle || newTitle.length === 0) {
      await nextTick();
      emitSearch();
    }
  },
);

function emitSearch() {
  emit('search');
}

async function handleFilterChange() {
  await nextTick();
  emitSearch();
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
  background: #ffffff;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  transition: all 0.3s ease;
}

.job-filters:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.search-input {
  font-size: 16px;
}

.search-input :deep(.q-field__control),
.filter-select :deep(.q-field__control),
.filter-input :deep(.q-field__control) {
  height: 56px !important;
  min-height: 56px !important;
  border-radius: 8px;
  background: #e0e0e0;
  transition: all 0.3s ease;
}

.search-input:hover :deep(.q-field__control),
.filter-select:hover :deep(.q-field__control),
.filter-input:hover :deep(.q-field__control) {
  background: #fafafa;
}

.search-input :deep(.q-field__control:focus-within),
.filter-select :deep(.q-field__control:focus-within),
.filter-input :deep(.q-field__control:focus-within) {
  background: #e0e0e0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.clear-btn {
  height: 56px !important;
  min-height: 56px !important;
  border-radius: 8px;
  font-weight: 500;
  transition: all 0.3s ease;
}

.clear-btn:hover {
  background: #e0e0e0 !important;
}

@media (max-width: 599px) {
  .job-filters {
    border-radius: 8px;
  }
}
</style>
