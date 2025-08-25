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

export interface JobOfferFilterDto extends Filter {
  title?: string;
  location?: string | undefined;
  workMode?: WorkMode | undefined;
  contractType?: ContractType | undefined;
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

export interface JobOfferStatusConfig {
  label: string;
  color: string;
  textColor: string;
  icon: string;
}

export const jobOfferStatusConfig: Record<string, JobOfferStatusConfig> = {
  Draft: {
    label: 'Brouillon',
    color: 'grey-4',
    textColor: 'grey-8',
    icon: 'edit',
  },
  Published: {
    label: 'Publiée',
    color: 'positive',
    textColor: 'white',
    icon: 'visibility',
  },
  Closed: {
    label: 'Fermée',
    color: 'negative',
    textColor: 'white',
    icon: 'visibility_off',
  },
};

export const workModeLabels: Record<string, string> = {
  OnSite: 'Sur site',
  Hybrid: 'Hybride',
  FullRemote: 'Télétravail complet',
};

export const contractTypeLabels: Record<string, string> = {
  FullTime: 'CDI',
  PartTime: 'Temps partiel',
  Contract: 'CDD',
  Freelance: 'Freelance',
  Internship: 'Stage',
  Temporary: 'Temporaire',
};
