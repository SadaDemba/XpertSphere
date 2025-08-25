import { BaseClient } from './BaseClient';
import type { JobOffer, JobOfferDto, JobOfferFilterDto } from '../models/job';
import { PaginatedResult, ResponseResult } from 'src/models';

export class JobOfferService extends BaseClient {
  constructor() {
    super('/JobOffers');
  }

  async getAllJobOffers(): Promise<ResponseResult<JobOfferDto[]> | null> {
    return this.get<ResponseResult<JobOfferDto[]>>(
      '',
      {},
      "Erreur lors de la récupération des offres d'emploi",
    );
  }

  async getAllPaginatedJobOffers(
    filter: JobOfferFilterDto,
  ): Promise<PaginatedResult<JobOffer> | null> {
    const params = new URLSearchParams();

    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.append(key, value.toString());
      }
    });

    return this.get<PaginatedResult<JobOffer>>(
      `/paginated?${params.toString()}`,
      {},
      "Erreur lors de la récupération des offres d'emploi",
    );
  }

  async getJobOfferById(id: string): Promise<ResponseResult<JobOfferDto> | null> {
    return this.get<ResponseResult<JobOfferDto>>(
      `/${id}`,
      {},
      "Erreur lors de la récupération de l'offre d'emploi",
    );
  }

  async getJobOffersByOrganization(
    organizationId: string,
  ): Promise<ResponseResult<JobOfferDto[]> | null> {
    return this.get<ResponseResult<JobOfferDto[]>>(
      `/organization/${organizationId}`,
      {},
      "Erreur lors de la récupération des offres de l'organisation",
    );
  }
}

export const jobOfferService = new JobOfferService();
