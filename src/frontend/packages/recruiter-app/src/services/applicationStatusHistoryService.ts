import { BaseClient } from './BaseClient';
import type {
  ApplicationStatusHistoryDto,
  CreateApplicationStatusHistoryDto,
  UpdateApplicationStatusHistoryDto,
} from '../models/application';
import type { ResponseResult, VoidResponseResult } from '../models/base';

export class ApplicationStatusHistoryService extends BaseClient {
  constructor() {
    super('/ApplicationStatusHistory');
  }

  async getByApplicationId(
    applicationId: string,
  ): Promise<ResponseResult<ApplicationStatusHistoryDto[]> | null> {
    return this.get<ResponseResult<ApplicationStatusHistoryDto[]>>(`/application/${applicationId}`);
  }

  async getById(id: string): Promise<ResponseResult<ApplicationStatusHistoryDto> | null> {
    return this.get<ResponseResult<ApplicationStatusHistoryDto>>(`/${id}`);
  }

  async createAppComment(
    dto: CreateApplicationStatusHistoryDto,
  ): Promise<ResponseResult<ApplicationStatusHistoryDto> | null> {
    return this.post<ResponseResult<ApplicationStatusHistoryDto>>('', dto);
  }

  async updateAppComment(
    id: string,
    dto: UpdateApplicationStatusHistoryDto,
  ): Promise<ResponseResult<ApplicationStatusHistoryDto> | null> {
    return this.put<ResponseResult<ApplicationStatusHistoryDto>>(`/${id}`, dto);
  }

  async deleteAppComment(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }
}

export const applicationStatusHistoryService = new ApplicationStatusHistoryService();
export default applicationStatusHistoryService;
