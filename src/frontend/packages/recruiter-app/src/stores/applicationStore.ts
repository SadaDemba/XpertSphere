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
import { applicationService } from '../services/applicationService';
import { applicationStatusHistoryService } from '../services/applicationStatusHistoryService';
import { ApplicationStatus } from '../enums';
import { useAuthStore } from './authStore';
import { useNotification } from 'src/composables/notification';

export const useApplicationStore = defineStore('application', () => {
  const notification = useNotification();

  // State
  const applications = ref<ApplicationDto[]>([]);
  const currentApplication = ref<ApplicationDto | null>(null);
  const applicationHistory = ref<ApplicationStatusHistoryDto[]>([]);
  const statusHistory = ref<ApplicationStatusHistoryDto[]>([]);
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
   * Retrieve all applications
   */
  const fetchAllApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getAllApplications();
      if (response?.isSuccess) {
        applications.value = response.data!;
      } else {
        setError('Error loading applications');
        notification.showErrorNotification('Error loading applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Retrieve paginated applications with filter
   */
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

      const response = await applicationService.getPaginatedApplications(paginationFilter);

      if (response?.isSuccess) {
        applications.value = response.items;
        totalCount.value = response.pagination.totalItems;
        currentPage.value = response.pagination.currentPage;
        pageSize.value = response.pagination.pageSize;
        totalPages.value = response.pagination.totalPages;
        hasPrevious.value = response.pagination.hasPrevious;
        hasNext.value = response.pagination.hasNext;

        notification.showSuccessNotification('Applications loaded successfully');
      } else {
        setError('Error loading applications');
        notification.showErrorNotification('Error loading applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Get application by ID
   */
  const fetchApplicationById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getApplicationById(id);
      if (response?.isSuccess) {
        currentApplication.value = response.data!;
      } else {
        setError(response?.message || 'Error loading application');
        notification.showErrorNotification(response?.message || 'Error loading application');
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading application');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading application',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Create a new application
   */
  const createApplication = async (applicationData: CreateApplicationDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.createApplication(applicationData);
      if (response?.isSuccess) {
        applications.value.unshift(response.data!);
        totalCount.value++;
        notification.showSuccessNotification('Application created successfully');
      } else {
        setError(response?.message || 'Error creating application');
        notification.showErrorNotification(response?.message || 'Error creating application');
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error creating application');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error creating application',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Update an application
   */
  const updateApplication = async (id: string, applicationData: UpdateApplicationDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.updateApplication(id, applicationData);

      if (response?.isSuccess) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = response.data!;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = response.data!;
        }
        notification.showSuccessNotification('Application updated successfully');
      } else {
        setError(response?.message || 'Error updating application');
        notification.showErrorNotification(response?.message || 'Error updating application');
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error updating application');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error updating application',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Delete an application
   */
  const deleteApplication = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.deleteApplication(id);

      if (response?.isSuccess) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = null;
        }
        notification.showSuccessNotification('Application deleted successfully');
      } else {
        setError(response?.message || 'Error deleting application');
        notification.showErrorNotification(response?.message || 'Error deleting application');
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error deleting application');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error deleting application',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Update application status
   */
  const updateApplicationStatus = async (id: string, statusUpdate: UpdateApplicationStatusDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.updateApplicationStatus(id, statusUpdate);

      if (response?.isSuccess) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = response.data!;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = response.data!;
        }
        notification.showSuccessNotification('Status updated successfully');
      } else {
        setError(response?.message || 'Error updating status');
        notification.showErrorNotification(response?.message || 'Error updating status');
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error updating status');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error updating status',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Withdraw an application
   */
  const withdrawApplication = async (id: string, reason: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.withdrawApplication(id, reason);

      if (response?.isSuccess) {
        const application = applications.value.find((app) => app.id === id);
        if (application) {
          application.currentStatus = ApplicationStatus.Withdrawn;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value.currentStatus = ApplicationStatus.Withdrawn;
        }
        notification.showSuccessNotification('Application withdrawn successfully');
      } else {
        setError(response?.message || 'Error withdrawing application');
        notification.showErrorNotification(response?.message || 'Error withdrawing application');
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error withdrawing application');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error withdrawing application',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch applications by job offer
   */
  const fetchApplicationsByJobOffer = async (jobOfferId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getApplicationsByJobOffer(jobOfferId);
      if (response?.isSuccess) {
        applications.value = response.data!;
      } else {
        setError('Error loading applications');
        notification.showErrorNotification('Error loading applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch current candidate's applications
   */
  const fetchMyCandidateApplications = async () => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getMyCandidateApplications();
      if (response?.isSuccess) {
        applications.value = response.data!;
      } else {
        setError('Error loading your applications');
        notification.showErrorNotification('Error loading your applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading your applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading your applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch applications by candidate
   */
  const fetchApplicationsByCandidate = async (candidateId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getApplicationsByCandidate(candidateId);
      if (response?.isSuccess) {
        applications.value = response.data!;
      } else {
        setError('Error loading applications');
        notification.showErrorNotification('Error loading applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch applications by organization
   */
  const fetchApplicationsByOrganization = async (organizationId?: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getApplicationsByOrganization(organizationId);
      if (response?.isSuccess) {
        applications.value = response.data!;
      } else {
        setError('Error loading applications');
        notification.showErrorNotification('Error loading applications');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading applications');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading applications',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch application status history
   */
  const fetchApplicationStatusHistory = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.getApplicationStatusHistory(id);
      if (response?.isSuccess) {
        applicationHistory.value = response.data!;
      } else {
        setError('Error loading history');
        notification.showErrorNotification('Error loading history');
      }
      return response?.data || [];
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading history');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading history',
      );
      return [];
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch status history from history service
   */
  const fetchStatusHistory = async (applicationId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationStatusHistoryService.getByApplicationId(applicationId);
      if (response?.isSuccess) {
        statusHistory.value = response.data!;
      } else {
        setError('Error loading history');
        notification.showErrorNotification('Error loading history');
      }
      return response?.data || [];
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading history');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading history',
      );
      return [];
    } finally {
      setLoading(false);
    }
  };

  /**
   * Check if user can manage application
   */
  const checkCanManageApplication = async (id: string) => {
    try {
      const response = await applicationService.canManageApplication(id);
      if (!response?.isSuccess) {
        setError(response?.message || 'Error checking permissions');
      }
      return response?.data;
    } catch (err) {
      setError(`Error checking permissions: ${err}`);
      return false;
    }
  };

  /**
   * Check if user has applied to job
   */
  const checkHasAppliedToJob = async (jobOfferId: string) => {
    try {
      const response = await applicationService.hasAppliedToJob(jobOfferId);
      if (!response?.isSuccess) {
        setError(response?.message || 'Error checking application');
      }
      return response?.data;
    } catch (err) {
      setError(`Error checking application: ${err}`);
      return false;
    }
  };

  /**
   * Check if candidate has applied to job
   */
  const checkHasCandidateAppliedToJob = async (jobOfferId: string, candidateId: string) => {
    try {
      const response = await applicationService.hasCandidateAppliedToJob(jobOfferId, candidateId);
      if (!response?.isSuccess) {
        setError(response?.message || 'Error checking application');
      }
      return response?.data;
    } catch (err) {
      setError(`Error checking application: ${err}`);
      return false;
    }
  };

  /**
   * Assign user to application
   */
  const assignUser = async (assignUserDto: AssignUserDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.assignUser(assignUserDto);

      if (response?.isSuccess) {
        const index = applications.value.findIndex((app) => app.id === assignUserDto.applicationId);
        if (index !== -1) {
          applications.value[index] = response.data!;
        }

        if (currentApplication.value?.id === assignUserDto.applicationId) {
          currentApplication.value = response.data!;
        }
        notification.showSuccessNotification('User assigned successfully');
      } else {
        setError(response?.message || 'Error assigning user');
        notification.showErrorNotification(response?.message || 'Error assigning user');
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error assigning user');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error assigning user',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Unassign user from application
   */
  const unassignUser = async (unassignUserDto: AssignUserDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await applicationService.unassignUser(unassignUserDto);

      if (response?.isSuccess) {
        const index = applications.value.findIndex(
          (app) => app.id === unassignUserDto.applicationId,
        );
        if (index !== -1) {
          applications.value[index] = response.data!;
        }

        if (currentApplication.value?.id === unassignUserDto.applicationId) {
          currentApplication.value = response.data!;
        }
        notification.showSuccessNotification('User unassigned successfully');
      } else {
        setError(response?.message || 'Error unassigning user');
        notification.showErrorNotification(response?.message || 'Error unassigning user');
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error unassigning user');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error unassigning user',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Reset the store
   */
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
    hasPrevious.value = false;
    hasNext.value = false;
  };

  return {
    // State
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
    hasPrevious,
    hasNext,

    // Getters
    appliedApplications,
    reviewedApplications,
    technicalTestApplications,
    offerMadeApplications,
    acceptedApplications,
    rejectedApplications,
    isLoading,
    hasError,
    errorMessage,

    // Actions
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
