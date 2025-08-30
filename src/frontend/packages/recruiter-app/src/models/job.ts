/* eslint-disable @typescript-eslint/no-explicit-any */
import { JobOfferStatus, WorkMode, ContractType } from '../enums';
import type { Filter } from './base';

export interface JobOffer {
  id: string;
  title: string;
  description: string;
  requirements: string;
  location?: string;
  workMode: WorkMode;
  contractType: ContractType;
  salaryMin?: number;
  salaryMax?: number;
  salaryCurrency?: string;
  status: JobOfferStatus;
  publishedAt?: string;
  expiresAt?: string;
  createdAt: string;
  updatedAt?: string;
  organizationId: string;
  organizationName: string;
  createdByUserId: string;
  createdByUserName: string;
  isActive: boolean;
  isExpired: boolean;
}

export interface JobOfferDto {
  id: string;
  title: string;
  description: string;
  requirements: string;
  location?: string;
  workMode: WorkMode;
  contractType: ContractType;
  salaryMin?: number;
  salaryMax?: number;
  salaryCurrency?: string;
  status: JobOfferStatus;
  publishedAt?: string;
  expiresAt?: string;
  createdAt: string;
  updatedAt?: string;
  organizationId: string;
  organizationName: string;
  createdByUserId: string;
  createdByUserName: string;
  isActive: boolean;
  isExpired: boolean;
}

export interface CreateJobOfferDto {
  title: string;
  description: string;
  requirements: string;
  location?: string | undefined;
  workMode: WorkMode;
  contractType: ContractType;
  salaryMin?: number | undefined;
  salaryMax?: number | undefined;
  salaryCurrency?: string | undefined;
  expiresAt?: string | undefined;
}

export interface UpdateJobOfferDto {
  title?: string | undefined;
  description?: string | undefined;
  requirements?: string | undefined;
  location?: string | undefined;
  workMode?: WorkMode | undefined;
  contractType?: ContractType | undefined;
  salaryMin?: number | undefined;
  salaryMax?: number | undefined;
  salaryCurrency?: string | undefined;
  expiresAt?: string | undefined;
}

export interface JobOfferFilterDto extends Filter {
  title?: string;
  location?: string;
  workMode?: WorkMode;
  contractType?: ContractType;
  status?: JobOfferStatus;
  salaryMin?: number;
  salaryMax?: number;
  organizationId?: string;
  createdByUserId?: string;
  isActive?: boolean;
  isExpired?: boolean;
  publishedAfter?: string;
  publishedBefore?: string;
  expiresAfter?: string;
  expiresBefore?: string;
}

// Backward compatibility
export type CreateJobOffer = CreateJobOfferDto;
export type UpdateJobOffer = UpdateJobOfferDto;
export interface JobOfferFilter
  extends Omit<
    JobOfferFilterDto,
    'pageNumber' | 'pageSize' | 'searchTerms' | 'sortBy' | 'sortDirection'
  > {
  page?: number;
  pageSize?: number;
}

export interface PaginatedJobOffers {
  items: JobOffer[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
  };
  isSuccess: boolean;
  message: string;
  errors: string[];
}

export interface JobOfferStatusConfig {
  label: string;
  color: string;
  textColor: string;
  icon: string;
}

export const jobOfferStatusConfig: Record<JobOfferStatus, JobOfferStatusConfig> = {
  [JobOfferStatus.Draft]: {
    label: 'Brouillon',
    color: 'grey-4',
    textColor: 'grey-8',
    icon: 'edit',
  },
  [JobOfferStatus.Published]: {
    label: 'Publiée',
    color: 'positive',
    textColor: 'white',
    icon: 'visibility',
  },
  [JobOfferStatus.Closed]: {
    label: 'Fermée',
    color: 'negative',
    textColor: 'white',
    icon: 'visibility_off',
  },
};

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

export function convertJobOffer(job: any): JobOffer {
  return {
    ...job,
    workMode: convertWorkMode(job.workMode),
    contractType: convertContractType(job.contractType),
    status: convertJobOfferStatus(job.status),
  };
}

export const workModeLabels: Record<WorkMode, string> = {
  [WorkMode.OnSite]: 'Sur site',
  [WorkMode.Hybrid]: 'Hybride',
  [WorkMode.FullRemote]: 'Télétravail complet',
};

export const contractTypeLabels: Record<ContractType, string> = {
  [ContractType.FullTime]: 'CDI',
  [ContractType.PartTime]: 'Temps partiel',
  [ContractType.Contract]: 'CDD',
  [ContractType.Freelance]: 'Freelance',
  [ContractType.Internship]: 'Stage',
  [ContractType.Temporary]: 'Temporaire',
};

export type Job = JobOffer;
export type JobStatus = JobOfferStatus;
export const jobStatusConfig = jobOfferStatusConfig;
