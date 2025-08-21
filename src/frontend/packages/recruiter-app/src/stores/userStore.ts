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

export const useUserStore = defineStore('user', () => {
  const service = new UserService();

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

      const data = await service.getPaginatedUsers(filter);

      users.value = data.items;
      totalCount.value = data.totalItems;
      currentPage.value = data.pageNumber;
      pageSize.value = data.pageSize;
      totalPages.value = data.totalPages;
    } catch (err) {
      setError(`Erreur lors du chargement des utilisateurs: ${err}`);
    } finally {
      setLoading(false);
    }
  };

  const fetchUserById = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getUserById(id);
      currentUser.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement de l'utilisateur: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const fetchUserProfile = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const data = await service.getUserProfile(id);
      currentUser.value = data;
      return data;
    } catch (err) {
      setError(`Erreur lors du chargement du profil utilisateur: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const createUser = async (userData: CreateUserDto) => {
    try {
      setLoading(true);
      clearError();
      const newUser = await service.createUser(userData);
      if (newUser) {
        // Note: We'll need to convert UserDto to UserSearchResultDto for the list
        totalCount.value++;
      }
      return newUser;
    } catch (err) {
      setError(`Erreur lors de la création de l'utilisateur: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const updateUser = async (id: string, userData: UpdateUserDto) => {
    try {
      setLoading(true);
      clearError();
      const updatedUser = await service.updateUser(id, userData);

      if (updatedUser) {
        // Update in the list if present
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1 && users.value[index]) {
          // Update the search result with the updated user data
          Object.assign(users.value[index], {
            firstName: updatedUser.firstName,
            lastName: updatedUser.lastName,
            email: updatedUser.email,
            phoneNumber: updatedUser.phoneNumber,
            isActive: updatedUser.isActive,
            department: updatedUser.department,
            organizationName: updatedUser.organizationName,
            fullName: updatedUser.fullName,
          });
        }

        if (currentUser.value?.id === id) {
          currentUser.value = updatedUser;
        }
      }

      return updatedUser;
    } catch (err) {
      setError(`Erreur lors de la mise à jour de l'utilisateur: ${err}`);
      return null;
    } finally {
      setLoading(false);
    }
  };

  const deleteUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deleteUser(id);

      if (success) {
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1) {
          users.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentUser.value?.id === id) {
          currentUser.value = null;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression de l'utilisateur: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const hardDeleteUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.hardDeleteUser(id);

      if (success) {
        const index = users.value.findIndex((user) => user.id === id);
        if (index !== -1) {
          users.value.splice(index, 1);
          totalCount.value--;
        }

        if (currentUser.value?.id === id) {
          currentUser.value = null;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la suppression définitive de l'utilisateur: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const activateUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.activateUser(id);

      if (success) {
        // Update user status in the list
        const user = users.value.find((u) => u.id === id);
        if (user) {
          user.isActive = true;
        }

        // Update current user if it's the same
        if (currentUser.value?.id === id) {
          currentUser.value.isActive = true;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de l'activation de l'utilisateur: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const deactivateUser = async (id: string) => {
    try {
      setLoading(true);
      clearError();
      const success = await service.deactivateUser(id);

      if (success) {
        // Update user status in the list
        const user = users.value.find((u) => u.id === id);
        if (user) {
          user.isActive = false;
        }

        // Update current user if it's the same
        if (currentUser.value?.id === id) {
          currentUser.value.isActive = false;
        }
      }

      return success;
    } catch (err) {
      setError(`Erreur lors de la désactivation de l'utilisateur: ${err}`);
      return false;
    } finally {
      setLoading(false);
    }
  };

  const checkEmailAvailable = async (email: string, excludeUserId?: string): Promise<boolean> => {
    try {
      return await service.checkEmailAvailable(email, excludeUserId);
    } catch (err) {
      setError(`Erreur lors de la vérification de l'email: ${err}`);
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
