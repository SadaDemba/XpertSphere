import { BaseClient } from './BaseClient';
import type {
  OrganizationDto,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationFilterDto,
  OrganizationCurrencyDto,
  UpdateOrganizationCurrencyDto,
} from '../models/organization';
import type { PaginatedResult, ResponseResult, VoidResponseResult } from '../models/base';

export class OrganizationService extends BaseClient {
  constructor() {
    super('/Organizations');
  }

  async getAllOrganizations(): Promise<ResponseResult<OrganizationDto[]> | null> {
    return this.get<ResponseResult<OrganizationDto[]>>('/all');
  }

  async getPaginatedOrganizations(
    filter: OrganizationFilterDto = {},
  ): Promise<PaginatedResult<OrganizationDto> | null> {
    return this.get<PaginatedResult<OrganizationDto>>('', { params: filter });
  }

  async getOrganizationById(id: string): Promise<ResponseResult<OrganizationDto> | null> {
    return this.get<ResponseResult<OrganizationDto>>(`/${id}`);
  }

  async createOrganization(
    organization: CreateOrganizationDto,
  ): Promise<ResponseResult<OrganizationDto> | null> {
    return this.post<ResponseResult<OrganizationDto>>('', organization);
  }

  async updateOrganization(
    id: string,
    organization: UpdateOrganizationDto,
  ): Promise<ResponseResult<OrganizationDto> | null> {
    return this.put<ResponseResult<OrganizationDto>>(`/${id}`, organization);
  }

  async deleteOrganization(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }

  async getMyOrganizationCurrency(): Promise<ResponseResult<OrganizationCurrencyDto> | null> {
    return this.get<ResponseResult<OrganizationCurrencyDto>>('/me/currency');
  }

  async updateMyOrganizationCurrency(
    dto: UpdateOrganizationCurrencyDto,
  ): Promise<ResponseResult<OrganizationCurrencyDto> | null> {
    return this.put<ResponseResult<OrganizationCurrencyDto>>('/me/currency', dto);
  }
}

export const organizationService = new OrganizationService();
export default organizationService;
