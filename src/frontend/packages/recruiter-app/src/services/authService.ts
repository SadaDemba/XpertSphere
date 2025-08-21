import { BaseClient } from './BaseClient';
import type {
  LoginDto,
  AuthResponseDto,
  RefreshTokenDto,
  User,
  UserTypeResponseDto,
  AdminResetPasswordDto,
  ResetPasswordResponseDto,
  ResetPasswordDto,
  ChangePasswordDto,
} from '../models/auth';

// Compatibility aliases
export interface LoginCredentials {
  email: string;
  password: string;
}

export interface UserInfo extends User {
  name?: string; // Compatibility
  roles?: string[]; // Compatibility
}

class AuthService extends BaseClient {
  constructor() {
    super('/auth'); // Auth endpoints
  }

  /**
   * Login with email and password
   */
  public async login(credentials: LoginCredentials): Promise<AuthResponseDto | null> {
    const loginData: LoginDto = {
      email: credentials.email,
      password: credentials.password,
      rememberMe: false,
    };

    try {
      const response = await this.apiClient.post<AuthResponseDto>('/login', loginData);

      // Save tokens if login successful
      if (response.data?.success && response.data.accessToken) {
        this.setJwtTokens({
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken ?? '',
          expiresAt: Math.floor(new Date(response.data.tokenExpiry!).getTime() / 1000),
        });
      }
      return response.data;
    } catch (error: any) {
      // If it's an axios error with response data, extract the error info
      if (this.isAxiosError(error) && error.response?.data) {
        return error.response.data as AuthResponseDto;
      }

      // For other errors, return a generic error response
      return {
        success: false,
        message: error.message || 'Network error occurred',
        errors: [error.message || 'Network error occurred'],
      };
    }
  }

  /**
   * Get current user information
   */
  public async getCurrentUser(): Promise<User | null> {
    try {
      const response = await this.get<User>('/me');
      if (response) {
        // Add compatibility fields
        const userInfo: UserInfo = {
          ...response,
          name: `${response.firstName} ${response.lastName}`,
          roles: response.roles ?? [],
        };
        return userInfo;
      }
      return null;
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }
  }

  /**
   * Check if user has specific role
   */
  public async hasRole(role: string): Promise<boolean> {
    const user = await this.getCurrentUser();
    return user?.roles?.includes(role) || false;
  }

  /**
   * Check if user has any of the specified roles
   */
  public async hasAnyRole(roles: string[]): Promise<boolean> {
    const user = await this.getCurrentUser();
    return roles.some((role) => user?.roles?.includes(role)) || false;
  }

  /**
   * Refresh user session
   */
  public async refreshSession(): Promise<boolean> {
    try {
      if (this.getAuthMode() === 'jwt') {
        // For JWT, the BaseClient handles token refresh automatically
        return this.isAuthenticated();
      } else {
        // For Entra ID, we might need to refresh via MSAL
        // TODO: implement MSAL session refresh
        return true;
      }
    } catch (error) {
      console.error('Session refresh failed:', error);
      return false;
    }
  }

  /**
   * Reset own password with token received by email (JWT mode only)
   */
  public async resetPassword(
    resetData: ResetPasswordDto,
  ): Promise<ResetPasswordResponseDto | null> {
    if (this.getAuthMode() !== 'jwt') {
      throw new Error('Password reset only available in JWT mode');
    }

    return await this.post<ResetPasswordResponseDto>('/admin-reset-password', resetData);
  }

  /**
   * Change own password with token (JWT mode only)
   */
  public async changePassword(
    changeData: ChangePasswordDto,
  ): Promise<ResetPasswordResponseDto | null> {
    if (this.getAuthMode() !== 'jwt') {
      throw new Error('Password change only available in JWT mode');
    }

    return await this.post<ResetPasswordResponseDto>('/reset-password', changeData);
  }

  /**
   * Request password reset (JWT mode only)
   */
  public async requestPasswordReset(email: string): Promise<boolean> {
    if (this.getAuthMode() !== 'jwt') {
      throw new Error('Password reset only available in JWT mode');
    }

    try {
      await this.post('/forgot-password', { email });
      return true;
    } catch (error) {
      console.error('Password reset request failed:', error);
      return false;
    }
  }

  /**
   * Logout user
   */
  public async logoutUser(): Promise<void> {
    try {
      // Call logout endpoint if available
      await this.post('/logout');
    } catch (error) {
      // Continue even if API call fails
      console.warn('Logout API call failed:', error);
    } finally {
      // Clear tokens locally
      this.logout();
    }
  }

  /**
   * Refresh access token
   */
  public async refreshToken(refreshToken: string): Promise<AuthResponseDto | null> {
    const refreshData: RefreshTokenDto = { refreshToken };
    return await this.post<AuthResponseDto>('/refresh', refreshData);
  }

  /**
   * Get user type for authentication flow
   */
  public async getUserType(email: string): Promise<UserTypeResponseDto | null> {
    return await this.get<UserTypeResponseDto>(`/user-type?email=${encodeURIComponent(email)}`);
  }

  /**
   * Get login URL (for Entra ID)
   */
  public async getLoginUrl(email?: string, returnUrl?: string): Promise<any> {
    const params = new URLSearchParams();
    if (email) params.append('email', email);
    if (returnUrl) params.append('returnUrl', returnUrl);

    return await this.get<any>(`/login-url?${params.toString()}`);
  }

  /**
   * Admin reset password for a user by email (SuperAdmin XpertSphere for all, Admin for their organization)
   */
  public async adminResetPassword(
    resetData: AdminResetPasswordDto,
  ): Promise<ResetPasswordResponseDto | null> {
    return await this.post<ResetPasswordResponseDto>('/admin-reset-password', resetData);
  }

  /**
   * Check auth service health
   */
  public async checkHealth(): Promise<any> {
    return await this.get<any>('/health');
  }
}

// Export singleton instance
export const authService = new AuthService();
export default authService;
