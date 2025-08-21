import type { AddressDto } from './address';
import type { Filter } from './base';

export interface Training {
  id?: string;
  title: string;
  institution: string;
  startDate: string;
  endDate?: string;
  description?: string;
}

export interface Experience {
  id?: string;
  position: string;
  company: string;
  startDate: string;
  endDate?: string;
  description?: string;
  technologies?: string[];
}

export interface UserSearchResultDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  organizationId?: string;
  organizationName?: string;
  employeeId?: string;
  department?: string;
  skills?: string;
  experience?: number;
  desiredSalary?: number;
  availability?: string;
  profileCompletionPercentage: number;
  city?: string;
  country?: string;
  fullName: string;
  isAvailable: boolean;
  experienceDisplay: string;
}

export interface UserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  lastLoginAt?: string;

  // Organization info (for internal users)
  organizationId?: string;
  organizationName?: string;
  employeeId?: string;
  department?: string;
  hireDate?: string;

  // Candidate info (for external users)
  linkedInProfile?: string;
  skills?: string;
  yearsOfExperience?: number;
  trainings?: Training[];
  experiences?: Experience[];
  desiredSalary?: number;
  availability?: string;
  cvPath?: string;

  // Computed fields
  fullName: string;
  emailConfirmed: boolean;
  profileCompletionPercentage: number;

  // Address
  address?: AddressDto;

  // Roles
  roles?: string[];
}

export interface CreateUserDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;

  // For internal users
  organizationId?: string;
  employeeId?: string;
  department?: string;
  hireDate?: string;

  // For candidates
  linkedInProfile?: string;
  skills?: string;
  yearsOfExperience?: number;
  trainings?: Training[];
  experiences?: Experience[];
  desiredSalary?: number;
  availability?: string;
  cvPath?: string;

  // Communication preferences
  emailNotificationsEnabled?: boolean;
  smsNotificationsEnabled?: boolean;
  preferredLanguage?: string;
  timeZone?: string;

  // Consent
  consentGivenAt?: string;

  // Authentication & Security
  externalId?: string;
  password: string;
  emailConfirmed?: boolean;
  isActive?: boolean;

  // Address
  address?: AddressDto;
}

export interface UpdateUserDto {
  firstName?: string | undefined;
  lastName?: string | undefined;
  email?: string | undefined;
  phoneNumber?: string | undefined;
  organizationId?: string | undefined;
  employeeId?: string | undefined;
  department?: string | undefined;
  hireDate?: string | undefined;
  linkedInProfile?: string | undefined;
  skills?: string | undefined;
  yearsOfExperience?: number | undefined;
  trainings?: Training[] | undefined;
  experiences?: Experience[] | undefined;
  desiredSalary?: number | undefined;
  availability?: string | undefined;
  isActive?: boolean | undefined;
  address?: AddressDto | undefined;
}

export interface UserFilterDto extends Filter {
  organizationId?: string;
  department?: string;
  isActive?: boolean | undefined;
  minExperience?: number;
  maxExperience?: number;
  minSalary?: number;
  maxSalary?: number;
  skills?: string;
  role?: string;
}

export interface PaginatedUsers {
  items: UserSearchResultDto[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
  };
  isSuccess: boolean;
  message: string;
  errors: string[];
}
