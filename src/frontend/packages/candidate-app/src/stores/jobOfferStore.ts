import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { jobOfferService } from '../services/jobOfferService';
import type { JobOfferDto, JobOfferFilterDto } from '../models/job';
import { JobOfferStatus } from '../enums';
import type { Pagination } from 'src/models';
import { useNotification } from 'src/composables/notification';

export const useJobOfferStore = defineStore('jobOffer', () => {
  // State
  const jobOffers = ref<JobOfferDto[]>([]);
  const currentJobOffer = ref<JobOfferDto | null>(null);
  const paginationInfo = ref<Pagination>({
    currentPage: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
    hasPrevious: false,
    hasNext: false,
  });
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const notification = useNotification();
  // Current filter state
  const currentFilter = ref<JobOfferFilterDto>({
    pageNumber: 1,
    pageSize: 10,
    status: 'Published',
  });

  // Getters
  const hasError = computed(() => error.value !== null);
  const hasJobOffers = computed(() => jobOffers.value.length > 0);
  const publishedJobOffers = computed(() =>
    jobOffers.value.filter(
      (job) => job.status === JobOfferStatus.Published && job.isActive && !job.isExpired,
    ),
  );

  // Actions
  const clearError = () => {
    error.value = null;
  };

  const setLoading = (loading: boolean) => {
    isLoading.value = loading;
  };

  const setError = (message: string) => {
    error.value = message;
    isLoading.value = false;
  };

  const fetchJobOffers = async (filter?: JobOfferFilterDto): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const searchFilter: JobOfferFilterDto = {
        ...currentFilter.value,
        ...filter,
        status: 'Published',
        isActive: true,
        isExpired: false,
      };

      const response = await jobOfferService.getAllPaginatedJobOffers(searchFilter);

      if (response?.isSuccess) {
        jobOffers.value = response.data!;
        paginationInfo.value = response.pagination;
        currentFilter.value = searchFilter;
        notification.showSuccessNotification("Offres d'emploies chargées avec succès");
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la chargement des offres');
        notification.showSuccessNotification(
          response?.message || 'Erreur lors du chargement des offres',
        );
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des offres',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const fetchJobOfferById = async (id: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const jobOffer = await jobOfferService.getJobOfferById(id);
      if (jobOffer?.isSuccess) {
        currentJobOffer.value = jobOffer.data!;
        notification.showSuccessNotification('Offre récupérée avec succès');
        return true;
      } else {
        setError(jobOffer?.message || "Erreur lors du chargement de l'offre");
        notification.showErrorNotification(
          jobOffer?.message || "Erreur lors du chargement de l'offre",
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur innatendue lors du chargement de l'offre",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const searchJobOffers = async (searchTerms: string): Promise<boolean> => {
    return fetchJobOffers({
      ...currentFilter.value,
      searchTerms,
      pageNumber: 1,
    });
  };

  const filterJobOffers = async (filter: Partial<JobOfferFilterDto>): Promise<boolean> => {
    return fetchJobOffers({
      ...currentFilter.value,
      ...filter,
      pageNumber: 1,
    });
  };

  const loadPage = async (page: number): Promise<boolean> => {
    return fetchJobOffers({
      ...currentFilter.value,
      pageNumber: page,
    });
  };

  const clearCurrentJobOffer = () => {
    currentJobOffer.value = null;
  };

  const resetFilters = () => {
    currentFilter.value = {
      pageNumber: 1,
      pageSize: 10,
      status: 'Published',
    };
  };

  return {
    // State
    jobOffers: jobOffers,
    currentJobOffer: currentJobOffer,
    paginationInfo: paginationInfo,
    isLoading: isLoading,
    error: error,
    currentFilter: currentFilter,

    // Getters
    hasError,
    hasJobOffers,
    publishedJobOffers,

    // Actions
    clearError,
    fetchJobOffers,
    fetchJobOfferById,
    searchJobOffers,
    filterJobOffers,
    loadPage,
    clearCurrentJobOffer,
    resetFilters,
  };
});
