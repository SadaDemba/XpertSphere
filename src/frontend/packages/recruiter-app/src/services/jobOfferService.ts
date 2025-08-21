import { BaseClient } from './BaseClient';
import type {
  JobOffer,
  CreateJobOfferDto,
  UpdateJobOfferDto,
  JobOfferFilterDto,
} from '../models/job';
import type { PaginatedResult } from '../models/base';

export class JobOfferService extends BaseClient {
  private readonly ENDPOINT = '/JobOffers';

  constructor() {
    super('/JobOffers');
  }

  /**
   * Get all job offers
   */
  async getAllJobOffers(): Promise<JobOffer[]> {
    const response = await this.get<JobOffer[]>('');
    return response || [];
  }

  /**
   * Get job offers with pagination and filters
   */
  async getPaginatedJobOffers(filter: JobOfferFilterDto = {}): Promise<PaginatedResult<JobOffer>> {
    const response = await this.get<PaginatedResult<JobOffer>>('', { params: filter });
    return (
      response || {
        items: [],
        pageNumber: 1,
        pageSize: 10,
        totalItems: 0,
        totalPages: 0,
        hasPrevious: false,
        hasNext: false,
      }
    );
  }

  /**
   * Get job offer by ID
   */
  async getJobOfferById(id: string): Promise<JobOffer | null> {
    const response = await this.get<JobOffer>(`/${id}`);
    return response;
  }

  /**
   * Create a new job offer
   */
  async createJobOffer(jobOffer: CreateJobOfferDto): Promise<JobOffer | null> {
    const response = await this.post<JobOffer>('', jobOffer);
    return response;
  }

  /**
   * Update an existing job offer
   */
  async updateJobOffer(id: string, jobOffer: UpdateJobOfferDto): Promise<JobOffer | null> {
    const response = await this.put<JobOffer>(`/${id}`, jobOffer);
    return response;
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
    const response = await this.get<JobOffer[]>(`/organization/${organizationId}`);
    return response || [];
  }

  /**
   * Get current user's job offers
   */
  async getMyJobOffers(): Promise<JobOffer[]> {
    const response = await this.get<JobOffer[]>('/my');
    return response || [];
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
