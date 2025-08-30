/* eslint-disable @typescript-eslint/no-explicit-any */
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
import { ResponseResult, VoidResponseResult } from 'src/models';

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
  public async login(
    credentials: LoginCredentials,
  ): Promise<ResponseResult<AuthResponseDto> | null> {
    const loginData: LoginDto = {
      email: credentials.email,
      password: credentials.password,
      rememberMe: false,
    };

    return this.post<ResponseResult<AuthResponseDto>>('/login', loginData);
  }

  /**
   * Get current user information
   */
  public async getCurrentUser(): Promise<ResponseResult<User> | null> {
    return this.get<ResponseResult<User>>('/me');
    /*try {

      if (response) {
        // Add compatibility fields
        const userInfo: UserInfo = {
          ...response.data!,
          name: response.data?.fullName ?? "",
          roles: response.data!.roles ?? [],
        };
        return userInfo;
      }
      return null;
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }*/
  }

  /**
   * Check if user has specific role
   */
  public async hasRole(role: string): Promise<boolean> {
    const user = await this.getCurrentUser();
    return user?.data?.roles!.includes(role) || false;
  }

  /**
   * Check if user has any of the specified roles
   */
  public async hasAnyRole(roles: string[]): Promise<boolean> {
    const user = await this.getCurrentUser();
    return roles.some((role) => user?.data?.roles!.includes(role)) || false;
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
  ): Promise<ResponseResult<ResetPasswordResponseDto> | null> {
    return this.post<ResponseResult<ResetPasswordResponseDto>>('/admin-reset-password', resetData);
  }

  /**
   * Change own password with token (JWT mode only)
   */
  public async changePassword(
    changeData: ChangePasswordDto,
  ): Promise<ResponseResult<ResetPasswordResponseDto> | null> {
    return this.post<ResponseResult<ResetPasswordResponseDto>>('/reset-password', changeData);
  }

  /**
   * Request password reset (JWT mode only)
   */
  public async requestPasswordReset(email: string): Promise<ResponseResult<boolean> | null> {
    return this.post('/forgot-password', { email });
  }

  /**
   * Logout user
   */
  public async logoutUser(): Promise<VoidResponseResult | null> {
    return this.post<VoidResponseResult>('/logout');
  }

  /**
   * Refresh access token
   */
  public async refreshToken(refreshToken: string): Promise<ResponseResult<AuthResponseDto> | null> {
    const refreshData: RefreshTokenDto = { refreshToken };
    return this.post<ResponseResult<AuthResponseDto>>('/refresh', refreshData);
  }

  /**
   * Get user type for authentication flow
   */
  public async getUserType(email: string): Promise<ResponseResult<UserTypeResponseDto> | null> {
    return this.get<ResponseResult<UserTypeResponseDto>>(
      `/user-type?email=${encodeURIComponent(email)}`,
    );
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
   * Admin reset password for a user by email
   */
  public async adminResetPassword(
    resetData: AdminResetPasswordDto,
  ): Promise<ResponseResult<ResetPasswordResponseDto> | null> {
    return this.post<ResponseResult<ResetPasswordResponseDto>>('/admin-reset-password', resetData);
  }

  /**
   * Check auth service health
   */
  public async checkHealth(): Promise<any> {
    return this.get<any>('/health');
  }
}

// Export singleton instance
export const authService = new AuthService();
export default authService;
