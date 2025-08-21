import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { jobOfferService } from '../services/jobOfferService';
import type { JobOfferDto, JobOfferFilterDto, PaginatedJobOffers } from '../models/job';
import { WorkMode, ContractType, JobOfferStatus } from '../enums';

export const useJobOfferStore = defineStore('jobOffer', () => {
  // State
  const jobOffers = ref<JobOfferDto[]>([]);
  const currentJobOffer = ref<JobOfferDto | null>(null);
  const paginationInfo = ref({
    currentPage: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
    hasPrevious: false,
    hasNext: false,
  });
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Current filter state
  const currentFilter = ref<JobOfferFilterDto>({
    pageNumber: 1,
    pageSize: 10,
    status: JobOfferStatus.Published, // Only show published jobs for candidates
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

      // Merge with current filter, ensuring we only get published jobs
      const searchFilter: JobOfferFilterDto = {
        ...currentFilter.value,
        ...filter,
        status: JobOfferStatus.Published,
        isActive: true,
        isExpired: false,
      };

      const response = await jobOfferService.getAllPaginatedJobOffers(searchFilter);

      if (response) {
        jobOffers.value = response.items;
        if (response.pagination) {
          paginationInfo.value = response.pagination;
        }
        currentFilter.value = searchFilter;
        return true;
      } else {
        setError("Aucune offre d'emploi trouvée");
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des offres');
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
      if (jobOffer) {
        currentJobOffer.value = jobOffer;
        return true;
      } else {
        setError("Offre d'emploi non trouvée");
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors du chargement de l'offre");
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
      status: JobOfferStatus.Published,
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
