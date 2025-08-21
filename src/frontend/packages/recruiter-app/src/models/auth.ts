/* eslint-disable @typescript-eslint/no-explicit-any */
import type { Address } from './address';

export interface LoginDto {
  email: string;
  password: string;
  rememberMe?: boolean;
  returnUrl?: string;
  forceLocalAuth?: boolean;
  preferredAuthType?: string;
  skipEntraIdRedirect?: boolean;
}

export interface AuthResponseDto {
  success: boolean;
  message?: string;
  statusCode?: number;
  errors?: string[];

  // Success response fields
  accessToken?: string;
  refreshToken?: string;
  tokenExpiry?: string;
  user?: User;
  emailConfirmationToken?: string;
  redirectUrl?: string;

  // Entra ID specific
  requiresEntraId?: boolean;
  entraIdAuthUrl?: string;
  authType?: 'B2B' | 'B2C' | 'Local';
  entraIdState?: string;
  isExternalAuth?: boolean;
  externalId?: string;
  entraIdClaims?: Record<string, any>;
}

export interface RefreshTokenDto {
  refreshToken: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles?: string[];
  isEmailConfirmed?: boolean;
  emailConfirmed?: boolean;
  organizationId?: string;
  organizationName?: string;
  profilePictureUrl?: string;
  createdAt: string;
  lastLoginAt?: string;

  // Additional fields from API response
  phoneNumber?: string;
  isActive?: boolean;
  updatedAt?: string;
  employeeId?: string;
  department?: string;
  hireDate?: string;
  linkedInProfile?: string;
  skills?: string;
  yearsOfExperience?: number;
  trainings?: any[];
  experiences?: any[];
  desiredSalary?: number;
  availability?: string;
  fullName?: string;
  profileCompletionPercentage?: number;
  address?: Address;

  // Entra ID specific
  externalId?: string;
  authProvider?: 'Local' | 'EntraId';
  entraIdClaims?: Record<string, any>;
}

export interface UserTypeResponseDto {
  success: boolean;
  message: string;
  userType: 'Recruiter' | 'Candidate' | 'Admin' | 'Unknown';
  authMethod: 'EntraId' | 'Local';
  organizationId?: string;
  organizationName?: string;
  entraIdAuthUrl?: string;
  errors: string[];
}

export interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export enum UserRole {
  PlatformSuperAdmin = 'XpertSphere.SuperAdmin',
  PlatformAdmin = 'XpertSphere.Admin',
  OrganizationAdmin = 'Organization.Admin',
  Manager = 'Organization.Manager',
  Recruiter = 'Organization.Recruiter',
  TechnicalEvaluator = 'Organization.TechnicalEvaluator',
  Candidate = 'Candidate',
}

export const PlatformRoles = [UserRole.PlatformSuperAdmin, UserRole.PlatformAdmin];

export const OrganizationRoles = [
  UserRole.OrganizationAdmin,
  UserRole.Manager,
  UserRole.Recruiter,
  UserRole.TechnicalEvaluator,
];

export const InternalRoles = [...PlatformRoles, ...OrganizationRoles];

export const ManagementRoles = [
  UserRole.PlatformSuperAdmin,
  UserRole.OrganizationAdmin,
  UserRole.Manager,
];

export const RecruitmentRoles = [UserRole.OrganizationAdmin, UserRole.Manager, UserRole.Recruiter];

export const EvaluationRoles = [
  UserRole.OrganizationAdmin,
  UserRole.Manager,
  UserRole.Recruiter,
  UserRole.TechnicalEvaluator,
];

export interface AdminResetPasswordDto {
  email: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ResetPasswordResponseDto {
  success: boolean;
  message: string;
  errors: string[];
}

export interface ResetPasswordDto {
  email: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordDto {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export enum AuthMethod {
  Local = 'Local',
  EntraId = 'EntraId',
}
