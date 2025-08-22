/* eslint-disable @typescript-eslint/no-explicit-any */
import { BaseClient } from './BaseClient';
import type {
  JobOffer,
  CreateJobOfferDto,
  UpdateJobOfferDto,
  JobOfferFilterDto,
} from '../models/job';
import type { JobPaginatedResult } from '../models/base';
import { JobOfferStatus, WorkMode, ContractType } from '../enums';

// Fonctions de conversion des strings backend vers enums frontend
function convertWorkMode(workMode: string): WorkMode {
  switch (workMode) {
    case 'OnSite':
      return WorkMode.OnSite;
    case 'Hybrid':
      return WorkMode.Hybrid;
    case 'FullRemote':
      return WorkMode.FullRemote;
    default:
      return WorkMode.OnSite;
  }
}

function convertContractType(contractType: string): ContractType {
  switch (contractType) {
    case 'FullTime':
      return ContractType.FullTime;
    case 'PartTime':
      return ContractType.PartTime;
    case 'Contract':
      return ContractType.Contract;
    case 'Freelance':
      return ContractType.Freelance;
    case 'Internship':
      return ContractType.Internship;
    case 'Temporary':
      return ContractType.Temporary;
    default:
      return ContractType.FullTime;
  }
}

function convertJobOfferStatus(status: string): JobOfferStatus {
  switch (status) {
    case 'Draft':
      return JobOfferStatus.Draft;
    case 'Published':
      return JobOfferStatus.Published;
    case 'Closed':
      return JobOfferStatus.Closed;
    default:
      return JobOfferStatus.Draft;
  }
}

// Fonction pour convertir un job offer du backend
function convertJobOffer(job: any): JobOffer {
  return {
    ...job,
    workMode: convertWorkMode(job.workMode),
    contractType: convertContractType(job.contractType),
    status: convertJobOfferStatus(job.status),
  };
}

export class JobOfferService extends BaseClient {
  private readonly ENDPOINT = '/JobOffers';

  constructor() {
    super('/JobOffers');
  }

  /**
   * Get all job offers
   */
  async getAllJobOffers(): Promise<JobOffer[]> {
    console.log('Here');
    const response = await this.get<any[]>('');
    console.log(response);
    return response ? response.map(convertJobOffer) : [];
  }

  /**
   * Get job offers with pagination and filters
   */
  async getPaginatedJobOffers(
    filter: JobOfferFilterDto = {},
  ): Promise<JobPaginatedResult<JobOffer>> {
    console.log(filter);
    const response = await this.get<JobPaginatedResult<JobOffer>>('/paginated', { params: filter });
    console.log(response);
    if (response) {
      return {
        ...response,
        items: response.items.map(convertJobOffer),
      };
    }
    return {
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
      message: '',
      errors: [],
    };
  }

  /**
   * Get job offer by ID
   */
  async getJobOfferById(id: string): Promise<JobOffer | null> {
    const response = await this.get<any>(`/${id}`);
    return response ? convertJobOffer(response) : null;
  }

  /**
   * Create a new job offer
   */
  async createJobOffer(jobOffer: CreateJobOfferDto): Promise<JobOffer | null> {
    const response = await this.post<any>('', jobOffer);
    return response ? convertJobOffer(response) : null;
  }

  /**
   * Update an existing job offer
   */
  async updateJobOffer(id: string, jobOffer: UpdateJobOfferDto): Promise<JobOffer | null> {
    const response = await this.put<any>(`/${id}`, jobOffer);
    return response ? convertJobOffer(response) : null;
  }

  /**
   * Delete a job offer
   */
  async deleteJobOffer(id: string): Promise<boolean> {
    const response = await this.delete(`/${id}`);
    return response !== null;
  }

  /**
   * Publish a job offer
   */
  async publishJobOffer(id: string): Promise<boolean> {
    const response = await this.post(`/${id}/publish`);
    return response !== null;
  }

  /**
   * Close a job offer
   */
  async closeJobOffer(id: string): Promise<boolean> {
    const response = await this.post(`/${id}/close`);
    return response !== null;
  }

  /**
   * Get job offers by organization
   */
  async getJobOffersByOrganization(organizationId: string): Promise<JobOffer[]> {
    const response = await this.get<any[]>(`/organization/${organizationId}`);
    return response ? response.map(convertJobOffer) : [];
  }

  /**
   * Get current user's job offers
   */
  async getMyJobOffers(): Promise<JobOffer[]> {
    const response = await this.get<any[]>('/my');
    return response ? response.map(convertJobOffer) : [];
  }

  /**
   * Check if current user can manage a specific job offer
   */
  async canManageJobOffer(id: string): Promise<boolean> {
    const response = await this.get<boolean>(`/${id}/can-manage`);
    return response || false;
  }
}

// Export singleton instance
export const jobOfferService = new JobOfferService();
export default jobOfferService;
