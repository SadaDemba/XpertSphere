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
          @update-status="(job: JobOffer, status: JobOfferStatus) => updateJobStatus(job, status)"
          @page-change="handlePageChange"
        />
      </div>
    </div>

    <job-dialog v-model="showJobDialog" :job="selectedJob" @saved="handleJobSaved" />
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useJobOfferStore } from '../../stores/jobOfferStore';
import { useDialog } from 'src/composables/dialog';
import JobFilters from '../../components/jobs/JobFilters.vue';
import JobList from '../../components/jobs/JobList.vue';
import JobDialog from '../../components/jobs/JobDialog.vue';
import type { JobOffer, JobOfferFilter, CreateJobOfferDto } from '../../models/job';
import { JobOfferStatus } from '../../enums';

const router = useRouter();
const jobOfferStore = useJobOfferStore();
const dialog = useDialog();

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
  router.push(`/jobs/${job.id}`);
}

async function deleteJob(job: JobOffer) {
  try {
    await dialog.confirmDelete(job.title, 'offre');
    await jobOfferStore.deleteJobOffer(job.id);
  } catch {
    // Annulé par l'utilisateur — ne rien faire
  }
}

async function duplicateJob(job: JobOffer) {
  const duplicated: Partial<JobOffer> = {
    ...job,
    title: `${job.title} (Copie)`,
  };
  delete duplicated.id;

  const result = await jobOfferStore.createJobOffer(duplicated as CreateJobOfferDto);
  if (result) {
    await loadJobs();
  }
}

function viewApplications(job: JobOffer) {
  router.push(`/applications?jobId=${job.id}`);
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

async function updateJobStatus(job: JobOffer, newStatus: JobOfferStatus) {
  if (newStatus === JobOfferStatus.Published) {
    await jobOfferStore.publishJobOffer(job.id);
  } else if (newStatus === JobOfferStatus.Closed) {
    await jobOfferStore.closeJobOffer(job.id);
  }
  // Draft : pas de méthode disponible pour l'instant
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
