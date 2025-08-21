import { BaseClient } from './BaseClient';
import type { UserRoleDto, AssignRoleDto } from '../models/userRole';

export class UserRoleService extends BaseClient {
  constructor() {
    super('/UserRoles');
  }

  async getUserRoles(userId: string): Promise<UserRoleDto[]> {
    const response = await this.get<UserRoleDto[]>(`/user/${userId}`);
    return response || [];
  }

  async getRoleUsers(roleId: string): Promise<UserRoleDto[]> {
    const response = await this.get<UserRoleDto[]>(`/role/${roleId}`);
    return response || [];
  }

  async assignRoleToUser(assignRoleDto: AssignRoleDto): Promise<UserRoleDto | null> {
    const response = await this.post<UserRoleDto>('/assign', assignRoleDto);
    return response;
  }

  async removeRoleFromUser(userRoleId: string): Promise<boolean> {
    const response = await this.delete(`/${userRoleId}`);
    return response !== null;
  }

  async updateUserRoleStatus(userRoleId: string, isActive: boolean): Promise<boolean> {
    const response = await this.patch(`/${userRoleId}/status`, isActive);
    return response !== null;
  }

  async extendUserRole(userRoleId: string, newExpiryDate?: string): Promise<boolean> {
    const response = await this.patch(`/${userRoleId}/extend`, newExpiryDate);
    return response !== null;
  }

  async checkUserHasRole(userId: string, roleName: string): Promise<boolean> {
    const response = await this.get<boolean>(
      `/user/${userId}/has-role/${encodeURIComponent(roleName)}`,
    );
    return response || false;
  }

  async checkUserHasActiveRole(userId: string, roleName: string): Promise<boolean> {
    const response = await this.get<boolean>(
      `/user/${userId}/has-active-role/${encodeURIComponent(roleName)}`,
    );
    return response || false;
  }

  async getUserRoleNames(userId: string): Promise<string[]> {
    const response = await this.get<string[]>(`/user/${userId}/role-names`);
    return response || [];
  }
}

export const userRoleService = new UserRoleService();
export default userRoleService;
