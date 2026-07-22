import type { Address } from './address';
import type { OrganizationSize, Currency } from '../enums';
import type { Filter } from './base';

export interface Organization {
  id: string;
  name: string;
  code: string;
  description?: string;
  industry?: string;
  size: OrganizationSize;
  address: Address;
  contactEmail?: string;
  contactPhone?: string;
  website?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  usersCount: number;
}

export interface OrganizationDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  industry?: string;
  size: OrganizationSize;
  address: Address;
  contactEmail?: string;
  contactPhone?: string;
  website?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  usersCount: number;
}

export interface CreateOrganizationDto {
  name: string;
  code: string;
  description?: string;
  industry?: string;
  size: OrganizationSize;
  address: Address;
  contactEmail?: string;
  contactPhone?: string;
  website?: string;
}

export interface UpdateOrganizationDto {
  name?: string;
  code?: string;
  description?: string;
  industry?: string;
  size?: OrganizationSize;
  address?: Address;
  contactEmail?: string;
  contactPhone?: string;
  website?: string;
  isActive?: boolean;
}

export interface OrganizationFilterDto extends Filter {
  isActive?: boolean;
  organizationSize?: OrganizationSize;
}

// Self-service currency settings (GET/PUT /api/organizations/me/currency), reserved to
// Organization.Admin - deliberately not part of Organization/OrganizationDto/CreateOrganizationDto/
// UpdateOrganizationDto above, which the backend does not expose this field through either (see
// configurable-salary-currency.md, §Hors périmètre).
export interface OrganizationCurrencyDto {
  currency: Currency | null;
}

export interface UpdateOrganizationCurrencyDto {
  currency: Currency;
}

export type { OrganizationSize };

export interface PaginatedOrganizations {
  items: OrganizationDto[];
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
