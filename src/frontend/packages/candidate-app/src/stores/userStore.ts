import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { userService } from '../services/userService';
import type { UpdateUserSkillsDto, UpdateUserProfileDto } from '../services/userService';
import type { User } from '../models/auth';
import { useNotification } from 'src/composables/notification';
import { useAuthStore } from './authStore';

/**
 * Merge a profile-update response onto the existing user while FORCE-KEEPING the
 * collections it does not reliably return. These endpoints can send back empty
 * arrays (not just null/undefined) for experiences/trainings and an empty string
 * for skills, so a nullish fallback (`??`) is NOT enough — the sections would
 * still be wiped. Scalar fields and address come from the response.
 */
const preserveUserCollections = (existing: User, incoming: User): User => ({
  ...existing,
  ...incoming,
  // Ne réappliquer que les collections réellement présentes : sous
  // `exactOptionalPropertyTypes`, affecter `undefined` à ces propriétés
  // optionnelles est une erreur de type.
  ...(existing.skills !== undefined && { skills: existing.skills }),
  ...(existing.experiences !== undefined && { experiences: existing.experiences }),
  ...(existing.trainings !== undefined && { trainings: existing.trainings }),
});

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
        // A skills update only changes skills — patch that single field and keep
        // everything else from the in-memory user. The update response does NOT
        // reliably return experiences/trainings/address, so trusting it would
        // wipe those sections until a reload.
        if (currentUser.value && currentUser.value.id === userId) {
          currentUser.value = {
            ...currentUser.value,
            ...(skillsDto.skills !== undefined && { skills: skillsDto.skills }),
          };
        }
        if (authStore.user && authStore.user.id === userId) {
          authStore.setUser({
            ...authStore.user,
            ...(skillsDto.skills !== undefined && { skills: skillsDto.skills }),
          });
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
        // Keep experiences/trainings/skills (the profile-update response does not
        // reliably return them, and may send empty arrays); scalar fields and
        // address come from the response.
        if (currentUser.value && currentUser.value.id === userId) {
          currentUser.value = preserveUserCollections(currentUser.value, response.data!);
        }
        if (authStore.user && authStore.user.id === userId) {
          authStore.setUser(preserveUserCollections(authStore.user, response.data!));
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

  const uploadCv = async (userId: string, cvFile: File): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await userService.uploadCv(userId, cvFile);

      if (response?.isSuccess) {
        notification.showSuccessNotification(response.data?.message || 'CV uploadé avec succès');
        return true;
      } else {
        setError(response?.message || "Erreur lors de l'upload du CV");
        notification.showErrorNotification(response?.message || "Erreur lors de l'upload du CV");
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de l'upload du CV");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de l'upload du CV",
      );
      return false;
    } finally {
      setLoading(false);
    }
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
    uploadCv,
    setCurrentUser,
    clearCurrentUser,
  };
});
