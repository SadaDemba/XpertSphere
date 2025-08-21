import axios from 'axios';
import type {
  AxiosInstance,
  AxiosError,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios';
import { settings } from '../settings';

interface JwtTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt?: number;
}

export class BaseClient {
  protected apiClient: AxiosInstance;
  private jwtTokens: JwtTokens | null = null;
  private refreshPromise: Promise<string> | null = null;
  private readonly JWT_TOKEN_KEY = settings.auth.jwt.tokenKey;
  private readonly REFRESH_TOKEN_KEY = settings.auth.jwt.refreshKey;

  protected constructor(url: string) {
    const baseURL = `${settings.webApi.baseUrl}${url}`;

    this.apiClient = axios.create({
      baseURL,
      responseType: 'json',
      timeout: 30000,
    });

    this.loadJwtTokensFromStorage();
    this.apiClient.interceptors.request.use(this.requestInterceptor.bind(this));
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

  protected async put<T>(service: string, data?: any, errorMessage?: string): Promise<T | null> {
    try {
      const response = await this.apiClient.put<T>(service, data, {});
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

  private async requestInterceptor(
    request: InternalAxiosRequestConfig,
  ): Promise<InternalAxiosRequestConfig> {
    const accessToken = await this.getValidJwtToken();

    if (accessToken && request.headers) {
      request.headers.Authorization = `Bearer ${accessToken}`;
    }

    // Add APIM subscription key for staging and production environments
    if (this.shouldIncludeApimKey() && request.headers) {
      request.headers[settings.apim.headerName] = settings.apim.subscriptionKey;
    }

    return Promise.resolve(request);
  }

  private async errorResponseInterceptor(error: AxiosError): Promise<AxiosError> {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const newToken = await this.refreshJwtToken();
        if (newToken && originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return this.apiClient(originalRequest);
        }
      } catch (refreshError) {
        this.handleAuthFailure();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }

  private async getValidJwtToken(): Promise<string | null> {
    if (!this.jwtTokens?.accessToken) {
      return null;
    }

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

  private async refreshJwtToken(): Promise<string> {
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
      const headers: Record<string, string> = {};

      // Add APIM subscription key for staging and production environments
      if (this.shouldIncludeApimKey()) {
        headers[settings.apim.headerName] = settings.apim.subscriptionKey;
      }

      const response = await axios.post(
        `${settings.webApi.baseUrl}/auth/refresh`,
        {
          refreshToken: this.jwtTokens.refreshToken,
        },
        { headers },
      );

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

  public setJwtTokens(tokens: JwtTokens): void {
    this.jwtTokens = tokens;
    localStorage.setItem(
      this.JWT_TOKEN_KEY,
      JSON.stringify({
        accessToken: tokens.accessToken,
        expiresAt: tokens.expiresAt,
      }),
    );
    if (tokens.refreshToken) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, tokens.refreshToken);
    }
  }

  private loadJwtTokensFromStorage(): void {
    const stored = localStorage.getItem(this.JWT_TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);

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

  public clearJwtTokens(): void {
    this.jwtTokens = null;
    localStorage.removeItem(this.JWT_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  private handleAuthFailure(): void {
    this.clearJwtTokens();
    window.dispatchEvent(new CustomEvent('auth:failure'));
  }

  public isAuthenticated(): boolean {
    this.loadJwtTokensFromStorage();
    return this.jwtTokens?.accessToken !== undefined;
  }

  public logout(): void {
    this.clearJwtTokens();
    window.dispatchEvent(new CustomEvent('auth:logout'));
  }

  private shouldIncludeApimKey(): boolean {
    return settings.apim.enabledEnvironments.includes(settings.environment);
  }
}
