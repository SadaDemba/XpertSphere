<template>
  <q-page class="job-listings-page">
    <div class="page-header q-pa-lg">
      <h4 class="text-h4 q-my-none">Offres d'emploi</h4>
      <p class="text-subtitle1 q-mt-sm text-grey-7">Découvrez les dernières opportunités</p>
    </div>

    <!-- Search and Filters -->
    <div class="filters-section q-px-lg q-pb-lg">
      <div class="row q-gutter-md items-center">
        <div class="col">
          <q-input
            v-model="searchTerm"
            filled
            placeholder="Rechercher par titre, compétences..."
            clearable
            @keyup.enter="handleSearch"
          >
            <template #prepend>
              <q-icon name="search" />
            </template>
            <template #append>
              <q-btn flat round icon="search" color="primary" @click="handleSearch" />
            </template>
          </q-input>
        </div>

        <div class="col-auto">
          <q-btn
            unelevated
            icon="tune"
            label="Filtres"
            :color="hasActiveFilters ? 'primary' : 'grey-6'"
            @click="showFilters = !showFilters"
          />
        </div>
      </div>

      <!-- Advanced Filters -->
      <q-slide-transition>
        <div v-show="showFilters" class="filters-panel q-mt-md q-pa-md bg-grey-1 rounded-borders">
          <div class="row q-gutter-md">
            <div class="col-12 col-md-3">
              <q-select
                v-model="filters.location"
                filled
                label="Localisation"
                :options="locationOptions"
                clearable
                emit-value
                map-options
              />
            </div>

            <div class="col-12 col-md-3">
              <q-select
                v-model="filters.workMode"
                filled
                label="Mode de travail"
                :options="workModeOptions"
                clearable
                emit-value
                map-options
              />
            </div>

            <div class="col-12 col-md-3">
              <q-select
                v-model="filters.contractType"
                filled
                label="Type de contrat"
                :options="contractTypeOptions"
                clearable
                emit-value
                map-options
              />
            </div>

            <div class="col-12 col-md-3 flex items-end">
              <q-btn color="primary" label="Appliquer" class="q-mr-sm" @click="handleFilter" />
              <q-btn flat label="Réinitialiser" @click="resetFilters" />
            </div>
          </div>
        </div>
      </q-slide-transition>
    </div>

    <!-- Results -->
    <div class="results-section q-px-lg">
      <div class="row justify-between items-center q-mb-lg">
        <div class="text-h6">{{ paginationInfo.totalItems }} offres trouvées</div>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center q-py-xl">
        <q-spinner size="50px" color="primary" />
      </div>

      <!-- Error -->
      <q-banner v-else-if="hasError" class="text-white bg-negative q-mb-md rounded-borders">
        <template #avatar>
          <q-icon name="error" />
        </template>
        {{ error }}
        <template #action>
          <q-btn flat color="white" label="Réessayer" @click="loadJobOffers" />
        </template>
      </q-banner>

      <!-- No results -->
      <div v-else-if="!hasJobOffers" class="text-center q-py-xl">
        <q-icon name="work_off" size="64px" color="grey" />
        <h6 class="q-mt-md q-mb-none text-grey">Aucune offre trouvée</h6>
        <p class="text-grey">Essayez de modifier vos critères de recherche</p>
      </div>

      <!-- Job Cards -->
      <div v-else class="row q-gutter-md">
        <div v-for="job in jobOffers" :key="job.id" class="col-12 col-md-6 col-lg-4">
          <job-card :job="job" @click="viewJobDetails(job.id)" />
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="hasJobOffers && paginationInfo.totalPages > 1" class="flex justify-center q-py-lg">
        <q-pagination
          v-model="currentPage"
          :max="paginationInfo.totalPages"
          :max-pages="5"
          boundary-numbers
          @update:model-value="handlePageChange"
        />
      </div>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useJobOfferStore } from '../stores/jobOfferStore';
import type { WorkMode, ContractType } from '../enums';
import { workModeLabels, contractTypeLabels } from '../models/job';
import JobCard from '../components/JobCard.vue';

// Router
const router = useRouter();

// Store
const jobOfferStore = useJobOfferStore();
const { jobOffers, paginationInfo, isLoading, error, hasError, hasJobOffers } =
  storeToRefs(jobOfferStore);

// Reactive state
const searchTerm = ref('');
const showFilters = ref(false);
const currentPage = ref(1);

const filters = ref({
  location: null as string | null,
  workMode: null as WorkMode | null,
  contractType: null as ContractType | null,
});

// Computed
const hasActiveFilters = computed(() => {
  return (
    filters.value.location ||
    filters.value.workMode !== null ||
    filters.value.contractType !== null ||
    searchTerm.value
  );
});

const workModeOptions = computed(() =>
  Object.entries(workModeLabels).map(([value, label]) => ({
    label,
    value: parseInt(value) as WorkMode,
  })),
);

const contractTypeOptions = computed(() =>
  Object.entries(contractTypeLabels).map(([value, label]) => ({
    label,
    value: parseInt(value) as ContractType,
  })),
);

// Mock location options - could be fetched from API
const locationOptions = [
  { label: 'Paris', value: 'Paris' },
  { label: 'Lyon', value: 'Lyon' },
  { label: 'Marseille', value: 'Marseille' },
  { label: 'Toulouse', value: 'Toulouse' },
  { label: 'Nice', value: 'Nice' },
  { label: 'Nantes', value: 'Nantes' },
  { label: 'Strasbourg', value: 'Strasbourg' },
  { label: 'Montpellier', value: 'Montpellier' },
  { label: 'Bordeaux', value: 'Bordeaux' },
  { label: 'Lille', value: 'Lille' },
];

// Methods
const loadJobOffers = async () => {
  await jobOfferStore.fetchJobOffers();
};

const handleSearch = async () => {
  currentPage.value = 1;
  if (searchTerm.value.trim()) {
    await jobOfferStore.searchJobOffers(searchTerm.value);
  } else {
    await loadJobOffers();
  }
};

const handleFilter = async () => {
  currentPage.value = 1;
  const filterData = {
    searchTerms: searchTerm.value,
    location: filters.value.location || undefined,
    workMode: filters.value.workMode || undefined,
    contractType: filters.value.contractType || undefined,
  };

  await jobOfferStore.filterJobOffers(filterData);
};

const resetFilters = async () => {
  searchTerm.value = '';
  filters.value = {
    location: null,
    workMode: null,
    contractType: null,
  };
  currentPage.value = 1;
  jobOfferStore.resetFilters();
  await loadJobOffers();
};

const handlePageChange = async (page: number) => {
  currentPage.value = page;
  await jobOfferStore.loadPage(page);
};

const viewJobDetails = (jobId: string) => {
  router.push(`/jobs/${jobId}`);
};

// Watch for pagination changes
watch(
  () => paginationInfo.value.currentPage,
  (newPage) => {
    currentPage.value = newPage;
  },
);

// Lifecycle
onMounted(async () => {
  await loadJobOffers();
});
</script>

<style lang="scss" scoped>
.job-listings-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.page-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  margin-bottom: 0;
}

.filters-section {
  background: white;
  border-bottom: 1px solid #e0e0e0;
}

.filters-panel {
  border: 1px solid #e0e0e0;
}

.results-section {
  flex: 1;
}
</style>
