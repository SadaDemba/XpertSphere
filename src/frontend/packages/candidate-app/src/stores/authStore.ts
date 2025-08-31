import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService } from '../services/authService';
import type {
  User,
  LoginDto,
  RegisterCandidateDto,
  ResumeAnalysisResponse,
  AuthResult,
} from '../models/auth';
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
  const hasError = computed(() => error.value !== null);
  const userFullName = computed(() =>
    user.value ? `${user.value.firstName} ${user.value.lastName}` : '',
  );
  const userInitials = computed(() =>
    user.value
      ? `${user.value.firstName.toUpperCase().charAt(0)}${user.value.lastName.toUpperCase().charAt(0)}`
      : '',
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

  const setUser = (newUser: User) => {
    user.value = newUser;
  };

  const setAuth = (authResponse: AuthResult) => {
    if (authResponse.isSuccess && authResponse.data!.user && authResponse.data!.accessToken) {
      user.value = authResponse.data!.user;
      token.value = authResponse.data!.accessToken;
      refreshToken.value = authResponse.data!.refreshToken || null;
      // Set tokens in service
      if (authResponse.data!.accessToken) {
        authService.setJwtTokens({
          accessToken: authResponse.data!.accessToken,
          refreshToken: authResponse.data!.refreshToken || '',
          expiresAt: Math.floor(new Date(authResponse.data!.tokenExpiry!).getTime() / 1000),
        });
      }
    }
  };

  const login = async (loginDto: LoginDto): Promise<boolean> => {
    try {
      setLoading(true);
      clearError();

      const response = await authService.login(loginDto);
      if (response?.isSuccess) {
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

      if (response?.isSuccess) {
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
        user.value = userInfo.data!;
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
    userInitials,

    // Actions
    clearError,
    setLoading,
    setError,
    setUser,
    login,
    registerCandidate,
    analyzeResume,
    logout,
    initialize,
  };
});
