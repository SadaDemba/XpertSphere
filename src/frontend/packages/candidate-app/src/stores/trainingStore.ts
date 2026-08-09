import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { trainingService } from '../services/trainingService';
import type { TrainingDto, CreateTrainingDto } from 'src/models/training';
import { useNotification } from 'src/composables/notification';

export const useTrainingStore = defineStore('training', () => {
  // State
  const trainings = ref<TrainingDto[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const notification = useNotification();

  // Getters
  const hasError = computed(() => error.value !== null);
  const hasTrainings = computed(() => trainings.value.length > 0);

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

  const fetchUserTrainings = async (userId: string): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await trainingService.getUserTrainings(userId);

      if (response?.isSuccess) {
        trainings.value = response.data!;
        return true;
      } else {
        setError(response?.message || 'Erreur lors du chargement des formations');
        notification.showErrorNotification(
          response?.message || 'Erreur lors du chargement des formations',
        );
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des formations');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des formations',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const replaceUserTrainings = async (
    userId: string,
    newTrainings: CreateTrainingDto[],
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await trainingService.replaceUserTrainings(userId, newTrainings);

      if (response?.isSuccess) {
        trainings.value = response.data!;
        notification.showSuccessNotification('Formations mises à jour avec succès');
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour des formations');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour des formations',
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des formations',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des formations',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const clearTrainings = () => {
    trainings.value = [];
  };

  return {
    // State
    trainings,
    isLoading,
    error,

    // Getters
    hasError,
    hasTrainings,

    // Actions
    clearError,
    fetchUserTrainings,
    replaceUserTrainings,
    clearTrainings,
  };
});
