/* eslint-disable @typescript-eslint/no-explicit-any */
import { BaseClient } from './BaseClient';
import type {
  UserDto,
  UserSearchResultDto,
  CreateUserDto,
  UpdateUserDto,
  UserFilterDto,
} from '../models/user';
import type { PaginatedResult, ResponseResult, VoidResponseResult } from '../models/base';

export class UserService extends BaseClient {
  constructor() {
    super('/Users');
  }

  async getPaginatedUsers(
    filter: UserFilterDto = {},
  ): Promise<PaginatedResult<UserSearchResultDto> | null> {
    return this.get<PaginatedResult<UserSearchResultDto>>('', { params: filter });
  }

  async getUserById(id: string): Promise<ResponseResult<UserDto> | null> {
    const response = await this.get<ResponseResult<UserDto>>(`/${id}`);
    return response;
  }

  async getUserProfile(id: string): Promise<ResponseResult<UserDto> | null> {
    const response = await this.get<ResponseResult<UserDto>>(`/${id}/profile`);
    return response;
  }

  async createUser(user: CreateUserDto): Promise<ResponseResult<UserDto> | null> {
    return this.post<ResponseResult<UserDto>>('', user);
  }

  async updateUser(id: string, user: UpdateUserDto): Promise<ResponseResult<UserDto> | null> {
    return this.put<ResponseResult<UserDto>>(`/${id}`, user);
  }

  async deleteUser(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }

  async hardDeleteUser(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}/permanent`);
  }

  async activateUser(id: string): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${id}/activate`);
  }

  async deactivateUser(id: string): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${id}/deactivate`);
  }

  async updateLastLogin(id: string): Promise<VoidResponseResult | null> {
    return this.patch<VoidResponseResult>(`/${id}/last-login`);
  }

  async updateProfileCompletion(id: string): Promise<ResponseResult<number> | null> {
    return this.patch<ResponseResult<number>>(`/${id}/profile-completion`);
  }

  async checkUserExists(id: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/${id}`);
  }

  async checkEmailAvailable(
    email: string,
    excludeUserId?: string,
  ): Promise<ResponseResult<boolean> | null> {
    const params: any = { email };
    if (excludeUserId) {
      params.excludeUserId = excludeUserId;
    }
    return this.get<ResponseResult<boolean>>('/email-available', { params });
  }

  async getUsersByOrganization(
    organizationId: string,
  ): Promise<ResponseResult<UserSearchResultDto[]> | null> {
    return this.get<ResponseResult<UserSearchResultDto[]>>(`/organization/${organizationId}`);
  }

  async getUsersWithIncompleteProfiles(
    threshold: number = 80,
  ): Promise<ResponseResult<UserSearchResultDto[]> | null> {
    return this.get<ResponseResult<UserSearchResultDto[]>>('/incomplete-profiles', {
      params: { threshold },
    });
  }

  async getRecentlyRegisteredUsers(
    days: number = 7,
  ): Promise<ResponseResult<UserSearchResultDto[]> | null> {
    return await this.get<ResponseResult<UserSearchResultDto[]>>('/recently-registered', {
      params: { days },
    });
  }

  async getInactiveUsers(days: number = 30): Promise<ResponseResult<UserSearchResultDto[]> | null> {
    return this.get<ResponseResult<UserSearchResultDto[]>>('/inactive', {
      params: { days },
    });
  }
}

export const userService = new UserService();
export default userService;
