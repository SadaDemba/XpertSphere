import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  OrganizationDto,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationFilterDto,
} from '../models/organization';
import { OrganizationService } from '../services/organizationService';

export const useOrganizationStore = defineStore('organization', () => {
  const service = new OrganizationService();

  const organizations = ref<OrganizationDto[]>([]);
  const currentOrganization = ref<OrganizationDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const totalPages = ref(0);

  const activeOrganizations = computed(() => organizations.value.filter((org) => org.isActive));

  const inactiveOrganizations = computed(() => organizations.value.filter((org) => !org.isActive));

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

  const fetchPaginatedOrganizations = async (filter: OrganizationFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const data = await service.getPaginatedOrganizations(filter);

      organizations.value = data.items;
      totalCount.value = data.totalItems;
      currentPage.value = data.pageNumber;
      pageSize.value = data.pageSize;
      totalPages.value = data.totalPages;
    } catch (err) {
      setError(`Erreur lors du chargement des organisations: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchOrganizationById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getOrganizationById(id);
      currentOrganization.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de l'organisation: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const createOrganization = async (organizationData: CreateOrganizationDto) => {
    try {
      setLoading(true);
      clearError();
      const newOrganization = await service.createOrganization(organizationData);
      if (newOrganization) {
        organizations.value.unshift(newOrganization);
        totalCount.value++;
      }
      return newOrganization;
    } catch (err) {
      setError(`Erreur lors de la création de l'organisation: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateOrganization = async (id: string, organizationData: UpdateOrganizationDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedOrganization = await service.updateOrganization(id, organizationData);

      if (updatedOrganization) {
        const index = organizations.value.findIndex((org) => org.id === id);
        if (index !== -1) {
          organizations.value[index] = updatedOrganization;
        }

        if (currentOrganization.value?.id === id) {
          currentOrganization.value = updatedOrganization;
        }
      }

      return updatedOrganization;
    } catch (err) {
      setError(`Erreur lors de la mise à jour de l'organisation: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteOrganization = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deleteOrganization(id);

      if (success) {
        const index = organizations.value.findIndex((org) => org.id === id);
        if (index !== -1) {
          organizations.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentOrganization.value?.id === id) {
          currentOrganization.value = null;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression de l'organisation: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const fetchAllOrganizations = async () => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getAllOrganizations();
      organizations.value = data;
    } catch (err) {
      setError(`Erreur lors du chargement des organisations: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const resetStore = () => {
    organizations.value = [];
    currentOrganization.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 10;
    totalPages.value = 0;
  };

  return {
    organizations,
    currentOrganization,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,

    activeOrganizations,
    inactiveOrganizations,
    isLoading,
    hasError,
    errorMessage,

    clearError,
    fetchAllOrganizations,
    fetchPaginatedOrganizations,
    fetchOrganizationById,
    createOrganization,
    updateOrganization,
    deleteOrganization,
    resetStore,
  };
});
