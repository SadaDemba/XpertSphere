import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import {
  type JobOffer,
  type CreateJobOfferDto,
  type UpdateJobOfferDto,
  type JobOfferFilterDto,
  convertJobOffer,
} from '../models/job';
import { jobOfferService } from '../services/jobOfferService';
import { JobOfferStatus } from '../enums';
import { useNotification } from 'src/composables/notification';

export const useJobOfferStore = defineStore('jobOffer', () => {
  const notification = useNotification();

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

  function convertEnums(jobOffers: JobOffer[]): JobOffer[] {
    return jobOffers.map((element) => convertJobOffer(element));
  }

  /**
   * Retrieve all job offers
   */
  const fetchAllJobOffers = async () => {
    try {
      setLoading(true);
      clearError();
      const response = await jobOfferService.getAllJobOffers();
      if (response?.isSuccess) {
        jobOffers.value = convertEnums(response.data!);
      } else {
        setError('Erreur lors du chargement des offres');
        notification.showErrorNotification('Erreur lors du chargement des offres');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des offres',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Retrieve all job offers with pagination and filter
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

      const response = await jobOfferService.getPaginatedJobOffers(paginationFilter);

      if (response?.isSuccess) {
        jobOffers.value = convertEnums(response.data);
        totalCount.value = response.pagination.totalItems;
        currentPage.value = response.pagination.currentPage;
        pageSize.value = response.pagination.pageSize;
        totalPages.value = response.pagination.totalPages;
        hasPrevious.value = response.pagination.hasPrevious;
        hasNext.value = response.pagination.hasNext;

        notification.showSuccessNotification('Offres chargées avec succès');
      } else {
        setError('Erreur lors du chargement des offres');
        notification.showErrorNotification('Erreur lors du chargement des offres');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des offres',
      );
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
      const response = await jobOfferService.getJobOfferById(id);
      if (response?.isSuccess) {
        currentJobOffer.value = convertJobOffer(response.data!);
      } else {
        setError(response?.message || "Erreur lors du chargement de l'offre");
        notification.showErrorNotification(
          response?.message || "Erreur lors du chargement de l'offre",
        );
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des offres',
      );
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
      const response = await jobOfferService.createJobOffer(jobOfferData);
      if (response?.isSuccess) {
        jobOffers.value.unshift(response.data!);
        totalCount.value++;
        notification.showSuccessNotification('Offres créée avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la création');
        notification.showErrorNotification(response?.message || 'Erreur lors de la création');
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de la création de l'offre");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la création de l'offre",
      );
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
      const response = await jobOfferService.updateJobOffer(id, jobOfferData);

      if (response?.isSuccess) {
        const index = jobOffers.value.findIndex((job) => job.id === id);
        if (index !== -1) {
          jobOffers.value[index] = convertJobOffer(response.data!);
        }

        if (currentJobOffer.value?.id === id) {
          currentJobOffer.value = convertJobOffer(response.data!);
        }
        notification.showSuccessNotification('Offre mise à jour avec succès');
      } else {
        setError(response?.message || "Erreur lors de la mise à jour de l'offre");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la mise à jour de l'offre",
        );
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'offre");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'offre",
      );
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
      const response = await jobOfferService.deleteJobOffer(id);

      if (response?.isSuccess) {
        const index = jobOffers.value.findIndex((job) => job.id === id);
        if (index !== -1) {
          jobOffers.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentJobOffer.value?.id === id) {
          currentJobOffer.value = null;
        }
      } else {
        setError(response?.message || "Erreur lors de la suppression de l'offre");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la suppression de l'offre",
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de la suppression de l'offre");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la suppression de l'offre",
      );
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
      const response = await jobOfferService.publishJobOffer(id);

      if (response?.isSuccess) {
        const jobOffer = jobOffers.value.find((job) => job.id === id);
        if (jobOffer) {
          jobOffer.status = JobOfferStatus.Published;
          jobOffer.publishedAt = new Date().toISOString();
        }

        if (currentJobOffer.value?.id === id) {
          currentJobOffer.value.status = JobOfferStatus.Published;
          currentJobOffer.value.publishedAt = new Date().toISOString();
        }
        notification.showSuccessNotification('Offre puliée avec succès');
      } else {
        setError(response?.message || "Erreur lors de la publication de l'offre");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la publication de l'offre",
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de la publication de l'offre");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la publication de l'offre",
      );
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
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de la fermeture de l'offre");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la fermeture de l'offre",
      );
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
      const response = await jobOfferService.getMyJobOffers();
      if (response?.isSuccess) {
        jobOffers.value = convertEnums(response.data!);
      } else {
        setError(response?.message || 'Erreur lors du chargement des offres');
        notification.showErrorNotification(
          response?.message || 'Erreur lors du chargement des offres',
        );
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des offres',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Vérifie si l'utilisateur peut gérer une offre
   */
  const checkCanManageJobOffer = async (id: string) => {
    try {
      const response = await jobOfferService.canManageJobOffer(id);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors de la vérification des permissions');
      }
      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors de la vérification des permissions',
      );
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
