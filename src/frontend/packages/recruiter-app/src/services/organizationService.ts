import { BaseClient } from './BaseClient';
import type {
  OrganizationDto,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationFilterDto,
} from '../models/organization';
import type { PaginatedResult } from '../models/base';

export class OrganizationService extends BaseClient {
  constructor() {
    super('/Organizations');
  }

  async getAllOrganizations(): Promise<OrganizationDto[]> {
    const response = await this.get<OrganizationDto[]>('/all');
    return response || [];
  }

  async getPaginatedOrganizations(
    filter: OrganizationFilterDto = {},
  ): Promise<PaginatedResult<OrganizationDto>> {
    const response = await this.get<PaginatedResult<OrganizationDto>>('', { params: filter });
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

  async getOrganizationById(id: string): Promise<OrganizationDto | null> {
    const response = await this.get<OrganizationDto>(`/${id}`);
    return response;
  }

  async createOrganization(organization: CreateOrganizationDto): Promise<OrganizationDto | null> {
    const response = await this.post<OrganizationDto>('', organization);
    return response;
  }

  async updateOrganization(
    id: string,
    organization: UpdateOrganizationDto,
  ): Promise<OrganizationDto | null> {
    const response = await this.put<OrganizationDto>(`/${id}`, organization);
    return response;
  }

  async deleteOrganization(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }
}

export const organizationService = new OrganizationService();
export default organizationService;
