import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type {
  UserDto,
  UserSearchResultDto,
  CreateUserDto,
  UpdateUserDto,
  UserFilterDto,
} from '../models/user';
import { UserService } from '../services/userService';
import { useNotification } from 'src/composables/notification';
import { convertUserDtoToUserSearchResultDto } from 'src/helpers/mapper';

export const useUserStore = defineStore('user', () => {
  const service = new UserService();
  const notification = useNotification();

  const users = ref<UserSearchResultDto[]>([]);
  const currentUser = ref<UserDto | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const totalCount = ref(0);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const totalPages = ref(0);

  const activeUsers = computed(() => users.value.filter((user) => user.isActive));

  const inactiveUsers = computed(() => users.value.filter((user) => !user.isActive));

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

  const fetchPaginatedUsers = async (filter: UserFilterDto = {}) => {
    try {
      setLoading(true);
      clearError();

      const response = await service.getPaginatedUsers(filter);
      if (response?.isSuccess) {
        users.value = response!.data;
        totalCount.value = response!.pagination.totalItems;
        currentPage.value = response!.pagination.currentPage;
        pageSize.value = response!.pagination.pageSize;
        totalPages.value = response!.pagination.totalPages;
        notification.showSuccessNotification('Utilisateurs chargés avce succès');
      } else {
        setError(response?.message || 'Erreur lors de la chargement des utilisateurs');
        notification.showSuccessNotification(
          response?.message || 'Erreur lors du chargement des utilisateurs',
        );
      }
    } catch (error) {
      setError(
        error instanceof Error ? error.message : 'Erreur lors du chargement des utilisateurs',
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors du chargement des utilisateurs',
      );
    } finally {
      setLoading(false);
    }
  };

  const fetchUserById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.getUserById(id);
      if (response?.isSuccess && response.data) {
        currentUser.value = response!.data;
        notification.showSuccessNotification('Utilisateurs chargés avce succès');
      } else {
        setError(response?.message || "Erreur lors de la chargement de l'utilisateur");
        notification.showSuccessNotification(
          response?.message || "Erreur lors du chargement de l'utilisateur",
        );
      }

      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'utilisateur",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  const fetchUserProfile = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.getUserProfile(id);
      if (response?.isSuccess && response.data) {
        currentUser.value = response!.data;
        notification.showSuccessNotification('Utilisateurs chargés avce succès');
      } else {
        setError(response?.message || "Erreur lors de la chargement de l'utilisateur");
        notification.showSuccessNotification(
          response?.message || "Erreur lors du chargement de l'utilisateur",
        );
      }
      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors du chargement de l'utilisateur",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  const createUser = async (userData: CreateUserDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.createUser(userData);
      if (response?.isSuccess && response.data) {
        users.value.unshift(convertUserDtoToUserSearchResultDto(response.data));
        totalCount.value++;
        notification.showSuccessNotification('Utilisateurs créé avec succès');
      } else {
        setError(response?.message || "Erreur lors de la création de l'utilisateur");
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la création de l'utilisateur",
        );
      }
      return response?.data;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la création de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la création de l'utilisateur",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateUser = async (id: string, userData: UpdateUserDto) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.updateUser(id, userData);

      if (response?.isSuccess && response?.data) {
        // Update in the list if present
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1 && users.value[index]) {
          // Update the search result with the updated user data
          Object.assign(users.value[index], {
            firstName: response.data.firstName,
            lastName: response.data.lastName,
            email: response.data.email,
            phoneNumber: response.data.phoneNumber,
            isActive: response.data.isActive,
            department: response.data.department,
            organizationName: response.data.organizationName,
            fullName: response.data.fullName,
          });
        }

        if (currentUser.value?.id === id) {
          currentUser.value = response!.data;
        }
        notification.showSuccessNotification('Succès de la mise à jour');
      } else {
        setError(response?.message || "Erreur lors de la mise à jour de l'utilisateur");
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la mise à jour de l'utilisateur",
        );
      }

      return response;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la mise à jour de l'utilisateur",
      );
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.deleteUser(id);

      if (response?.isSuccess) {
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1) {
          users.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentUser.value?.id === id) {
          currentUser.value = null;
        }
        notification.showSuccessNotification("Succès de l'opération");
      } else {
        setError(response?.message || 'Erreur lors de la suppression');
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la suppression de l'utilisateur",
        );
      }
      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la suppression de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la suppression de l'utilisateur",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const hardDeleteUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.hardDeleteUser(id);

      if (response?.isSuccess) {
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1) {
          users.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentUser.value?.id === id) {
          currentUser.value = null;
        }
      } else {
        setError(response?.message || 'Erreur lors de la suppression définitive');
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la suppression définitive de l'utilisateur",
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la suppression définitive');
      notification.showErrorNotification(
        error instanceof Error ? error.message : 'Erreur lors de la suppression définitive',
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const activateUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.activateUser(id);

      if (response?.isSuccess) {
        // Update user status in the list
        const user = users.value.find((u) => u.id === id);
        if (user) {
          user.isActive = true;
        }

        // Update current user if it's the same
        if (currentUser.value?.id === id) {
          currentUser.value.isActive = true;
        }

        notification.showSuccessNotification("Succès de l'opération");
      } else {
        setError(response?.message || "Erreur lors de l'activation");
        notification.showSuccessNotification(
          response?.message || "Erreur lors de l'activation de l'utilisateur",
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de l'activation de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de l'activation de l'utilisateur",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const deactivateUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const response = await service.deactivateUser(id);

      if (response?.isSuccess) {
        // Update user status in the list
        const user = users.value.find((u) => u.id === id);
        if (user) {
          user.isActive = false;
        }

        // Update current user if it's the same
        if (currentUser.value?.id === id) {
          currentUser.value.isActive = false;
        }
        notification.showSuccessNotification("Succès de l'opération");
      } else {
        setError(response?.message || 'Erreur lors de la désactivation');
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la désactivation de l'utilisateur",
        );
      }

      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la désactivation de l'utilisateur",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la désactivation de l'utilisateur",
      );
      return false;
    } finally {
      setLoading(false);
    }
  };

  const checkEmailAvailable = async (email: string, excludeUserId?: string): Promise<boolean> => {
    try {
      const response = await service.checkEmailAvailable(email, excludeUserId);
      if (!response?.isSuccess) {
        notification.showSuccessNotification(
          response?.message || "Erreur lors de la vérification de l'email",
        );
        return false;
      }
      return response?.isSuccess;
    } catch (error) {
      setError(
        error instanceof Error ? error.message : "Erreur lors de la vérification de l'email",
      );
      notification.showErrorNotification(
        error instanceof Error ? error.message : "Erreur lors de la vérification de l'email",
      );
      return false;
    }
  };

  const resetStore = () => {
    users.value = [];
    currentUser.value = null;
    loading.value = false;
    error.value = null;
    totalCount.value = 0;
    currentPage.value = 1;
    pageSize.value = 10;
    totalPages.value = 0;
  };

  return {
    users,
    currentUser,
    loading,
    error,
    totalCount,
    currentPage,
    pageSize,
    totalPages,

    activeUsers,
    inactiveUsers,
    isLoading,
    hasError,
    errorMessage,

    clearError,
    fetchPaginatedUsers,
    fetchUserById,
    fetchUserProfile,
    createUser,
    updateUser,
    deleteUser,
    hardDeleteUser,
    activateUser,
    deactivateUser,
    checkEmailAvailable,
    resetStore,
  };
});
