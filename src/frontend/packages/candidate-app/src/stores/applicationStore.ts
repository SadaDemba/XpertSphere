import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { applicationService } from '../services/applicationService';
import type { ApplicationDto, CreateApplicationDto } from '../models/application';
import { ApplicationStatus } from '../enums';

export const useApplicationStore = defineStore('application', () => {
  // State
  const applications = ref<ApplicationDto[]>([]);
  const currentApplication = ref<ApplicationDto | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Getters
  const hasError = computed(() => error.value !== null);
  const hasApplications = computed(() => applications.value.length > 0);

  const activeApplications = computed(() =>
    applications.value.filter((app) => app.isActive && !app.isCompleted),
  );

  const completedApplications = computed(() => applications.value.filter((app) => app.isCompleted));

  const applicationsByStatus = computed(() => {
    const statusGroups: Record<string, ApplicationDto[]> = {};
    applications.value.forEach((app) => {
      const status = app.currentStatus.toString();
      if (!statusGroups[status]) {
        statusGroups[status] = [];
      }
      statusGroups[status].push(app);
    });
    return statusGroups;
  });

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

  const fetchMyApplications = async (): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const result = await applicationService.getMyApplications();
      if (result) {
        applications.value = result;
        return true;
      } else {
        applications.value = [];
        return true;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement de vos candidatures',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationById = async (id: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const application = await applicationService.getApplicationById(id);
      if (application) {
        currentApplication.value = application;
        return true;
      } else {
        setError('Candidature non trouvée');
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement de la candidature',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const applyToJob = async (createApplicationDto: CreateApplicationDto): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const newApplication = await applicationService.createApplication(createApplicationDto);
      if (newApplication) {
        // Add to the list
        applications.value.unshift(newApplication);
        return true;
      } else {
        setError('Erreur lors de la création de la candidature');
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la candidature');
      return false;
    } finally {
      setLoading(false);
    }
  };

  const updateApplication = async (
    id: string,
    coverLetter?: string,
    additionalNotes?: string,
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const updatedApplication = await applicationService.updateApplication(
        id,
        coverLetter,
        additionalNotes,
      );
      if (updatedApplication) {
        // Update in the list
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = updatedApplication;
        }

        // Update current application if it's the same
        if (currentApplication.value?.id === id) {
          currentApplication.value = updatedApplication;
        }
        return true;
      } else {
        setError('Erreur lors de la mise à jour de la candidature');
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la mise à jour');
      return false;
    } finally {
      setLoading(false);
    }
  };

  const withdrawApplication = async (id: string, reason: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      await applicationService.withdrawApplication(id, reason);

      // Update the status in the list
      const application = applications.value.find((app) => app.id === id);
      if (application) {
        application.currentStatus = ApplicationStatus.Withdrawn;
        application.isActive = false;
        application.isCompleted = true;
      }

      // Update current application if it's the same
      if (currentApplication.value?.id === id) {
        currentApplication.value.currentStatus = ApplicationStatus.Withdrawn;
        currentApplication.value.isActive = false;
        currentApplication.value.isCompleted = true;
      }

      return true;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du retrait de la candidature');
      return false;
    } finally {
      setLoading(false);
    }
  };

  const checkIfApplied = async (jobOfferId: string): Promise<boolean> => {
    try {
      return await applicationService.hasAppliedToJob(jobOfferId);
    } catch (error) {
      console.error('Error checking application status:', error);
      return false;
    }
  };

  const clearCurrentApplication = () => {
    currentApplication.value = null;
  };

  return {
    // State
    applications: applications,
    currentApplication: currentApplication,
    isLoading: isLoading,
    error: error,

    // Getters
    hasError,
    hasApplications,
    activeApplications,
    completedApplications,
    applicationsByStatus,

    // Actions
    clearError,
    fetchMyApplications,
    fetchApplicationById,
    applyToJob,
    updateApplication,
    withdrawApplication,
    checkIfApplied,
    clearCurrentApplication,
  };
});
