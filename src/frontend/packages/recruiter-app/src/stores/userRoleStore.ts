import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { UserRoleDto, AssignRoleDto } from '../models/userRole';
import { userRoleService } from '../services/userRoleService';
import { useNotification } from 'src/composables/notification';

export const useUserRoleStore = defineStore('userRole', () => {
  const notification = useNotification();

  // State
  const userRoles = ref<UserRoleDto[]>([]);
  const roleUsers = ref<UserRoleDto[]>([]);
  const currentUserRoles = ref<UserRoleDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  // Getters (computed)
  const activeUserRoles = computed(() => userRoles.value.filter((userRole) => userRole.isActive));

  const inactiveUserRoles = computed(() =>
    userRoles.value.filter((userRole) => !userRole.isActive),
  );

  const expiredUserRoles = computed(() =>
    userRoles.value.filter(
      (userRole) => userRole.expiresAt && new Date(userRole.expiresAt) < new Date(),
    ),
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
   * Fetch user roles
   */
  const fetchUserRoles = async (userId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.getUserRoles(userId);
      if (response?.isSuccess) {
        userRoles.value = response.data!;
        currentUserRoles.value = response.data!;
      } else {
        setError('Erreur lors du chargement des rôles utilisateur');
        notification.showErrorNotification('Erreur lors du chargement des rôles utilisateur');
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement des rôles utilisateur',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des rôles utilisateur',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Fetch role users
   */
  const fetchRoleUsers = async (roleId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.getRoleUsers(roleId);
      if (response?.isSuccess) {
        roleUsers.value = response.data!;
      } else {
        setError('Erreur lors du chargement des utilisateurs du rôle');
        notification.showErrorNotification('Erreur lors du chargement des utilisateurs du rôle');
      }
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : 'Erreur lors du chargement des utilisateurs du rôle',
      );
      notification.showErrorNotification(
        error instanceof Error
          ? error.message
          : 'Erreur lors du chargement des utilisateurs du rôle',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Assign role to user
   */
  const assignRoleToUser = async (assignRoleDto: AssignRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.assignRoleToUser(assignRoleDto);
      console.log(response);
      if (response?.isSuccess && response) {
        const newUserRole = response.data!;
        userRoles.value.push(newUserRole);
        if (currentUserRoles.value.some((ur) => ur.userId === assignRoleDto.userId)) {
          currentUserRoles.value.push(newUserRole);
        }
        notification.showSuccessNotification('Rôle assigné avec succès');
      } else {
        setError(response?.message || "Erreur lors de l'assignation du rôle");
        notification.showErrorNotification(
          response?.message || "Erreur lors de l'assignation du rôle",
        );
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de l'assignation du rôle");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de l'assignation du rôle",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Remove role from user
   */
  const removeRoleFromUser = async (userRoleId: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.removeRoleFromUser(userRoleId);

      if (response?.isSuccess) {
        userRoles.value = userRoles.value.filter((ur) => ur.id !== userRoleId);
        currentUserRoles.value = currentUserRoles.value.filter((ur) => ur.id !== userRoleId);
        roleUsers.value = roleUsers.value.filter((ur) => ur.id !== userRoleId);

        notification.showSuccessNotification('Rôle retiré avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la suppression du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la suppression du rôle',
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la suppression du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la suppression du rôle',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Update user role status
   */
  const updateUserRoleStatus = async (userRoleId: string, isActive: boolean) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.updateUserRoleStatus(userRoleId, isActive);

      if (response?.isSuccess) {
        const updateRole = (roles: UserRoleDto[]) => {
          const userRole = roles.find((ur) => ur.id === userRoleId);
          if (userRole) {
            userRole.isActive = isActive;
          }
        };

        updateRole(userRoles.value);
        updateRole(currentUserRoles.value);
        updateRole(roleUsers.value);

        notification.showSuccessNotification(
          isActive
            ? 'Rôle utilisateur activé avec succès'
            : 'Rôle utilisateur désactivé avec succès',
        );
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour du statut du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour du statut du rôle',
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour du statut du rôle',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour du statut du rôle',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Extend user role expiry
   */
  const extendUserRole = async (userRoleId: string, newExpiryDate?: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await userRoleService.extendUserRole(userRoleId, newExpiryDate);

      if (response?.isSuccess) {
        const updateRole = (roles: UserRoleDto[]) => {
          const userRole = roles.find((ur) => ur.id === userRoleId);
          if (userRole) {
            userRole.expiresAt = newExpiryDate!;
          }
        };

        updateRole(userRoles.value);
        updateRole(currentUserRoles.value);
        updateRole(roleUsers.value);

        notification.showSuccessNotification('Rôle utilisateur prolongé avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la prolongation du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la prolongation du rôle',
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la prolongation du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la prolongation du rôle',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Check if user has role
   */
  const checkUserHasRole = async (userId: string, roleName: string) => {
    try {
      const response = await userRoleService.checkUserHasRole(userId, roleName);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors de la vérification du rôle utilisateur');
      }
      return response?.data;
    } catch (err) {
      setError(`Erreur lors de la vérification du rôle utilisateur: ${err}`);
      return false;
    }
  };

  /**
   * Check if user has active role
   */
  const checkUserHasActiveRole = async (userId: string, roleName: string) => {
    try {
      const response = await userRoleService.checkUserHasActiveRole(userId, roleName);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors de la vérification du rôle actif');
      }
      return response?.data;
    } catch (err) {
      setError(`Erreur lors de la vérification du rôle actif: ${err}`);
      return false;
    }
  };

  /**
   * Fetch user role names
   */
  const fetchUserRoleNames = async (userId: string) => {
    try {
      const response = await userRoleService.getUserRoleNames(userId);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors du chargement des noms de rôles');
      }
      return response?.data || [];
    } catch (err) {
      setError(`Erreur lors du chargement des noms de rôles: ${err}`);
      return [];
    }
  };

  /**
   * Reset the store
   */
  const resetStore = () => {
    userRoles.value = [];
    roleUsers.value = [];
    currentUserRoles.value = [];
    loading.value = false;
    error.value = null;
  };

  return {
    // State
    userRoles,
    roleUsers,
    currentUserRoles,
    loading,
    error,

    // Getters
    activeUserRoles,
    inactiveUserRoles,
    expiredUserRoles,
    isLoading,
    hasError,
    errorMessage,

    // Actions
    clearError,
    fetchUserRoles,
    fetchRoleUsers,
    assignRoleToUser,
    removeRoleFromUser,
    updateUserRoleStatus,
    extendUserRole,
    checkUserHasRole,
    checkUserHasActiveRole,
    fetchUserRoleNames,
    resetStore,
  };
});
