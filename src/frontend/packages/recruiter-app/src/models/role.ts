import type { Filter } from './base';

export interface Role {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  usersCount: number;
  permissionsCount: number;
}

export interface RoleDto {
  id: string;
  name: string;
  displayName: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  usersCount: number;
  permissionsCount: number;
}

export interface CreateRoleDto {
  name: string;
  displayName: string;
  description?: string;
}

export interface UpdateRoleDto {
  displayName: string;
  description?: string;
  isActive: boolean;
}

export interface RoleFilterDto extends Filter {
  isActive?: boolean;
  name?: string;
  displayName?: string;
  description?: string;
  userId?: string;
}

export interface PaginatedRoles {
  items: RoleDto[];
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
