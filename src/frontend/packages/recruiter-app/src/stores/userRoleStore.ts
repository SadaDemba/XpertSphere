import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { UserRoleDto, AssignRoleDto } from '../models/userRole';
import { UserRoleService } from '../services/userRoleService';

export const useUserRoleStore = defineStore('userRole', () => {
  const service = new UserRoleService();

  const userRoles = ref<UserRoleDto[]>([]);
  const roleUsers = ref<UserRoleDto[]>([]);
  const currentUserRoles = ref<UserRoleDto[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

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

  const fetchUserRoles = async (userId: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getUserRoles(userId);
      userRoles.value = data;
      currentUserRoles.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des rôles utilisateur: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchRoleUsers = async (roleId: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getRoleUsers(roleId);
      roleUsers.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des utilisateurs: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const assignRoleToUser = async (assignRoleDto: AssignRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const newUserRole = await service.assignRoleToUser(assignRoleDto);
      if (newUserRole) {
        userRoles.value.push(newUserRole);
        if (currentUserRoles.value.some((ur) => ur.userId === assignRoleDto.userId)) {
          currentUserRoles.value.push(newUserRole);
        }
      }
      return newUserRole;
    } catch (err) {
      setError(`Erreur lors de l'assignation du rôle: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const removeRoleFromUser = async (userRoleId: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.removeRoleFromUser(userRoleId);

      if (success) {
        userRoles.value = userRoles.value.filter((ur) => ur.id !== userRoleId);
        currentUserRoles.value = currentUserRoles.value.filter((ur) => ur.id !== userRoleId);
        roleUsers.value = roleUsers.value.filter((ur) => ur.id !== userRoleId);
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression du rôle: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const updateUserRoleStatus = async (userRoleId: string, isActive: boolean) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.updateUserRoleStatus(userRoleId, isActive);

      if (success) {
        const updateRole = (roles: UserRoleDto[]) => {
          const userRole = roles.find((ur) => ur.id === userRoleId);
          if (userRole) {
            userRole.isActive = isActive;
          }
        };

        updateRole(userRoles.value);
        updateRole(currentUserRoles.value);
        updateRole(roleUsers.value);
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la mise à jour du statut: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const extendUserRole = async (userRoleId: string, newExpiryDate?: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.extendUserRole(userRoleId, newExpiryDate);

      if (success) {
        const updateRole = (roles: UserRoleDto[]) => {
          const userRole = roles.find((ur) => ur.id === userRoleId);
          if (userRole) {
            userRole.expiresAt = newExpiryDate!;
          }
        };

        updateRole(userRoles.value);
        updateRole(currentUserRoles.value);
        updateRole(roleUsers.value);
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de l'extension du rôle: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const checkUserHasRole = async (userId: string, roleName: string) => {
    try {
      return await service.checkUserHasRole(userId, roleName);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const checkUserHasActiveRole = async (userId: string, roleName: string) => {
    try {
      return await service.checkUserHasActiveRole(userId, roleName);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const fetchUserRoleNames = async (userId: string) => {
    try {
      return await service.getUserRoleNames(userId);
    } catch (err) {
      setError(`Erreur lors du chargement des noms de rôles: ${err}`);
      return [];
    }
  };

  const resetStore = () => {
    userRoles.value = [];
    roleUsers.value = [];
    currentUserRoles.value = [];
    loading.value = false;
    error.value = null;
  };

  return {
    userRoles,
    roleUsers,
    currentUserRoles,
    loading,
    error,

    activeUserRoles,
    inactiveUserRoles,
    expiredUserRoles,
    isLoading,
    hasError,
    errorMessage,

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
