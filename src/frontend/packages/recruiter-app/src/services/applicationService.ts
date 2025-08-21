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

export class ApplicationService extends BaseClient {
  constructor() {
    super('/Applications');
  }

  async getAllApplications(): Promise<ApplicationDto[]> {
    const response = await this.get<ApplicationDto[]>('');
    return response || [];
  }

  async getPaginatedApplications(
    filter: ApplicationFilterDto = { organizationId: '' },
  ): Promise<PaginatedApplications> {
    const response = await this.get<PaginatedApplications>('/paginated', { params: filter });
    return (
      response || {
        items: [],
        pagination: {
          currentPage: 1,
          pageSize: 10,
          totalItems: 0,
          totalPages: 0,
          hasPrevious: false,
          hasNext: false,
        },
        isSuccess: false,
        message: 'No data',
        errors: [],
      }
    );
  }

  async getApplicationById(id: string): Promise<ApplicationDto | null> {
    const response = await this.get<ApplicationDto>(`/${id}`);
    return response;
  }

  async createApplication(application: CreateApplicationDto): Promise<ApplicationDto | null> {
    const response = await this.post<ApplicationDto>('', application);
    return response;
  }

  async updateApplication(
    id: string,
    application: UpdateApplicationDto,
  ): Promise<ApplicationDto | null> {
    const response = await this.put<ApplicationDto>(`/${id}`, application);
    return response;
  }

  async deleteApplication(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }

  async updateApplicationStatus(
    id: string,
    statusUpdate: UpdateApplicationStatusDto,
  ): Promise<ApplicationDto | null> {
    const response = await this.put<ApplicationDto>(`/${id}/status`, statusUpdate);
    return response;
  }

  async withdrawApplication(id: string, reason: string): Promise<boolean> {
    const response = await this.post(`/${id}/withdraw`, { reason });
    return response !== null;
  }

  async getApplicationsByJobOffer(jobOfferId: string): Promise<ApplicationDto[]> {
    const response = await this.get<ApplicationDto[]>(`/job-offer/${jobOfferId}`);
    return response || [];
  }

  async getMyCandidateApplications(): Promise<ApplicationDto[]> {
    const response = await this.get<ApplicationDto[]>('/my');
    return response || [];
  }

  async getApplicationsByCandidate(candidateId: string): Promise<ApplicationDto[]> {
    const response = await this.get<ApplicationDto[]>(`/candidate/${candidateId}`);
    return response || [];
  }

  async getApplicationsByOrganization(organizationId?: string): Promise<ApplicationDto[]> {
    const endpoint = organizationId ? `/organization/${organizationId}` : '/organization';
    const response = await this.get<ApplicationDto[]>(endpoint);
    return response || [];
  }

  async getApplicationStatusHistory(id: string): Promise<ApplicationStatusHistoryDto[]> {
    const response = await this.get<ApplicationStatusHistoryDto[]>(`/${id}/history`);
    return response || [];
  }

  async canManageApplication(id: string): Promise<boolean> {
    const response = await this.get<boolean>(`/${id}/can-manage`);
    return response || false;
  }

  async hasAppliedToJob(jobOfferId: string): Promise<boolean> {
    const response = await this.get<boolean>(`/check-applied/job-offer/${jobOfferId}`);
    return response || false;
  }

  async hasCandidateAppliedToJob(jobOfferId: string, candidateId: string): Promise<boolean> {
    const response = await this.get<boolean>(
      `/check-applied/job-offer/${jobOfferId}/candidate/${candidateId}`,
    );
    return response || false;
  }

  async assignUser(assignUserDto: AssignUserDto): Promise<ApplicationDto | null> {
    const response = await this.post<ApplicationDto>(
      `/${assignUserDto.applicationId}/assign-user`,
      assignUserDto,
    );
    return response;
  }

  async unassignUser(unassignUserDto: AssignUserDto): Promise<ApplicationDto | null> {
    const response = await this.post<ApplicationDto>(
      `/${unassignUserDto.applicationId}/unassign-user`,
      unassignUserDto,
    );
    return response;
  }
}

export const applicationService = new ApplicationService();
export default applicationService;
