import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  ApplicationDto,
  CreateApplicationDto,
  UpdateApplicationDto,
  UpdateApplicationStatusDto,
  ApplicationFilterDto,
  AssignUserDto,
  ApplicationStatusHistoryDto,
} from '../models/application';
import { ApplicationService } from '../services/applicationService';
import { applicationStatusHistoryService } from '../services/applicationStatusHistoryService';
import { ApplicationStatus } from '../enums';
import { useAuthStore } from './authStore';

export const useApplicationStore = defineStore('application', () => {
  const service = new ApplicationService();

  const applications = ref<ApplicationDto[]>([]);
  const currentApplication = ref<ApplicationDto | null>(null);
  const applicationHistory = ref<ApplicationStatusHistoryDto[]>([]);
  const statusHistory = ref<ApplicationStatusHistoryDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const totalPages = ref(0);

  const appliedApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.Applied),
  );

  const reviewedApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.Reviewed),
  );

  const technicalTestApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.TechnicalTest),
  );

  const offerMadeApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.OfferMade),
  );

  const acceptedApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.Accepted),
  );

  const rejectedApplications = computed(() =>
    applications.value.filter((app) => app.currentStatus === ApplicationStatus.Rejected),
  );

  const isLoading = computed(() => loading.value);
  const hasError = computed(() => error.value !== null);
  const errorMessage = computed(() => error.value);

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

  const fetchAllApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getAllApplications();
      applications.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchPaginatedApplications = async (
    filter: ApplicationFilterDto = { organizationId: '' },
  ) => {
    try {
      setLoading(true);
      clearError();

      const authStore = useAuthStore();
      const organizationId = authStore.user?.organizationId ?? '';

      const paginationFilter: ApplicationFilterDto = {
        ...filter,
        organizationId: organizationId,
        page: filter.page || currentPage.value,
        pageSize: filter.pageSize || pageSize.value,
      };

      const data = await service.getPaginatedApplications(paginationFilter);

      applications.value = data.items;
      totalCount.value = data.pagination.totalItems;
      currentPage.value = data.pagination.currentPage;
      pageSize.value = data.pagination.pageSize;
      totalPages.value = data.pagination.totalPages;
    } catch (err) {
      setError(`Erreur lors du chargement des candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getApplicationById(id);
      currentApplication.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de la candidature: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const createApplication = async (applicationData: CreateApplicationDto) => {
    try {
      setLoading(true);
      clearError();
      const newApplication = await service.createApplication(applicationData);
      if (newApplication) {
        applications.value.unshift(newApplication);
        totalCount.value++;
      }
      return newApplication;
    } catch (err) {
      setError(`Erreur lors de la création de la candidature: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateApplication = async (id: string, applicationData: UpdateApplicationDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedApplication = await service.updateApplication(id, applicationData);

      if (updatedApplication) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = updatedApplication;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = updatedApplication;
        }
      }

      return updatedApplication;
    } catch (err) {
      setError(`Erreur lors de la mise à jour de la candidature: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteApplication = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deleteApplication(id);

      if (success) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = null;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression de la candidature: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const updateApplicationStatus = async (id: string, statusUpdate: UpdateApplicationStatusDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedApplication = await service.updateApplicationStatus(id, statusUpdate);

      if (updatedApplication) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = updatedApplication;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = updatedApplication;
        }
      }

      return updatedApplication;
    } catch (err) {
      setError(`Erreur lors de la mise à jour du statut: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const withdrawApplication = async (id: string, reason: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.withdrawApplication(id, reason);

      if (success) {
        const application = applications.value.find((app) => app.id === id);
        if (application) {
          application.currentStatus = ApplicationStatus.Withdrawn;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value.currentStatus = ApplicationStatus.Withdrawn;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors du retrait de la candidature: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationsByJobOffer = async (jobOfferId: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getApplicationsByJobOffer(jobOfferId);
      applications.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchMyCandidateApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getMyCandidateApplications();
      applications.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement de vos candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationsByCandidate = async (candidateId: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getApplicationsByCandidate(candidateId);
      applications.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationsByOrganization = async (organizationId?: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getApplicationsByOrganization(organizationId);
      applications.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des candidatures: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationStatusHistory = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getApplicationStatusHistory(id);
      applicationHistory.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de l'historique: ${err}`);
      return [];
    } finally {
      setLoading(false);
    }
  };

  const fetchStatusHistory = async (applicationId: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await applicationStatusHistoryService.getByApplicationId(applicationId);
      statusHistory.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de l'historique: ${err}`);
      return [];
    } finally {
      setLoading(false);
    }
  };

  const checkCanManageApplication = async (id: string) => {
    try {
      return await service.canManageApplication(id);
    } catch (err) {
      setError(`Erreur lors de la vérification des permissions: ${err}`);
      return false;
    }
  };

  const checkHasAppliedToJob = async (jobOfferId: string) => {
    try {
      return await service.hasAppliedToJob(jobOfferId);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const checkHasCandidateAppliedToJob = async (jobOfferId: string, candidateId: string) => {
    try {
      return await service.hasCandidateAppliedToJob(jobOfferId, candidateId);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const assignUser = async (assignUserDto: AssignUserDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedApplication = await service.assignUser(assignUserDto);

      if (updatedApplication) {
        const index = applications.value.findIndex((app) => app.id === assignUserDto.applicationId);
        if (index !== -1) {
          applications.value[index] = updatedApplication;
        }

        if (currentApplication.value?.id === assignUserDto.applicationId) {
          currentApplication.value = updatedApplication;
        }
      }

      return updatedApplication;
    } catch (err) {
      setError(`Erreur lors de l'assignation: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const unassignUser = async (unassignUserDto: AssignUserDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedApplication = await service.unassignUser(unassignUserDto);

      if (updatedApplication) {
        const index = applications.value.findIndex(
          (app) => app.id === unassignUserDto.applicationId,
        );
        if (index !== -1) {
          applications.value[index] = updatedApplication;
        }

        if (currentApplication.value?.id === unassignUserDto.applicationId) {
          currentApplication.value = updatedApplication;
        }
      }

      return updatedApplication;
    } catch (err) {
      setError(`Erreur lors de la désassignation: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const resetStore = () => {
    applications.value = [];
    currentApplication.value = null;
    applicationHistory.value = [];
    statusHistory.value = [];
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 10;
    totalPages.value = 0;
  };

  return {
    applications,
    currentApplication,
    applicationHistory,
    statusHistory,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,

    appliedApplications,
    reviewedApplications,
    technicalTestApplications,
    offerMadeApplications,
    acceptedApplications,
    rejectedApplications,
    isLoading,
    hasError,
    errorMessage,

    clearError,
    fetchAllApplications,
    fetchPaginatedApplications,
    fetchApplicationById,
    createApplication,
    updateApplication,
    deleteApplication,
    updateApplicationStatus,
    withdrawApplication,
    fetchApplicationsByJobOffer,
    fetchMyCandidateApplications,
    fetchApplicationsByCandidate,
    fetchApplicationsByOrganization,
    fetchApplicationStatusHistory,
    fetchStatusHistory,
    checkCanManageApplication,
    checkHasAppliedToJob,
    checkHasCandidateAppliedToJob,
    assignUser,
    unassignUser,
    resetStore,
  };
});
