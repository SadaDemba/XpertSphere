import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { experienceService } from '../services/experienceService';
import type { ExperienceDto, CreateExperienceDto } from 'src/models/experience';
import { useNotification } from 'src/composables/notification';

export const useExperienceStore = defineStore('experience', () => {
  // State
  const experiences = ref<ExperienceDto[]>([]);
  const currentExperience = ref<ExperienceDto | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const notification = useNotification();

  // Getters
  const hasError = computed(() => error.value !== null);
  const hasExperiences = computed(() => experiences.value.length > 0);
  const currentExperiences = computed(() => experiences.value.filter((exp) => exp.isCurrent));
  const pastExperiences = computed(() => experiences.value.filter((exp) => !exp.isCurrent));

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

  const fetchUserExperiences = async (userId: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await experienceService.getUserExperiences(userId);

      if (response?.isSuccess) {
        experiences.value = response.data!;
        notification.showSuccessNotification('Expériences chargée avec succès');
        return true;
      } else {
        setError(response?.message || 'Erreur lors du chargement des expériences');
        notification.showErrorNotification(
          response?.message || 'Erreur lors du chargement des expériences',
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement des expériences',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des expériences',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const fetchExperienceById = async (id: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const experience = await experienceService.getExperienceById(id);
      if (experience?.isSuccess) {
        currentExperience.value = experience.data!;
        notification.showSuccessNotification('Expérience chargée avec succès');
        return true;
      } else {
        setError(experience?.message || "Erreur lors du chargement de l'expérience");
        notification.showErrorNotification(
          experience?.message || "Erreur lors du chargement de l'expérience",
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : "Erreur inattendue lors du chargement de l'expérience",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const replaceUserExperiences = async (
    userId: string,
    newExperiences: CreateExperienceDto[],
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await experienceService.replaceUserExperiences(userId, newExperiences);

      if (response?.isSuccess) {
        experiences.value = response.data!;
        notification.showSuccessNotification('Expériences mises à jour avec succès');
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour des expériences');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour des expériences',
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des expériences',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des expériences',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const clearCurrentExperience = () => {
    currentExperience.value = null;
  };

  const clearExperiences = () => {
    experiences.value = [];
  };

  return {
    // State
    experiences: experiences,
    currentExperience: currentExperience,
    isLoading: isLoading,
    error: error,

    // Getters
    hasError,
    hasExperiences,
    currentExperiences,
    pastExperiences,

    // Actions
    clearError,
    fetchUserExperiences,
    fetchExperienceById,
    replaceUserExperiences,
    clearCurrentExperience,
    clearExperiences,
  };
});
