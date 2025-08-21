/* eslint-disable @typescript-eslint/no-explicit-any */
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

  // Additional candidate fields
  phoneNumber?: string;
  isActive?: boolean;
  updatedAt?: string;
  linkedInProfile?: string;
  skills?: string;
  yearsOfExperience?: number;
  trainings?: any[];
  experiences?: any[];
  desiredSalary?: number;
  availability?: string;
  fullName?: string;
  profileCompletionPercentage?: number;

  // Entra ID specific
  externalId?: string;
  authProvider?: 'Local' | 'EntraId';
  entraIdClaims?: Record<string, any>;
}

export interface Training {
  school: string;
  level: string;
  period: string;
  field: string;
}

export interface Experience {
  title: string;
  company: string;
  location?: string;
  date: string;
  description: string;
}

export interface RegisterCandidateDto {
  // Account Info
  email: string;
  password: string;
  confirmPassword: string;

  // Personal Info
  firstName: string;
  lastName: string;
  phoneNumber?: string;

  // Address
  streetNumber?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;

  // Professional Info
  skills?: string;
  yearsOfExperience?: number;
  desiredSalary?: number;
  availability?: string;
  linkedInProfile?: string;

  // Training and Experience
  trainings?: Training[];
  experiences?: Experience[];

  // Communication Preferences
  emailNotificationsEnabled?: boolean;
  smsNotificationsEnabled?: boolean;
  preferredLanguage?: string;
  timeZone?: string;

  // Legal
  acceptTerms: boolean;
  acceptPrivacyPolicy: boolean;
  consentGivenAt?: Date | string;

  // Navigation
  returnUrl?: string;

  // EntraID (not used for candidates)
  forceLocalRegistration?: boolean;
  organizationDomain?: string;
  entraIdToken?: string;
  externalId?: string;
  linkToEntraId?: boolean;
  entraIdMetadata?: Record<string, string>;
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

export interface ResumeAnalysisResponse {
  extracted_data: {
    first_name?: string;
    last_name?: string;
    email?: string;
    phone_number?: string;
    profession?: string;
    address?: string;
    languages?: string[];
    trainings?: Array<{
      school: string;
      level: string;
      period: string;
      field: string;
    }>;
    skills?: string[];
    experiences?: Array<{
      title: string;
      description: string;
      date: string;
      company: string;
      location?: string;
    }>;
    id?: string;
  };
}
