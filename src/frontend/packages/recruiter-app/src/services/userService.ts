import { BaseClient } from './BaseClient';
import type {
  UserDto,
  UserSearchResultDto,
  CreateUserDto,
  UpdateUserDto,
  UserFilterDto,
} from '../models/user';
import type { PaginatedResult } from '../models/base';

export class UserService extends BaseClient {
  constructor() {
    super('/Users');
  }

  async getPaginatedUsers(
    filter: UserFilterDto = {},
  ): Promise<PaginatedResult<UserSearchResultDto>> {
    const response = await this.get<any>('', { params: filter });

    if (!response) {
      return {
        items: [],
        pageNumber: 1,
        pageSize: 10,
        totalItems: 0,
        totalPages: 0,
        hasPrevious: false,
        hasNext: false,
      };
    }

    // Map the API response structure to our expected format
    return {
      items: response.items || [],
      pageNumber: response.pagination?.currentPage || 1,
      pageSize: response.pagination?.pageSize || 10,
      totalItems: response.pagination?.totalItems || 0,
      totalPages: response.pagination?.totalPages || 0,
      hasPrevious: response.pagination?.hasPrevious || false,
      hasNext: response.pagination?.hasNext || false,
    };
  }

  async getUserById(id: string): Promise<UserDto | null> {
    const response = await this.get<UserDto>(`/${id}`);
    return response;
  }

  async getUserProfile(id: string): Promise<UserDto | null> {
    const response = await this.get<UserDto>(`/${id}/profile`);
    return response;
  }

  async createUser(user: CreateUserDto): Promise<UserDto | null> {
    const response = await this.post<UserDto>('', user);
    return response;
  }

  async updateUser(id: string, user: UpdateUserDto): Promise<UserDto | null> {
    const response = await this.put<UserDto>(`/${id}`, user);
    return response;
  }

  async deleteUser(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }

  async hardDeleteUser(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}/permanent`);
    return response !== null;
  }

  async activateUser(id: string): Promise<boolean> {
    const response = await this.patch(`/${id}/activate`);
    return response !== null;
  }

  async deactivateUser(id: string): Promise<boolean> {
    const response = await this.patch(`/${id}/deactivate`);
    return response !== null;
  }

  async updateLastLogin(id: string): Promise<boolean> {
    const response = await this.patch(`/${id}/last-login`);
    return response !== null;
  }

  async updateProfileCompletion(id: string): Promise<number | null> {
    const response = await this.patch<number>(`/${id}/profile-completion`);
    return response;
  }

  async checkUserExists(id: string): Promise<boolean> {
    try {
      const response = await this.get<UserDto>(`/${id}`);
      return response !== null;
    } catch {
      return false;
    }
  }

  async checkEmailAvailable(email: string, excludeUserId?: string): Promise<boolean> {
    const params: any = { email };
    if (excludeUserId) {
      params.excludeUserId = excludeUserId;
    }
    const response = await this.get<boolean>('/email-available', { params });
    return response || false;
  }

  async getUsersByOrganization(organizationId: string): Promise<UserSearchResultDto[]> {
    const response = await this.get<UserSearchResultDto[]>(`/organization/${organizationId}`);
    return response || [];
  }

  async getUsersWithIncompleteProfiles(threshold: number = 80): Promise<UserSearchResultDto[]> {
    const response = await this.get<UserSearchResultDto[]>('/incomplete-profiles', {
      params: { threshold },
    });
    return response || [];
  }

  async getRecentlyRegisteredUsers(days: number = 7): Promise<UserSearchResultDto[]> {
    const response = await this.get<UserSearchResultDto[]>('/recently-registered', {
      params: { days },
    });
    return response || [];
  }

  async getInactiveUsers(days: number = 30): Promise<UserSearchResultDto[]> {
    const response = await this.get<UserSearchResultDto[]>('/inactive', {
      params: { days },
    });
    return response || [];
  }
}

export const userService = new UserService();
export default userService;
