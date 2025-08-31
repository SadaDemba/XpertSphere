import { BaseClient } from './BaseClient';
import type {
  ApplicationDto,
  ApplicationFilterDto,
  PaginatedApplications,
  CreateApplicationDto,
  UpdateApplicationDto,
  ApplicationStatusHistoryDto,
  UpdateApplicationStatusDto,
  AssignUserDto,
} from '../models/application';
import type { ResponseResult, VoidResponseResult } from '../models/base';

export class ApplicationService extends BaseClient {
  constructor() {
    super('/Applications');
  }

  async getAllApplications(): Promise<ResponseResult<ApplicationDto[]> | null> {
    return this.get<ResponseResult<ApplicationDto[]>>('');
  }

  async getPaginatedApplications(
    filter: ApplicationFilterDto = { organizationId: '' },
  ): Promise<PaginatedApplications | null> {
    return this.get<PaginatedApplications>('/paginated', { params: filter });
  }

  async getApplicationById(id: string): Promise<ResponseResult<ApplicationDto> | null> {
    return this.get<ResponseResult<ApplicationDto>>(`/${id}`);
  }

  async createApplication(
    application: CreateApplicationDto,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    return this.post<ResponseResult<ApplicationDto>>('', application);
  }

  async updateApplication(
    id: string,
    application: UpdateApplicationDto,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    return this.put<ResponseResult<ApplicationDto>>(`/${id}`, application);
  }

  async deleteApplication(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }

  async updateApplicationStatus(
    id: string,
    statusUpdate: UpdateApplicationStatusDto,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    return this.put<ResponseResult<ApplicationDto>>(`/${id}/status`, statusUpdate);
  }

  async withdrawApplication(id: string, reason: string): Promise<VoidResponseResult | null> {
    return this.post<VoidResponseResult>(`/${id}/withdraw`, { reason });
  }

  async getApplicationsByJobOffer(
    jobOfferId: string,
  ): Promise<ResponseResult<ApplicationDto[]> | null> {
    return this.get<ResponseResult<ApplicationDto[]>>(`/job-offer/${jobOfferId}`);
  }

  async getMyCandidateApplications(): Promise<ResponseResult<ApplicationDto[]> | null> {
    return this.get<ResponseResult<ApplicationDto[]>>('/my');
  }

  async getApplicationsByCandidate(
    candidateId: string,
  ): Promise<ResponseResult<ApplicationDto[]> | null> {
    return this.get<ResponseResult<ApplicationDto[]>>(`/candidate/${candidateId}`);
  }

  async getApplicationsByOrganization(
    organizationId?: string,
  ): Promise<ResponseResult<ApplicationDto[]> | null> {
    const endpoint = organizationId ? `/organization/${organizationId}` : '/organization';
    return this.get<ResponseResult<ApplicationDto[]>>(endpoint);
  }

  async getApplicationStatusHistory(
    id: string,
  ): Promise<ResponseResult<ApplicationStatusHistoryDto[]> | null> {
    return this.get<ResponseResult<ApplicationStatusHistoryDto[]>>(`/${id}/history`);
  }

  async canManageApplication(id: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/${id}/can-manage`);
  }

  async hasAppliedToJob(jobOfferId: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/check-applied/job-offer/${jobOfferId}`);
  }

  async hasCandidateAppliedToJob(
    jobOfferId: string,
    candidateId: string,
  ): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(
      `/check-applied/job-offer/${jobOfferId}/candidate/${candidateId}`,
    );
  }

  async assignUser(assignUserDto: AssignUserDto): Promise<ResponseResult<ApplicationDto> | null> {
    return this.post<ResponseResult<ApplicationDto>>(
      `/${assignUserDto.applicationId}/assign-user`,
      assignUserDto,
    );
  }

  async unassignUser(
    unassignUserDto: AssignUserDto,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    return this.post<ResponseResult<ApplicationDto>>(
      `/${unassignUserDto.applicationId}/unassign-user`,
      unassignUserDto,
    );
  }
}

export const applicationService = new ApplicationService();
export default applicationService;
