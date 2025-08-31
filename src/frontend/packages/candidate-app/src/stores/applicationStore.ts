import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { applicationService } from '../services/applicationService';
import type { ApplicationDto, CreateApplicationDto } from '../models/application';
import { ApplicationStatus } from '../enums';
import { useNotification } from 'src/composables/notification';

export const useApplicationStore = defineStore('application', () => {
  // State
  const applications = ref<ApplicationDto[]>([]);
  const currentApplication = ref<ApplicationDto | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const notification = useNotification();

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

  const fetchMyApplications = async (): Promise<void> => {
    try {
      setLoading(true);
      clearError();

      const result = await applicationService.getMyApplications();
      if (result?.isSuccess) {
        applications.value = result.data!;
        notification.showSuccessNotification('Candidatures récupérées avec succès');
      } else {
        applications.value = [];
        setError('Aucune réponse du serveur');
        notification.showErrorNotification(result?.message || 'Aucune réponse du serveur');
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement de vos candidatures',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement de vos candidatures',
      );
    } finally {
      setLoading(false);
    }
  };

  const fetchApplicationById = async (id: string): Promise<void> => {
    try {
      setLoading(true);
      clearError();

      const application = await applicationService.getApplicationById(id);
      if (application?.isSuccess) {
        currentApplication.value = application.data!;
        notification.showSuccessNotification('Candidature récupérée avec succès');
      } else {
        setError(application?.message || 'Candidature non trouvée');
        notification.showErrorNotification(application?.message || 'Candidature non trouvée');
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement de la candidature',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement de la candidature',
      );
    } finally {
      setLoading(false);
    }
  };

  const applyToJob = async (createApplicationDto: CreateApplicationDto): Promise<void> => {
    try {
      setLoading(true);
      clearError();

      const newApplication = await applicationService.createApplication(createApplicationDto);
      if (newApplication?.isSuccess) {
        applications.value.unshift(newApplication.data!);
        notification.showSuccessNotification('Votre candidature a été envoyée avce succès');
      } else {
        setError(newApplication?.message || 'Erreur lors de la création de la candidature');
        notification.showErrorNotification(
          newApplication?.message || 'Erreur lors de la création de la candidature',
        );
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur innatendue lors de la candidature');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur innatendue lors de la candidature',
      );
    } finally {
      setLoading(false);
    }
  };

  const updateApplication = async (
    id: string,
    coverLetter?: string,
    additionalNotes?: string,
  ): Promise<void> => {
    try {
      setLoading(true);
      clearError();

      const updatedApplication = await applicationService.updateApplication(
        id,
        coverLetter,
        additionalNotes,
      );
      if (updatedApplication?.isSuccess) {
        const index = applications.value.findIndex((app) => app.id === id);
        if (index !== -1) {
          applications.value[index] = updatedApplication.data!;
        }

        if (currentApplication.value?.id === id) {
          currentApplication.value = updatedApplication.data!;
        }
        notification.showSuccessNotification('Candidature modifiée avec succès');
      } else {
        setError(updatedApplication?.message || 'Erreur lors de la mise à jour de la candidature');
        notification.showErrorNotification(
          updatedApplication?.message || 'Erreur lors de la mise à jour de la candidature',
        );
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la mise à jour');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour',
      );
    } finally {
      setLoading(false);
    }
  };

  const withdrawApplication = async (id: string, reason: string): Promise<void> => {
    try {
      setLoading(true);
      clearError();

      const withdrawnApplication = await applicationService.withdrawApplication(id, reason);

      if (withdrawnApplication?.isSuccess) {
        // Update the status in the list
        const application = applications.value.find((app) => app.id === id);
        if (application) {
          application.currentStatus = ApplicationStatus.Withdrawn;
          application.isActive = false;
          application.isCompleted = true;

          // Update current application if it's the same
          if (currentApplication.value?.id === id) {
            currentApplication.value = application;
          }
        }
        notification.showSuccessNotification('Candidature retirée avec succès');
      } else {
        setError(withdrawnApplication?.message || 'Erreur lors du retrait de votre candidature');
        notification.showErrorNotification(
          withdrawnApplication?.message || 'Erreur lors du retrait de votre candidature',
        );
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du retrait de la candidature');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du retrait de la candidature',
      );
    } finally {
      setLoading(false);
    }
  };

  const checkIfApplied = async (jobOfferId: string): Promise<boolean> => {
    try {
      const response = await applicationService.hasAppliedToJob(jobOfferId);
      if (response?.isSuccess) {
        //notification.showInfoNotification('Candidature existante');
        return response?.data || false;
      } else {
        setError(
          response?.message || "Erreur lors de la vérification de l'existance de la candidature",
        );
        notification.showInfoNotification(
          response?.message || "Erreur lors de la vérification de l'existance de la candidature",
        );
      }
      return false;
    } catch (error) {
      console.error(
        error instanceof Error
          ? error.message
          : 'Erreur lors de la vérification du statut de la candidature',
      );
      notification.showInfoNotification(
        error instanceof Error
          ? error.message
          : 'Erreur lors de la vérification du statut de la candidature',
      );
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
