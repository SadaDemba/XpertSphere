import { BaseClient } from './BaseClient';
import type { RoleDto, CreateRoleDto, UpdateRoleDto, RoleFilterDto } from '../models/role';
import type { PaginatedResult, ResponseResult, VoidResponseResult } from '../models/base';

export class RoleService extends BaseClient {
  constructor() {
    super('/Roles');
  }

  async getAllRoles(): Promise<ResponseResult<RoleDto[]> | null> {
    return this.get<ResponseResult<RoleDto[]>>('');
  }

  async getPaginatedRoles(filter: RoleFilterDto = {}): Promise<PaginatedResult<RoleDto> | null> {
    return this.get<PaginatedResult<RoleDto>>('/paginated', { params: filter });
  }

  async getRoleById(id: string): Promise<ResponseResult<RoleDto> | null> {
    return this.get<ResponseResult<RoleDto>>(`/${id}`);
  }

  async getRoleByName(name: string): Promise<ResponseResult<RoleDto> | null> {
    return this.get<ResponseResult<RoleDto>>(`/by-name/${encodeURIComponent(name)}`);
  }

  async createRole(role: CreateRoleDto): Promise<ResponseResult<RoleDto> | null> {
    return this.post<ResponseResult<RoleDto>>('', role);
  }

  async updateRole(id: string, role: UpdateRoleDto): Promise<ResponseResult<RoleDto> | null> {
    return this.put<ResponseResult<RoleDto>>(`/${id}`, role);
  }

  async deleteRole(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }

  async activateRole(id: string): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${id}/activate`);
  }

  async deactivateRole(id: string): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${id}/deactivate`);
  }

  async checkRoleExists(name: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/exists/${encodeURIComponent(name)}`);
  }

  async canDeleteRole(id: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/${id}/can-delete`);
  }
}

export const roleService = new RoleService();
export default roleService;
