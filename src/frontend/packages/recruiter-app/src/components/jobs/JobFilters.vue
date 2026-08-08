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
            @keydown.enter="handleSearchFieldEnter"
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
            @keydown.enter="handleSearchFieldEnter"
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
import { ref, watch, nextTick, onBeforeUnmount } from 'vue';
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

// Debounce local (400ms) partagé par les champs "Titre" et "Localisation" :
// aucune dépendance de debounce n'est disponible dans ce package pour un
// q-input texte libre (voir spec ux-polish-pre-demo.md, point 2).
const SEARCH_DEBOUNCE_MS = 400;
const SEARCH_MIN_LENGTH = 3;
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;
// Le clic sur "Effacer tous les filtres" vide title/location directement (au lieu
// de passer par une saisie utilisateur) : sans ce garde, les watchers ci-dessous
// déclencheraient chacun leur propre recherche immédiate, en plus de celle déjà
// émise par clearAllFilters (double/triple appel réseau redondant).
let suppressFieldWatchers = false;

function clearSearchDebounce() {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
    searchDebounceTimer = null;
  }
}

async function triggerImmediateSearch() {
  clearSearchDebounce();
  await nextTick();
  emitSearch();
}

function scheduleDebouncedSearch() {
  clearSearchDebounce();
  searchDebounceTimer = setTimeout(() => {
    searchDebounceTimer = null;
    emitSearch();
  }, SEARCH_DEBOUNCE_MS);
}

function handleSearchFieldChange(newValue: string | null | undefined) {
  if (suppressFieldWatchers) return;

  const length = newValue?.length ?? 0;
  if (length === 0) {
    void triggerImmediateSearch();
  } else if (length >= SEARCH_MIN_LENGTH) {
    scheduleDebouncedSearch();
  } else {
    // 1 ou 2 caractères : pas de recherche automatique, on annule un
    // debounce précédemment programmé pour ne pas déclencher un appel
    // réseau avec une valeur désormais obsolète.
    clearSearchDebounce();
  }
}

function handleSearchFieldEnter() {
  void triggerImmediateSearch();
}

watch(() => localFilters.value.title, handleSearchFieldChange);
watch(() => localFilters.value.location, handleSearchFieldChange);

onBeforeUnmount(() => {
  clearSearchDebounce();
});

function emitSearch() {
  emit('search');
}

async function handleFilterChange() {
  await nextTick();
  emitSearch();
}

function clearAllFilters() {
  suppressFieldWatchers = true;
  clearSearchDebounce();
  localFilters.value.title = '';
  localFilters.value.location = '';
  delete localFilters.value.workMode;
  delete localFilters.value.contractType;
  delete localFilters.value.status;
  emit('clear');
  void nextTick(() => {
    suppressFieldWatchers = false;
  });
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
