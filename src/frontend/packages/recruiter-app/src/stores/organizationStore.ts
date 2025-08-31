import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  OrganizationDto,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationFilterDto,
} from '../models/organization';
import { organizationService } from '../services/organizationService';
import { useNotification } from 'src/composables/notification';

export const useOrganizationStore = defineStore('organization', () => {
  const notification = useNotification();

  // State
  const organizations = ref<OrganizationDto[]>([]);
  const currentOrganization = ref<OrganizationDto | null>(null);
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
  const activeOrganizations = computed(() => organizations.value.filter((org) => org.isActive));

  const inactiveOrganizations = computed(() => organizations.value.filter((org) => !org.isActive));

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
   * Retrieve all organizations
   */
  const fetchAllOrganizations = async () => {
    try {
      setLoading(true);
      clearError();
      const response = await organizationService.getAllOrganizations();
      if (response?.isSuccess) {
        organizations.value = response.data!;
      } else {
        setError('Erreur lors du chargement des organisations');
        notification.showErrorNotification('Erreur lors du chargement des organisations');
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement des organisations',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des organisations',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Retrieve all organizations with pagination and filter
   */
  const fetchPaginatedOrganizations = async (filter: OrganizationFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const paginationFilter: OrganizationFilterDto = {
        ...filter,
        pageNumber: filter.pageNumber || currentPage.value,
        pageSize: filter.pageSize || pageSize.value,
      };

      const response = await organizationService.getPaginatedOrganizations(paginationFilter);

      if (response?.isSuccess) {
        organizations.value = response.data;
        totalCount.value = response.pagination.totalItems;
        currentPage.value = response.pagination.currentPage;
        pageSize.value = response.pagination.pageSize;
        totalPages.value = response.pagination.totalPages;
        hasPrevious.value = response.pagination.hasPrevious;
        hasNext.value = response.pagination.hasNext;

        notification.showSuccessNotification('Organisations chargées avec succès');
      } else {
        setError('Erreur lors du chargement des organisations');
        notification.showErrorNotification('Erreur lors du chargement des organisations');
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement des organisations',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des organisations',
      );
    } finally {
      setLoading(false);
    }
  };

  /**
   * Get organization by ID
   */
  const fetchOrganizationById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await organizationService.getOrganizationById(id);
      if (response?.isSuccess) {
        currentOrganization.value = response.data!;
      } else {
        setError(response?.message || "Erreur lors du chargement de l'organisation");
        notification.showErrorNotification(
          response?.message || "Erreur lors du chargement de l'organisation",
        );
      }
      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'organisation",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'organisation",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Create a new organization
   */
  const createOrganization = async (organizationData: CreateOrganizationDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await organizationService.createOrganization(organizationData);
      if (response?.isSuccess) {
        organizations.value.unshift(response.data!);
        totalCount.value++;
        notification.showSuccessNotification('Organisation créée avec succès');
      } else {
        setError(response?.message || "Erreur lors de la création de l'organisation");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la création de l'organisation",
        );
      }
      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la création de l'organisation",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la création de l'organisation",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Update an organization
   */
  const updateOrganization = async (id: string, organizationData: UpdateOrganizationDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await organizationService.updateOrganization(id, organizationData);

      if (response?.isSuccess) {
        const index = organizations.value.findIndex((org) => org.id === id);
        if (index !== -1) {
          organizations.value[index] = response.data!;
        }

        if (currentOrganization.value?.id === id) {
          currentOrganization.value = response.data!;
        }
        notification.showSuccessNotification('Organisation mise à jour avec succès');
      } else {
        setError(response?.message || "Erreur lors de la mise à jour de l'organisation");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la mise à jour de l'organisation",
        );
      }

      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'organisation",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'organisation",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Delete an organization
   */
  const deleteOrganization = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await organizationService.deleteOrganization(id);

      if (response?.isSuccess) {
        const index = organizations.value.findIndex((org) => org.id === id);
        if (index !== -1) {
          organizations.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentOrganization.value?.id === id) {
          currentOrganization.value = null;
        }
        notification.showSuccessNotification('Organisation supprimée avec succès');
      } else {
        setError(response?.message || "Erreur lors de la suppression de l'organisation");
        notification.showErrorNotification(
          response?.message || "Erreur lors de la suppression de l'organisation",
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la suppression de l'organisation",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la suppression de l'organisation",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Reset the store
   */
  const resetStore = () => {
    organizations.value = [];
    currentOrganization.value = null;
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
    organizations,
    currentOrganization,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,
    hasPrevious,
    hasNext,

    // Getters
    activeOrganizations,
    inactiveOrganizations,
    isLoading,
    hasError,
    errorMessage,

    // Actions
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
