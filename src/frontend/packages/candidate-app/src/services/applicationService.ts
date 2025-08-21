import { BaseClient } from './BaseClient';
import type {
  ApplicationDto,
  CreateApplicationDto,
  WithdrawApplicationRequest,
} from '../models/application';

export class ApplicationService extends BaseClient {
  constructor() {
    super('/Applications');
  }

  async createApplication(
    createApplicationDto: CreateApplicationDto,
  ): Promise<ApplicationDto | null> {
    return this.post<ApplicationDto>(
      '',
      createApplicationDto,
      'Erreur lors de la création de la candidature',
    );
  }

  async getMyApplications(): Promise<ApplicationDto[] | null> {
    return this.get<ApplicationDto[]>(
      '/my',
      {},
      'Erreur lors de la récupération de vos candidatures',
    );
  }

  async getApplicationById(id: string): Promise<ApplicationDto | null> {
    return this.get<ApplicationDto>(
      `/${id}`,
      {},
      'Erreur lors de la récupération de la candidature',
    );
  }

  async withdrawApplication(id: string, reason: string): Promise<void> {
    const request: WithdrawApplicationRequest = { reason };

    await this.post(`/${id}/withdraw`, request, 'Erreur lors du retrait de la candidature');
  }

  async hasAppliedToJob(jobOfferId: string): Promise<boolean> {
    const result = await this.get<boolean>(
      `/check-applied/job-offer/${jobOfferId}`,
      {},
      'Erreur lors de la vérification de la candidature',
    );
    return result || false;
  }

  async updateApplication(
    id: string,
    coverLetter?: string,
    additionalNotes?: string,
  ): Promise<ApplicationDto | null> {
    const updateDto = { coverLetter, additionalNotes };

    return this.put<ApplicationDto>(
      `/${id}`,
      updateDto,
      'Erreur lors de la mise à jour de la candidature',
    );
  }
}

export const applicationService = new ApplicationService();
