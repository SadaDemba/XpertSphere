<template>
  <q-page class="jobs-page">
    <div class="page-header q-pa-md">
      <div class="row items-center justify-between">
        <div>
          <h1 class="text-h4 q-mb-xs">Offres d'emploi</h1>
          <p class="text-subtitle2 text-grey-7 q-ma-none">
            Gérez vos offres d'emploi et suivez les candidatures
          </p>
        </div>
        <q-btn
          color="primary"
          icon="add"
          label="Nouvelle offre"
          aria-label="Créer une nouvelle offre d'emploi"
          @click="createJob"
        />
      </div>
    </div>

    <div class="page-content q-pa-md">
      <div class="jobs-filters q-mb-md">
        <job-filters v-model:filters="filters" @search="handleSearch" @clear="clearFilters" />
      </div>

      <!-- Erreur -->
      <q-banner v-if="jobOfferStore.hasError" class="bg-negative text-white q-mb-md">
        <template #avatar>
          <q-icon name="error" />
        </template>
        {{ jobOfferStore.errorMessage }}
        <template #action>
          <q-btn flat label="Réessayer" @click="loadJobs" />
          <q-btn flat icon="close" @click="jobOfferStore.clearError" />
        </template>
      </q-banner>

      <div class="jobs-list" role="main" aria-label="Liste des offres d'emploi">
        <job-list
          :jobs="jobs"
          :loading="loading"
          :current-page="currentPage"
          :total-pages="totalPages"
          :total-items="totalItems"
          @edit="editJob"
          @delete="deleteJob"
          @duplicate="duplicateJob"
          @view-applications="viewApplications"
          @page-change="handlePageChange"
        />
      </div>
    </div>

    <job-dialog v-model="showJobDialog" :job="selectedJob" @saved="handleJobSaved" />
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useJobOfferStore } from '../../stores/jobOfferStore';
import JobFilters from '../../components/jobs/JobFilters.vue';
import JobList from '../../components/jobs/JobList.vue';
import JobDialog from '../../components/jobs/JobDialog.vue';
import type { JobOffer, JobOfferFilter } from '../../models/job';

const jobOfferStore = useJobOfferStore();

const showJobDialog = ref(false);
const selectedJob = ref<JobOffer | null>(null);

const filters = ref<JobOfferFilter>({});

const jobs = computed(() => jobOfferStore.jobOffers || []);
const loading = computed(() => jobOfferStore.isLoading);
const currentPage = computed(() => jobOfferStore.currentPage);
const totalPages = computed(() => jobOfferStore.totalPages);
const totalItems = computed(() => jobOfferStore.totalCount);

onMounted(() => {
  loadJobs();
});

async function loadJobs() {
  await jobOfferStore.fetchPaginatedJobOffers(filters.value);
}

function createJob() {
  selectedJob.value = null;
  showJobDialog.value = true;
}

function editJob(job: JobOffer) {
  selectedJob.value = job;
  showJobDialog.value = true;
}

async function deleteJob(job: JobOffer) {
  if (confirm(`Êtes-vous sûr de vouloir supprimer "${job.title}" ?`)) {
    await jobOfferStore.deleteJobOffer(job.id);
  }
}

function duplicateJob(job: JobOffer) {
  console.log('Duplicate job:', job.id);
  // TODO: Implémenter la duplication
}

function viewApplications(job: JobOffer) {
  console.log('View applications for job:', job.id);
  // TODO: Naviguer vers les candidatures
}

function handleSearch() {
  loadJobs();
}

function clearFilters() {
  filters.value.title = '';
  filters.value.location = '';
  loadJobs();
}

function handleJobSaved() {
  showJobDialog.value = false;
  loadJobs();
}

function handlePageChange(page: number) {
  filters.value.page = page;
  loadJobs();
}
</script>

<style scoped>
.jobs-page {
  background-color: #f8f9fa;
}

.page-header {
  background: white;
  border-bottom: 1px solid #e0e0e0;
}

.page-content {
  max-width: 1400px;
  margin: 0 auto;
}

@media (max-width: 599px) {
  .page-header .row {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }
}
</style>
