import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  JobOffer,
  CreateJobOfferDto,
  UpdateJobOfferDto,
  JobOfferFilterDto,
} from '../models/job';
import { jobOfferService } from '../services/jobOfferService';
import { JobOfferStatus } from '../enums';

export const useJobOfferStore = defineStore('jobOffer', () => {
  // État
  const jobOffers = ref<JobOffer[]>([]);
  const currentJobOffer = ref<JobOffer | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  // Pagination
  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const totalPages = ref(0);
  const hasPrevious = ref(false);
  const hasNext = ref(false);

  // Getters (computed)
  const draftJobOffers = computed(() =>
    jobOffers.value.filter((job) => job.status === JobOfferStatus.Draft),
  );

  const publishedJobOffers = computed(() =>
    jobOffers.value.filter((job) => job.status === JobOfferStatus.Published),
  );

  const closedJobOffers = computed(() =>
    jobOffers.value.filter((job) => job.status === JobOfferStatus.Closed),
  );

  const isLoading = computed(() => loading.value);
  const hasError = computed(() => error.value !== null);
  const errorMessage = computed(() => error.value);

  // Actions
  const clearError = () => {
    error.value = null;
  };

  const setLoading = (state: boolean) => {
    loading.value = state;
  };

  const setError = (message: string) => {
    error.value = message;
    loading.value = false;
  };

  /**
   * Récupère toutes les offres d'emploi
   */
  const fetchAllJobOffers = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await jobOfferService.getAllJobOffers();
      jobOffers.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des offres: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Récupère les offres d'emploi avec pagination et filtres
   */
  const fetchPaginatedJobOffers = async (filter: JobOfferFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const paginationFilter: JobOfferFilterDto = {
        ...filter,
        pageNumber: filter.pageNumber || currentPage.value,
        pageSize: filter.pageSize || pageSize.value,
      };

      const data = await jobOfferService.getPaginatedJobOffers(paginationFilter);

      jobOffers.value = data.items;
      totalCount.value = data.pagination.totalItems;
      currentPage.value = data.pagination.currentPage;
      pageSize.value = data.pagination.pageSize;
      totalPages.value = data.pagination.totalPages;
      hasPrevious.value = data.pagination.hasPrevious;
      hasNext.value = data.pagination.hasNext;
    } catch (err) {
      setError(`Erreur lors du chargement des offres: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Récupère une offre d'emploi par ID
   */
  const fetchJobOfferById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await jobOfferService.getJobOfferById(id);
      currentJobOffer.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de l'offre: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Crée une nouvelle offre d'emploi
   */
  const createJobOffer = async (jobOfferData: CreateJobOfferDto) => {
    try {
      setLoading(true);
      clearError();
      const newJobOffer = await jobOfferService.createJobOffer(jobOfferData);
      if (newJobOffer) {
        jobOffers.value.unshift(newJobOffer);
        totalCount.value++;
      }
      return newJobOffer;
    } catch (err) {
      setError(`Erreur lors de la création de l'offre: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Met à jour une offre d'emploi
   */
  const updateJobOffer = async (id: string, jobOfferData: UpdateJobOfferDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedJobOffer = await jobOfferService.updateJobOffer(id, jobOfferData);

      if (updatedJobOffer) {
        const index = jobOffers.value.findIndex((job) => job.id === id);
        if (index !== -1) {
          jobOffers.value[index] = updatedJobOffer;
        }

        if (currentJobOffer.value?.id === id) {
          currentJobOffer.value = updatedJobOffer;
        }
      }

      return updatedJobOffer;
    } catch (err) {
      setError(`Erreur lors de la mise à jour de l'offre: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Supprime une offre d'emploi
   */
  const deleteJobOffer = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      await jobOfferService.deleteJobOffer(id);

      const index = jobOffers.value.findIndex((job) => job.id === id);
      if (index !== -1) {
        jobOffers.value.splice(index, 1);
        totalCount.value--;
      }

      if (currentJobOffer.value?.id === id) {
        currentJobOffer.value = null;
      }

      return true;
    } catch (err) {
      setError(`Erreur lors de la suppression de l'offre: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Publie une offre d'emploi
   */
  const publishJobOffer = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      await jobOfferService.publishJobOffer(id);

      const jobOffer = jobOffers.value.find((job) => job.id === id);
      if (jobOffer) {
        jobOffer.status = JobOfferStatus.Published;
        jobOffer.publishedAt = new Date().toISOString();
      }

      if (currentJobOffer.value?.id === id) {
        currentJobOffer.value.status = JobOfferStatus.Published;
        currentJobOffer.value.publishedAt = new Date().toISOString();
      }

      return true;
    } catch (err) {
      setError(`Erreur lors de la publication de l'offre: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Ferme une offre d'emploi
   */
  const closeJobOffer = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      await jobOfferService.closeJobOffer(id);

      const jobOffer = jobOffers.value.find((job) => job.id === id);
      if (jobOffer) {
        jobOffer.status = JobOfferStatus.Closed;
      }

      if (currentJobOffer.value?.id === id) {
        currentJobOffer.value.status = JobOfferStatus.Closed;
      }

      return true;
    } catch (err) {
      setError(`Erreur lors de la fermeture de l'offre: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Récupère les offres d'emploi de l'utilisateur actuel
   */
  const fetchMyJobOffers = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await jobOfferService.getMyJobOffers();
      jobOffers.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement de vos offres: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Vérifie si l'utilisateur peut gérer une offre
   */
  const checkCanManageJobOffer = async (id: string) => {
    try {
      return await jobOfferService.canManageJobOffer(id);
    } catch (err) {
      setError(`Erreur lors de la vérification des permissions: ${err}`);
      return false;
    }
  };

  /**
   * Réinitialise le store
   */
  const resetStore = () => {
    jobOffers.value = [];
    currentJobOffer.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 10;
    totalPages.value = 0;
    hasPrevious.value = false;
    hasNext.value = false;
  };

  return {
    // État
    jobOffers,
    currentJobOffer,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,
    hasPrevious,
    hasNext,

    // Getters
    draftJobOffers,
    publishedJobOffers,
    closedJobOffers,
    isLoading,
    hasError,
    errorMessage,

    // Actions
    clearError,
    fetchAllJobOffers,
    fetchPaginatedJobOffers,
    fetchJobOfferById,
    createJobOffer,
    updateJobOffer,
    deleteJobOffer,
    publishJobOffer,
    closeJobOffer,
    fetchMyJobOffers,
    checkCanManageJobOffer,
    resetStore,
  };
});
