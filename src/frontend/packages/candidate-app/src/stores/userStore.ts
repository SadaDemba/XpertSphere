import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { userService } from '../services/userService';
import type { UpdateUserSkillsDto, UpdateUserProfileDto } from '../services/userService';
import type { User } from '../models/auth';
import { useNotification } from 'src/composables/notification';
import { useAuthStore } from './authStore';

export const useUserStore = defineStore('user', () => {
  // State
  const currentUser = ref<User | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const notification = useNotification();
  const authStore = useAuthStore();

  // Getters
  const hasError = computed(() => error.value !== null);

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

  const updateUserSkills = async (
    userId: string,
    skillsDto: UpdateUserSkillsDto,
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await userService.updateUserSkills(userId, skillsDto);

      if (response?.isSuccess) {
        // Update current user if it's the same user
        if (currentUser.value && currentUser.value.id === userId) {
          currentUser.value = response.data!;
        }
        // Update auth store if it's the same user
        if (authStore.user && authStore.user.id === userId) {
          authStore.setUser(response.data!);
        }
        notification.showSuccessNotification('Compétences mises à jour avec succès');
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour des compétences');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour des compétences',
        );
        return false;
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des compétences',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour des compétences',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const updateUserProfile = async (
    userId: string,
    profileDto: UpdateUserProfileDto,
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await userService.updateUserProfile(userId, profileDto);

      if (response?.isSuccess) {
        // Update current user if it's the same user
        if (currentUser.value && currentUser.value.id === userId) {
          currentUser.value = response.data!;
        }
        // Update auth store if it's the same user
        if (authStore.user && authStore.user.id === userId) {
          authStore.setUser(response.data!);
        }
        notification.showSuccessNotification('Profil mis à jour avec succès');
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour du profil');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour du profil',
        );
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la mise à jour du profil');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour du profil',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const setCurrentUser = (user: User | null) => {
    currentUser.value = user;
  };

  const clearCurrentUser = () => {
    currentUser.value = null;
  };

  return {
    // State
    currentUser,
    isLoading,
    error,

    // Getters
    hasError,

    // Actions
    clearError,
    updateUserSkills,
    updateUserProfile,
    setCurrentUser,
    clearCurrentUser,
  };
});
