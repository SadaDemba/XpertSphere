import { BaseClient } from './BaseClient';
import type {
  ApplicationDto,
  CreateApplicationDto,
  WithdrawApplicationRequest,
} from '../models/application';
import { ResponseResult, VoidResponseResult } from 'src/models';

export class ApplicationService extends BaseClient {
  constructor() {
    super('/Applications');
  }

  async createApplication(
    createApplicationDto: CreateApplicationDto,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    return this.post<ResponseResult<ApplicationDto>>(
      '',
      createApplicationDto,
      'Erreur lors de la création de la candidature',
    );
  }

  async getMyApplications(): Promise<ResponseResult<ApplicationDto[]> | null> {
    return this.get<ResponseResult<ApplicationDto[]>>(
      '/my',
      {},
      'Erreur lors de la récupération de vos candidatures',
    );
  }

  async getApplicationById(id: string): Promise<ResponseResult<ApplicationDto> | null> {
    return this.get<ResponseResult<ApplicationDto>>(
      `/${id}`,
      {},
      'Erreur lors de la récupération de la candidature',
    );
  }

  async withdrawApplication(id: string, reason: string): Promise<VoidResponseResult | null> {
    const request: WithdrawApplicationRequest = { reason };

    return await this.post<VoidResponseResult>(
      `/${id}/withdraw`,
      request,
      'Erreur lors du retrait de la candidature',
    );
  }

  async hasAppliedToJob(jobOfferId: string): Promise<ResponseResult<boolean> | null> {
    return await this.get<ResponseResult<boolean>>(
      `/check-applied/job-offer/${jobOfferId}`,
      {},
      'Erreur lors de la vérification de la candidature',
    );
  }

  async updateApplication(
    id: string,
    coverLetter?: string,
    additionalNotes?: string,
  ): Promise<ResponseResult<ApplicationDto> | null> {
    const updateDto = { coverLetter, additionalNotes };

    return this.put<ResponseResult<ApplicationDto>>(
      `/${id}`,
      updateDto,
      'Erreur lors de la mise à jour de la candidature',
    );
  }
}

export const applicationService = new ApplicationService();
