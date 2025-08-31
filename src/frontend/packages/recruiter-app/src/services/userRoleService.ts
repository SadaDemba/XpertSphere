import { BaseClient } from './BaseClient';
import type { UserRoleDto, AssignRoleDto } from '../models/userRole';
import type { ResponseResult, VoidResponseResult } from '../models/base';

export class UserRoleService extends BaseClient {
  constructor() {
    super('/UserRoles');
  }

  async getUserRoles(userId: string): Promise<ResponseResult<UserRoleDto[]> | null> {
    return this.get<ResponseResult<UserRoleDto[]>>(`/user/${userId}`);
  }

  async getRoleUsers(roleId: string): Promise<ResponseResult<UserRoleDto[]> | null> {
    return this.get<ResponseResult<UserRoleDto[]>>(`/role/${roleId}`);
  }

  async assignRoleToUser(
    assignRoleDto: AssignRoleDto,
  ): Promise<ResponseResult<UserRoleDto> | null> {
    return this.post<ResponseResult<UserRoleDto>>('/assign', assignRoleDto);
  }

  async removeRoleFromUser(userRoleId: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${userRoleId}`);
  }

  async updateUserRoleStatus(
    userRoleId: string,
    isActive: boolean,
  ): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${userRoleId}/status`, isActive);
  }

  async extendUserRole(
    userRoleId: string,
    newExpiryDate?: string,
  ): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${userRoleId}/extend`, newExpiryDate);
  }

  async checkUserHasRole(
    userId: string,
    roleName: string,
  ): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(
      `/user/${userId}/has-role/${encodeURIComponent(roleName)}`,
    );
  }

  async checkUserHasActiveRole(
    userId: string,
    roleName: string,
  ): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(
      `/user/${userId}/has-active-role/${encodeURIComponent(roleName)}`,
    );
  }

  async getUserRoleNames(userId: string): Promise<ResponseResult<string[]> | null> {
    return this.get<ResponseResult<string[]>>(`/user/${userId}/role-names`);
  }
}

export const userRoleService = new UserRoleService();
export default userRoleService;
