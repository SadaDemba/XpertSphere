import { BaseClient } from './BaseClient';
import type { RoleDto, CreateRoleDto, UpdateRoleDto, RoleFilterDto } from '../models/role';
import type { PaginatedResult } from '../models/base';

export class RoleService extends BaseClient {
  constructor() {
    super('/Roles');
  }

  async getAllRoles(): Promise<RoleDto[]> {
    const response = await this.get<RoleDto[]>('');
    return response || [];
  }

  async getPaginatedRoles(filter: RoleFilterDto = {}): Promise<PaginatedResult<RoleDto>> {
    const response = await this.get<PaginatedResult<RoleDto>>('/paginated', { params: filter });
    return (
      response || {
        items: [],
        pageNumber: 1,
        pageSize: 10,
        totalItems: 0,
        totalPages: 0,
        hasPrevious: false,
        hasNext: false,
      }
    );
  }

  async getRoleById(id: string): Promise<RoleDto | null> {
    const response = await this.get<RoleDto>(`/${id}`);
    return response;
  }

  async getRoleByName(name: string): Promise<RoleDto | null> {
    const response = await this.get<RoleDto>(`/by-name/${encodeURIComponent(name)}`);
    return response;
  }

  async createRole(role: CreateRoleDto): Promise<RoleDto | null> {
    const response = await this.post<RoleDto>('', role);
    return response;
  }

  async updateRole(id: string, role: UpdateRoleDto): Promise<RoleDto | null> {
    const response = await this.put<RoleDto>(`/${id}`, role);
    return response;
  }

  async deleteRole(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }

  async activateRole(id: string): Promise<boolean> {
    const response = await this.patch(`/${id}/activate`);
    return response !== null;
  }

  async deactivateRole(id: string): Promise<boolean> {
    const response = await this.patch(`/${id}/deactivate`);
    return response !== null;
  }

  async checkRoleExists(name: string): Promise<boolean> {
    const response = await this.get<boolean>(`/exists/${encodeURIComponent(name)}`);
    return response || false;
  }

  async canDeleteRole(id: string): Promise<boolean> {
    const response = await this.get<boolean>(`/${id}/can-delete`);
    return response || false;
  }
}

export const roleService = new RoleService();
export default roleService;
