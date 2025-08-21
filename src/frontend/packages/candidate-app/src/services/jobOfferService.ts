import { BaseClient } from './BaseClient';
import type { JobOfferDto, JobOfferFilterDto, PaginatedJobOffers } from '../models/job';

export class JobOfferService extends BaseClient {
  constructor() {
    super('/JobOffers');
  }

  async getAllJobOffers(): Promise<JobOfferDto[] | null> {
    return this.get<JobOfferDto[]>('', {}, "Erreur lors de la récupération des offres d'emploi");
  }

  async getAllPaginatedJobOffers(filter: JobOfferFilterDto): Promise<PaginatedJobOffers | null> {
    const params = new URLSearchParams();

    Object.entries(filter).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params.append(key, value.toString());
      }
    });

    return this.get<PaginatedJobOffers>(
      `/paginated?${params.toString()}`,
      {},
      "Erreur lors de la récupération des offres d'emploi",
    );
  }

  async getJobOfferById(id: string): Promise<JobOfferDto | null> {
    return this.get<JobOfferDto>(
      `/${id}`,
      {},
      "Erreur lors de la récupération de l'offre d'emploi",
    );
  }

  async getJobOffersByOrganization(organizationId: string): Promise<JobOfferDto[] | null> {
    return this.get<JobOfferDto[]>(
      `/organization/${organizationId}`,
      {},
      "Erreur lors de la récupération des offres de l'organisation",
    );
  }
}

export const jobOfferService = new JobOfferService();
