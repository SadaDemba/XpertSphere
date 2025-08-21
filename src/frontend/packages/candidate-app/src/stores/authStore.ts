import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService } from '../services/authService';
import type {
  User,
  LoginDto,
  RegisterCandidateDto,
  AuthResponseDto,
  ResumeAnalysisResponse,
} from '../models/auth';

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
  const hasError = computed(() => error.value !== null);
  const userFullName = computed(() =>
    user.value ? `${user.value.firstName} ${user.value.lastName}` : '',
  );

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

  const setAuth = (authResponse: AuthResponseDto) => {
    if (authResponse.success && authResponse.user && authResponse.accessToken) {
      user.value = authResponse.user;
      token.value = authResponse.accessToken;
      refreshToken.value = authResponse.refreshToken || null;

      // Set tokens in service
      if (authResponse.accessToken && authResponse.refreshToken) {
        authService.setJwtTokens({
          accessToken: authResponse.accessToken,
          refreshToken: authResponse.refreshToken ?? '',
          expiresAt: Math.floor(new Date(authResponse.tokenExpiry!).getTime() / 1000),
        });
      }
    }
  };

  const login = async (loginDto: LoginDto): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await authService.login(loginDto);
      if (response?.success) {
        setAuth(response);
        return true;
      } else {
        setError(response?.message || 'Erreur lors de la connexion');
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Erreur lors de la connexion');
      return false;
    } finally {
      setLoading(false);
    }
  };

  const registerCandidate = async (
    registerDto: RegisterCandidateDto,
    resume?: File,
  ): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await authService.registerCandidate(registerDto, resume);
      if (response?.success) {
        setAuth(response);
        return true;
      } else {
        setError(response?.message || "Erreur lors de l'inscription");
        return false;
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de l'inscription");
      return false;
    } finally {
      setLoading(false);
    }
  };

  const analyzeResume = async (resume: File): Promise<ResumeAnalysisResponse | null> => {
    try {
      setLoading(true);
      clearError();

      return await authService.analyzeResume(resume);
    } catch (error) {
      setError(error instanceof Error ? error.message : "Erreur lors de l'analyse du CV");
      return null;
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    user.value = null;
    token.value = null;
    refreshToken.value = null;
    error.value = null;
    authService.logout();
  };

  const initialize = async () => {
    if (isInitialized.value) return;

    try {
      setLoading(true);

      if (authService.isAuthenticated()) {
        const currentUser = await authService.getCurrentUser();
        if (currentUser) {
          user.value = currentUser;
        } else {
          // If we can't get user info, clear auth
          logout();
        }
      }
    } catch (error) {
      console.error('Failed to initialize auth:', error);
      logout();
    } finally {
      setLoading(false);
      isInitialized.value = true;
    }
  };

  return {
    // State
    user: user,
    token: token,
    refreshToken: refreshToken,
    isLoading: isLoading,
    error: error,
    isInitialized: isInitialized,

    // Getters
    isAuthenticated,
    hasError,
    userFullName,

    // Actions
    clearError,
    setLoading,
    setError,
    login,
    registerCandidate,
    analyzeResume,
    logout,
    initialize,
  };
});
