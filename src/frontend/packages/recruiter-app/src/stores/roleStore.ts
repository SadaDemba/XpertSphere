import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { RoleDto, CreateRoleDto, UpdateRoleDto, RoleFilterDto } from '../models/role';
import { RoleService } from '../services/roleService';

export const useRoleStore = defineStore('role', () => {
  const service = new RoleService();

  const roles = ref<RoleDto[]>([]);
  const currentRole = ref<RoleDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const totalPages = ref(0);

  const activeRoles = computed(() => roles.value.filter((role) => role.isActive));

  const inactiveRoles = computed(() => roles.value.filter((role) => !role.isActive));

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

  const fetchAllRoles = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getAllRoles();
      roles.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des rôles: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchPaginatedRoles = async (filter: RoleFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const data = await service.getPaginatedRoles(filter);

      roles.value = data.items;
      totalCount.value = data.totalItems;
      currentPage.value = data.pageNumber;
      pageSize.value = data.pageSize;
      totalPages.value = data.totalPages;
    } catch (err) {
      setError(`Erreur lors du chargement des rôles: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchRoleById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getRoleById(id);
      currentRole.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement du rôle: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const fetchRoleByName = async (name: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getRoleByName(name);
      currentRole.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement du rôle: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const createRole = async (roleData: CreateRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const newRole = await service.createRole(roleData);
      if (newRole) {
        roles.value.unshift(newRole);
        totalCount.value++;
      }
      return newRole;
    } catch (err) {
      setError(`Erreur lors de la création du rôle: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateRole = async (id: string, roleData: UpdateRoleDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedRole = await service.updateRole(id, roleData);

      if (updatedRole) {
        const index = roles.value.findIndex((role) => role.id === id);
        if (index !== -1) {
          roles.value[index] = updatedRole;
        }

        if (currentRole.value?.id === id) {
          currentRole.value = updatedRole;
        }
      }

      return updatedRole;
    } catch (err) {
      setError(`Erreur lors de la mise à jour du rôle: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deleteRole(id);

      if (success) {
        const index = roles.value.findIndex((role) => role.id === id);
        if (index !== -1) {
          roles.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentRole.value?.id === id) {
          currentRole.value = null;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression du rôle: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const activateRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.activateRole(id);

      if (success) {
        const role = roles.value.find((r) => r.id === id);
        if (role) {
          role.isActive = true;
        }

        if (currentRole.value?.id === id) {
          currentRole.value.isActive = true;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de l'activation du rôle: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const deactivateRole = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deactivateRole(id);

      if (success) {
        const role = roles.value.find((r) => r.id === id);
        if (role) {
          role.isActive = false;
        }

        if (currentRole.value?.id === id) {
          currentRole.value.isActive = false;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la désactivation du rôle: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const checkRoleExists = async (name: string) => {
    try {
      return await service.checkRoleExists(name);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const checkCanDeleteRole = async (id: string) => {
    try {
      return await service.canDeleteRole(id);
    } catch (err) {
      setError(`Erreur lors de la vérification: ${err}`);
      return false;
    }
  };

  const resetStore = () => {
    roles.value = [];
    currentRole.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 10;
    totalPages.value = 0;
  };

  return {
    roles,
    currentRole,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,

    activeRoles,
    inactiveRoles,
    isLoading,
    hasError,
    errorMessage,

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
