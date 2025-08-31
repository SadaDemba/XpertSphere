/* eslint-disable @typescript-eslint/no-explicit-any */
import { settings } from '../settings';
import axios from 'axios';
import { scopes, msalInstance } from './MsalService';
import type {
  AxiosInstance,
  AxiosError,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios';

// Hybrid authentication configuration
export interface AuthConfig {
  mode: 'jwt' | 'entraid';
  jwtTokenKey?: string;
  refreshTokenKey?: string;
  apim: {
    subscriptionKey: string;
    headerName: string;
    enabledEnvironments: string[];
  };
}

interface JwtTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

// Determine auth mode based on settings
const getAuthMode = (): AuthConfig => {
  return {
    mode: settings.auth.mode,
    jwtTokenKey: settings.auth.jwt.tokenKey,
    refreshTokenKey: settings.auth.jwt.refreshKey,
    apim: settings.apim,
  };
};

export class BaseClient {
  protected apiClient: AxiosInstance;
  private authConfig: AuthConfig;
  private jwtTokens: JwtTokens | null = null;
  private refreshPromise: Promise<string> | null = null;

  protected constructor(url: string) {
    this.authConfig = getAuthMode();

    // Use configured base URL for all auth modes
    const baseURL = `${settings.webApi.baseUrl}/api${url}`;

    this.apiClient = axios.create({
      baseURL,
      responseType: 'json',
      timeout: 30000,
    });

    // Load JWT tokens if in dev mode
    if (this.authConfig.mode === 'jwt') {
      this.loadJwtTokensFromStorage();
    }

    this.apiClient.interceptors.request.use(this.requestInterceptor.bind(this));
    this.apiClient.interceptors.response.use(undefined, this.blobErrorResponseInterceptor);
    this.apiClient.interceptors.response.use(undefined, this.errorResponseInterceptor.bind(this));
  }

  protected async get<T>(
    service: string,
    config?: AxiosRequestConfig,
    errorMessage?: string,
  ): Promise<T | null> {
    try {
      const response = await this.apiClient.get<T>(service, config ?? {});

      if (response.status >= 200 && response.status < 300) {
        return response.data;
      } else {
        throw new Error(errorMessage);
      }
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async post<T>(service: string, data?: any, errorMessage?: string): Promise<T | null> {
    try {
      const response = await this.apiClient.post<T>(service, data, {});

      if (response.status >= 200 && response.status < 300) {
        return response.data;
      } else {
        throw new Error(errorMessage);
      }
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async patch<T>(service: string, data?: any, errorMessage?: string): Promise<T | null> {
    try {
      const response = await this.apiClient.patch<T>(service, data, {});

      if (response.status >= 200 && response.status < 300) {
        return response.data;
      } else {
        throw new Error(errorMessage);
      }
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async put<T>(
    service: string,
    data?: any,
    type?: string,
    errorMessage?: string,
  ): Promise<T | null> {
    try {
      const response = await this.apiClient.put<T>(service, data, {
        headers: {
          'Content-Type': type ?? 'application/json',
        },
      });

      if (response.status >= 200 && response.status < 300) {
        return response.data;
      } else {
        throw new Error(errorMessage);
      }
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async delete<T>(service: string, errorMessage?: string): Promise<T | null> {
    try {
      const response = await this.apiClient.delete<T>(service);

      if (response.status >= 200 && response.status < 300) {
        return response.data;
      } else {
        throw new Error(errorMessage);
      }
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async downloadPhoto(service: string, errorMessage?: string): Promise<AxiosResponse> {
    try {
      return await this.apiClient.get<string>(service, {
        responseType: 'arraybuffer',
        timeout: 60_000,
      });
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected async downloadFile(
    service: string,
    config?: AxiosRequestConfig,
    errorMessage?: string,
  ): Promise<Blob> {
    try {
      const response = await this.apiClient.get(service, {
        responseType: 'blob',
        timeout: 60_000,
        ...config,
      });

      return response.data;
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : errorMessage);
    }
  }

  protected isAxiosError(error: Error): error is AxiosError {
    return (error as AxiosError).isAxiosError;
  }

  private async requestInterceptor(
    request: InternalAxiosRequestConfig,
  ): Promise<InternalAxiosRequestConfig> {
    let accessToken: string | null = null;

    if (this.authConfig.mode === 'jwt') {
      // JWT mode - get token from local storage
      accessToken = await this.getValidJwtToken();
    } else {
      // Entra ID mode - use MSAL
      accessToken = await msalInstance.getAccessToken(scopes);
    }

    // Add Authorization token
    if (accessToken != null && request.headers) {
      request.headers.Authorization = `Bearer ${accessToken}`;
    }

    // Add APIM subscription key for staging and production environments
    if (this.shouldIncludeApimKey() && request.headers) {
      request.headers[this.authConfig.apim.headerName] = this.authConfig.apim.subscriptionKey;
    }

    return Promise.resolve(request);
  }

  private blobErrorResponseInterceptor(error: AxiosError): Promise<AxiosError> {
    const contentType: string = error.response?.headers['content-type']?.toLowerCase() ?? '';
    if (error.request.responseType === 'blob' && contentType.includes('json')) {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();

        reader.onload = (): void => {
          if (error.response) {
            error.response.data = JSON.parse(reader.result as string);
            resolve(Promise.reject(error));
          }
        };

        reader.onerror = (): void => reject(error);

        reader.readAsText(error.response?.data as Blob);
      });
    }

    return Promise.reject(error);
  }

  private async errorResponseInterceptor(error: AxiosError): Promise<AxiosError> {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // If the user is unauthenticated, retry login/refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        let newToken: string | null = null;

        if (this.authConfig.mode === 'jwt') {
          // Try to refresh JWT token
          newToken = await this.refreshJwtToken();
        } else {
          // Entra ID mode - use MSAL to refresh
          newToken = await msalInstance.getAccessToken(scopes);
        }

        if (newToken && originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return this.apiClient(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed - logout user
        this.handleAuthFailure();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }

  // === JWT Methods ===

  /**
   * Get a valid JWT token (refresh if necessary)
   */
  private async getValidJwtToken(): Promise<string | null> {
    if (!this.jwtTokens?.accessToken) {
      return null;
    }

    // Check if token expires soon (within 5 minutes)
    const now = Date.now() / 1000;
    const expiresAt = this.jwtTokens.expiresAt || 0;
    const shouldRefresh = expiresAt - now < 300; // 5 minutes

    if (shouldRefresh && this.jwtTokens.refreshToken) {
      try {
        return await this.refreshJwtToken();
      } catch (error) {
        console.warn('JWT token refresh failed:', error);
        return this.jwtTokens.accessToken;
      }
    }

    return this.jwtTokens.accessToken;
  }

  /**
   * Refresh JWT token
   */
  private async refreshJwtToken(): Promise<string> {
    // Avoid multiple simultaneous calls
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    this.refreshPromise = this.performJwtRefresh();

    try {
      const newToken = await this.refreshPromise;
      this.refreshPromise = null;
      return newToken;
    } catch (error) {
      this.refreshPromise = null;
      throw error;
    }
  }

  private async performJwtRefresh(): Promise<string> {
    if (!this.jwtTokens?.refreshToken) {
      throw new Error('No refresh token available');
    }

    try {
      // Direct call to avoid interceptor recursion
      const response = await axios.post(`${settings.webApi.baseUrl}/auth/refresh`, {
        refreshToken: this.jwtTokens.refreshToken,
      });

      const { accessToken, refreshToken, expiresIn } = response.data;

      this.setJwtTokens({
        accessToken,
        refreshToken: refreshToken || this.jwtTokens.refreshToken,
        expiresAt: expiresIn ? Date.now() / 1000 + expiresIn : undefined,
      });

      return accessToken;
    } catch (error) {
      throw new Error(
        error instanceof Error ? error.message : 'Le renouvellement de la connexion a échoué',
      );
    }
  }

  /**
   * Set JWT tokens
   */
  public setJwtTokens(tokens: JwtTokens): void {
    this.jwtTokens = tokens;
    if (this.authConfig.jwtTokenKey) {
      localStorage.setItem(
        this.authConfig.jwtTokenKey,
        JSON.stringify({
          accessToken: tokens.accessToken,
          expiresAt: tokens.expiresAt,
        }),
      );
    }
    if (this.authConfig.refreshTokenKey && tokens.refreshToken) {
      localStorage.setItem(this.authConfig.refreshTokenKey, tokens.refreshToken);
    }
  }

  /**
   * Load JWT tokens from localStorage
   */
  private loadJwtTokensFromStorage(): void {
    if (!this.authConfig.jwtTokenKey) return;

    const stored = localStorage.getItem(this.authConfig.jwtTokenKey);
    const refreshToken = this.authConfig.refreshTokenKey
      ? localStorage.getItem(this.authConfig.refreshTokenKey)
      : null;

    if (stored) {
      try {
        const tokenData = JSON.parse(stored);
        this.jwtTokens = {
          ...tokenData,
          refreshToken: refreshToken,
        };
      } catch (error) {
        console.warn('Failed to parse stored JWT tokens:', error);
        this.clearJwtTokens();
      }
    }
  }

  /**
   * Clear JWT tokens
   */
  public clearJwtTokens(): void {
    this.jwtTokens = null;
    if (this.authConfig.jwtTokenKey) {
      localStorage.removeItem(this.authConfig.jwtTokenKey);
    }
    if (this.authConfig.refreshTokenKey) {
      localStorage.removeItem(this.authConfig.refreshTokenKey);
    }
  }

  /**
   * Handle authentication failure
   */
  private handleAuthFailure(): void {
    if (this.authConfig.mode === 'jwt') {
      this.clearJwtTokens();
    }
    // Emit event to notify app
    window.dispatchEvent(new CustomEvent('auth:failure'));
  }

  // === Public Methods ===

  /**
   * Check if user is authenticated
   */
  public isAuthenticated(): boolean {
    this.loadJwtTokensFromStorage();
    if (this.authConfig.mode === 'jwt') {
      return this.jwtTokens?.accessToken != undefined;
    }
    // For Entra ID, we could check via MSAL
    return true; // TODO: implement MSAL auth check
  }

  /**
   * Get current auth mode
   */
  public getAuthMode(): 'jwt' | 'entraid' {
    return this.authConfig.mode;
  }

  /**
   * For development - login with JWT
   */
  public async loginWithJwt(email: string, password: string): Promise<boolean> {
    if (this.authConfig.mode !== 'jwt') {
      throw new Error('JWT login only available in development mode');
    }

    try {
      const headers: Record<string, string> = {};

      // Add APIM subscription key for staging and production environments
      if (this.shouldIncludeApimKey()) {
        headers[this.authConfig.apim.headerName] = this.authConfig.apim.subscriptionKey;
      }

      const response = await axios.post(
        `${settings.webApi.baseUrl}/auth/login`,
        {
          email,
          password,
        },
        { headers },
      );

      const { accessToken, refreshToken, expiresIn } = response.data;

      this.setJwtTokens({
        accessToken,
        refreshToken,
        expiresAt: expiresIn ? Date.now() / 1000 + expiresIn : undefined,
      });

      return true;
    } catch (error) {
      console.error('JWT login failed:', error);
      return false;
    }
  }

  /**
   * Logout user
   */
  public logout(): void {
    if (this.authConfig.mode === 'jwt') {
      this.clearJwtTokens();
    } else {
      // TODO: implement MSAL logout
    }
    window.dispatchEvent(new CustomEvent('auth:logout'));
  }

  private shouldIncludeApimKey(): boolean {
    return this.authConfig.apim.enabledEnvironments.includes(settings.environment);
  }
}
