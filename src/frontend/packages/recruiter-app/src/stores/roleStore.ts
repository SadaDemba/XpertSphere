import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { RoleDto, CreateRoleDto, UpdateRoleDto, RoleFilterDto } from '../models/role';
import { roleService } from '../services/roleService';
import { useNotification } from 'src/composables/notification';

export const useRoleStore = defineStore('role', () => {
  const notification = useNotification();

  // State
  const roles = ref<RoleDto[]>([]);
  const currentRole = ref<RoleDto | null>(null);
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
  const activeRoles = computed(() => roles.value.filter((role) => role.isActive));

  const inactiveRoles = computed(() => roles.value.filter((role) => !role.isActive));

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
   * Retrieve all roles
   */
  const fetchAllRoles = async () => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.getAllRoles();
      if (response?.isSuccess) {
        roles.value = response.data!;
      } else {
        setError('Erreur lors du chargement des rôles');
        notification.showErrorNotification('Erreur lors du chargement des rôles');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des rôles');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des rôles',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Retrieve all roles with pagination and filter
   */
  const fetchPaginatedRoles = async (filter: RoleFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const paginationFilter: RoleFilterDto = {
        ...filter,
        pageNumber: filter.pageNumber || currentPage.value,
        pageSize: filter.pageSize || pageSize.value,
      };

      const response = await roleService.getPaginatedRoles(paginationFilter);

      if (response?.isSuccess) {
        roles.value = response.data;
        totalCount.value = response.pagination.totalItems;
        currentPage.value = response.pagination.currentPage;
        pageSize.value = response.pagination.pageSize;
        totalPages.value = response.pagination.totalPages;
        hasPrevious.value = response.pagination.hasPrevious;
        hasNext.value = response.pagination.hasNext;

        notification.showSuccessNotification('Rôles chargés avec succès');
      } else {
        setError('Erreur lors du chargement des rôles');
        notification.showErrorNotification('Erreur lors du chargement des rôles');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement des rôles');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des rôles',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Get role by ID
   */
  const fetchRoleById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.getRoleById(id);
      if (response?.isSuccess) {
        currentRole.value = response.data!;
      } else {
        setError(response?.message || 'Erreur lors du chargement du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors du chargement du rôle',
        );
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement du rôle',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Get role by name
   */
  const fetchRoleByName = async (name: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.getRoleByName(name);
      if (response?.isSuccess) {
        currentRole.value = response.data!;
      } else {
        setError(response?.message || 'Erreur lors du chargement du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors du chargement du rôle',
        );
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors du chargement du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement du rôle',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Create a new role
   */
  const createRole = async (roleData: CreateRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.createRole(roleData);
      if (response?.isSuccess) {
        roles.value.unshift(response.data!);
        totalCount.value++;
        notification.showSuccessNotification('Rôle créé avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la création');
        notification.showErrorNotification(response?.message || 'Erreur lors de la création');
      }
      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la création du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la création du rôle',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Update a role
   */
  const updateRole = async (id: string, roleData: UpdateRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.updateRole(id, roleData);

      if (response?.isSuccess) {
        const index = roles.value.findIndex((role) => role.id === id);
        if (index !== -1) {
          roles.value[index] = response.data!;
        }

        if (currentRole.value?.id === id) {
          currentRole.value = response.data!;
        }
        notification.showSuccessNotification('Rôle mis à jour avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la mise à jour du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la mise à jour du rôle',
        );
      }

      return response?.data;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la mise à jour du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la mise à jour du rôle',
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Delete a role
   */
  const deleteRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.deleteRole(id);

      if (response?.isSuccess) {
        const index = roles.value.findIndex((role) => role.id === id);
        if (index !== -1) {
          roles.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentRole.value?.id === id) {
          currentRole.value = null;
        }
        notification.showSuccessNotification('Rôle supprimé avec succès');
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
   * Activate a role
   */
  const activateRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.activateRole(id);

      if (response?.isSuccess) {
        const role = roles.value.find((r) => r.id === id);
        if (role) {
          role.isActive = true;
        }

        if (currentRole.value?.id === id) {
          currentRole.value.isActive = true;
        }
        notification.showSuccessNotification('Rôle activé avec succès');
      } else {
        setError(response?.message || "Erreur lors de l'activation du rôle");
        notification.showErrorNotification(
          response?.message || "Erreur lors de l'activation du rôle",
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de l'activation du rôle");
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de l'activation du rôle",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Deactivate a role
   */
  const deactivateRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await roleService.deactivateRole(id);

      if (response?.isSuccess) {
        const role = roles.value.find((r) => r.id === id);
        if (role) {
          role.isActive = false;
        }

        if (currentRole.value?.id === id) {
          currentRole.value.isActive = false;
        }
        notification.showSuccessNotification('Rôle désactivé avec succès');
      } else {
        setError(response?.message || 'Erreur lors de la désactivation du rôle');
        notification.showErrorNotification(
          response?.message || 'Erreur lors de la désactivation du rôle',
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la désactivation du rôle');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la désactivation du rôle',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Check if a role exists
   */
  const checkRoleExists = async (name: string) => {
    try {
      const response = await roleService.checkRoleExists(name);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors de la vérification');
      }
      return response?.data;
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  /**
   * Check if a role can be deleted
   */
  const checkCanDeleteRole = async (id: string) => {
    try {
      const response = await roleService.canDeleteRole(id);
      if (!response?.isSuccess) {
        setError(response?.message || 'Erreur lors de la vérification');
      }
      return response?.data;
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  /**
   * Reset the store
   */
  const resetStore = () => {
    roles.value = [];
    currentRole.value = null;
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
    roles,
    currentRole,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,
    hasPrevious,
    hasNext,

    // Getters
    activeRoles,
    inactiveRoles,
    isLoading,
    hasError,
    errorMessage,

    // Actions
    clearError,
    fetchAllRoles,
    fetchPaginatedRoles,
    fetchRoleById,
    fetchRoleByName,
    createRole,
    updateRole,
    deleteRole,
    activateRole,
    deactivateRole,
    checkRoleExists,
    checkCanDeleteRole,
    resetStore,
  };
});
