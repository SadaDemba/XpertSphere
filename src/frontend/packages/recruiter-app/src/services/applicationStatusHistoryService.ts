import { BaseClient } from './BaseClient';
import type {
  ApplicationStatusHistoryDto,
  CreateApplicationStatusHistoryDto,
  UpdateApplicationStatusHistoryDto,
} from '../models/application';

export class ApplicationStatusHistoryService extends BaseClient {
  constructor() {
    super('/ApplicationStatusHistory');
  }

  async getByApplicationId(applicationId: string): Promise<ApplicationStatusHistoryDto[]> {
    const response = await this.get<ApplicationStatusHistoryDto[]>(`/application/${applicationId}`);
    return response || [];
  }

  async getById(id: string): Promise<ApplicationStatusHistoryDto | null> {
    const response = await this.get<ApplicationStatusHistoryDto>(`/${id}`);
    return response;
  }

  async createAppComment(
    dto: CreateApplicationStatusHistoryDto,
  ): Promise<ApplicationStatusHistoryDto | null> {
    const response = await this.post<ApplicationStatusHistoryDto>('', dto);
    return response;
  }

  async updateAppComment(
    id: string,
    dto: UpdateApplicationStatusHistoryDto,
  ): Promise<ApplicationStatusHistoryDto | null> {
    const response = await this.put<ApplicationStatusHistoryDto>(`/${id}`, dto);
    return response;
  }

  async deleteAppComment(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }
}

export const applicationStatusHistoryService = new ApplicationStatusHistoryService();
export default applicationStatusHistoryService;
