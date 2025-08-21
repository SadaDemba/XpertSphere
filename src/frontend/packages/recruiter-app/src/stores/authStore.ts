/* eslint-disable @typescript-eslint/no-explicit-any */
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService } from '../services/authService';
import type { User, UserRole } from '../models/auth';
import { InternalRoles, ManagementRoles, RecruitmentRoles, EvaluationRoles } from '../models/auth';
import { settings } from 'src/settings';

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref<User | null>(null);
  const token = ref<string | null>(null);
  const refreshToken = ref<string | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);
  const isInitialized = ref(false);

  // Getters
  const isAuthenticated = computed(() => !!user.value && !!token.value);

  const userRoles = computed(() => {
    if (!user.value) return [];
    return user.value.roles || [];
  });

  const isInternalUser = computed(() => {
    const roles = userRoles.value;
    return roles.some((role) => InternalRoles.includes(role as UserRole));
  });

  const isCandidate = computed(() => {
    const roles = userRoles.value;
    return roles.includes('Candidate');
  });

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

  /**
   * Login user with credentials
   */
  const login = async (credentials: { email: string; password: string }) => {
    try {
      setLoading(true);
      clearError();

      const response = await authService.login(credentials);

      if (response?.success && response.user) {
        user.value = response.user;
        token.value = response.accessToken || null;
        refreshToken.value = response.refreshToken || null;
        return true;
      } else {
        const errorMessage =
          response?.errors?.join(', ') || response?.message || 'La connexion a échoué';
        setError(errorMessage);
        return false;
      }
    } catch (err: any) {
      const errorMessage = err?.message || 'Une erreur réseau est survenu !';
      setError(errorMessage);
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Load current user from token
   */
  const loadCurrentUser = async () => {
    try {
      setLoading(true);
      clearError();

      // Sync token from localStorage if not already set
      if (!token.value && authService.isAuthenticated()) {
        const storedToken = localStorage.getItem(settings.auth.jwt.tokenKey);
        const storedRefreshToken = localStorage.getItem(settings.auth.jwt.refreshKey);
        if (storedToken) {
          const tokenData = JSON.parse(storedToken);
          token.value = tokenData.accessToken;
          refreshToken.value = storedRefreshToken;
        }
      }

      const userInfo = await authService.getCurrentUser();
      if (userInfo) {
        user.value = userInfo;
        return true;
      }
      return false;
    } catch (err) {
      console.error('Failed to load current user:', err);
      return false;
    } finally {
      setLoading(false);
    }
  };

  /**
   * Logout user
   */
  const logout = async () => {
    try {
      await authService.logoutUser();
    } catch (err) {
      console.warn('Logout API call failed:', err);
    } finally {
      // Clear state regardless of API call success
      user.value = null;
      token.value = null;
      refreshToken.value = null;
      error.value = null;
    }
  };

  /**
   * Check if user has specific role
   */
  const hasRole = (role: UserRole | string): boolean => {
    const roles = userRoles.value;
    return roles.includes(role);
  };

  /**
   * Check if user has any of the specified roles
   */
  const hasAnyRole = (roles: (UserRole | string)[]): boolean => {
    const userRolesList = userRoles.value;
    return roles.some((role) => userRolesList.includes(role));
  };

  /**
   * Check if user can manage users (Management roles only)
   */
  const canManageUsers = computed(() => {
    const roles = userRoles.value;
    return roles.some((role) => ManagementRoles.includes(role as UserRole));
  });

  /**
   * Check if user can manage job offers (Recruitment roles)
   */
  const canManageJobOffers = computed(() => {
    const roles = userRoles.value;
    return roles.some((role) => RecruitmentRoles.includes(role as UserRole));
  });

  /**
   * Check if user can manage candidates (Evaluation roles)
   */
  const canManageCandidates = computed(() => {
    const roles = userRoles.value;
    return roles.some((role) => EvaluationRoles.includes(role as UserRole));
  });

  /**
   * Initialize auth state (call on app startup)
   */
  const initialize = async () => {
    if (isInitialized.value) return;

    // Check if user is already authenticated (token in storage)
    if (authService.isAuthenticated()) {
      await loadCurrentUser();
    }
    isInitialized.value = true;
  };

  /**
   * Reset store to initial state
   */
  const reset = () => {
    user.value = null;
    token.value = null;
    refreshToken.value = null;
    isLoading.value = false;
    error.value = null;
  };

  return {
    // State
    user: user,
    token: token,
    refreshToken: refreshToken,
    isLoading: isLoading,
    error: error,

    // Getters
    isAuthenticated,
    userRoles,
    isInternalUser,
    isCandidate,
    hasError,
    canManageUsers,
    canManageJobOffers,
    canManageCandidates,

    // Actions
    clearError,
    setError,
    login,
    logout,
    loadCurrentUser,
    hasRole,
    hasAnyRole,
    initialize,
    reset,
  };
});
