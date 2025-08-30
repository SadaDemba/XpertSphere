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
        setError('Error loading user roles');
        notification.showErrorNotification('Error loading user roles');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading user roles');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading user roles',
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
        setError('Error loading role users');
        notification.showErrorNotification('Error loading role users');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error loading role users');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error loading role users',
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
      if (response?.isSuccess) {
        const newUserRole = response.data!;
        userRoles.value.push(newUserRole);
        if (currentUserRoles.value.some((ur) => ur.userId === assignRoleDto.userId)) {
          currentUserRoles.value.push(newUserRole);
        }
        notification.showSuccessNotification('Role assigned successfully');
      } else {
        setError(response?.message || 'Error assigning role');
        notification.showErrorNotification(response?.message || 'Error assigning role');
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error assigning role');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error assigning role',
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
        notification.showSuccessNotification('Role removed successfully');
      } else {
        setError(response?.message || 'Error removing role');
        notification.showErrorNotification(response?.message || 'Error removing role');
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error removing role');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error removing role',
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
          isActive ? 'User role activated successfully' : 'User role deactivated successfully',
        );
      } else {
        setError(response?.message || 'Error updating role status');
        notification.showErrorNotification(response?.message || 'Error updating role status');
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error updating role status');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error updating role status',
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

        notification.showSuccessNotification('User role extended successfully');
      } else {
        setError(response?.message || 'Error extending role');
        notification.showErrorNotification(response?.message || 'Error extending role');
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Error extending role');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Error extending role',
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
        setError(response?.message || 'Error checking user role');
      }
      return response?.data;
    } catch (err) {
      setError(`Error checking user role: ${err}`);
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
        setError(response?.message || 'Error checking active role');
      }
      return response?.data;
    } catch (err) {
      setError(`Error checking active role: ${err}`);
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
        setError(response?.message || 'Error loading role names');
      }
      return response?.data || [];
    } catch (err) {
      setError(`Error loading role names: ${err}`);
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
