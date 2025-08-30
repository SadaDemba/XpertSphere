import { BaseClient } from './BaseClient';
import type {
  JobOffer,
  CreateJobOfferDto,
  UpdateJobOfferDto,
  JobOfferFilterDto,
} from '../models/job';
import type { PaginatedResult, ResponseResult, VoidResponseResult } from '../models/base';

export class JobOfferService extends BaseClient {
  constructor() {
    super('/JobOffers');
  }

  /**
   * Get all job offers
   */
  async getAllJobOffers(): Promise<ResponseResult<JobOffer[]> | null> {
    return this.get<ResponseResult<JobOffer[]>>('');
  }

  /**
   * Get job offers with pagination and filters
   */
  async getPaginatedJobOffers(
    filter: JobOfferFilterDto = {},
  ): Promise<PaginatedResult<JobOffer> | null> {
    return this.get<PaginatedResult<JobOffer>>('/paginated', { params: filter });
  }

  /**
   * Get job offer by ID
   */
  async getJobOfferById(id: string): Promise<ResponseResult<JobOffer> | null> {
    return await this.get<ResponseResult<JobOffer>>(`/${id}`);
  }

  /**
   * Create a new job offer
   */
  async createJobOffer(jobOffer: CreateJobOfferDto): Promise<ResponseResult<JobOffer> | null> {
    return this.post<ResponseResult<JobOffer>>('', jobOffer);
  }

  /**
   * Update an existing job offer
   */
  async updateJobOffer(
    id: string,
    jobOffer: UpdateJobOfferDto,
  ): Promise<ResponseResult<JobOffer> | null> {
    return this.put<ResponseResult<JobOffer>>(`/${id}`, jobOffer);
  }

  /**
   * Delete a job offer
   */
  async deleteJobOffer(id: string): Promise<VoidResponseResult | null> {
    return this.delete<VoidResponseResult>(`/${id}`);
  }

  /**
   * Publish a job offer
   */
  async publishJobOffer(id: string): Promise<VoidResponseResult | null> {
    return this.post<VoidResponseResult>(`/${id}/publish`);
  }

  /**
   * Close a job offer
   */
  async closeJobOffer(id: string): Promise<VoidResponseResult | null> {
    return this.post<VoidResponseResult>(`/${id}/close`);
  }

  /**
   * Get job offers by organization
   */
  async getJobOffersByOrganization(
    organizationId: string,
  ): Promise<ResponseResult<JobOffer[]> | null> {
    return this.get<ResponseResult<JobOffer[]>>(`/organization/${organizationId}`);
  }

  /**
   * Get current user's job offers
   */
  async getMyJobOffers(): Promise<ResponseResult<JobOffer[]> | null> {
    return this.get<ResponseResult<JobOffer[]>>('/my');
  }

  /**
   * Check if current user can manage a specific job offer
   */
  async canManageJobOffer(id: string): Promise<ResponseResult<boolean> | null> {
    return this.get<ResponseResult<boolean>>(`/${id}/can-manage`);
  }
}
// Export singleton instance
export const jobOfferService = new JobOfferService();
export default jobOfferService;
