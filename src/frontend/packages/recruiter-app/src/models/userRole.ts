export interface UserRole {
  id: string;
  userId: string;
  roleId: string;
  roleName: string;
  isActive: boolean;
  assignedAt: string;
  expiresAt?: string;
  assignedBy?: string;
  assignedByName?: string;
}

export interface UserRoleDto {
  id: string;
  userId: string;
  roleId: string;
  userFullName: string;
  userEmail: string;
  roleName: string;
  roleDisplayName: string;
  assignedAt: string;
  assignedBy?: string;
  assignedByName?: string;
  isActive: boolean;
  expiresAt?: string;
}

export interface AssignRoleDto {
  userId: string;
  roleId: string;
  expiresAt?: string;
}
