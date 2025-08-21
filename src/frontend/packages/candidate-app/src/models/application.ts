import { ApplicationStatus, ApplicationSource } from '../enums';

export interface Application {
  id: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  jobId: string;
  jobTitle: string;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string;
  source: ApplicationSource;
  coverLetter: string;
  resumeUrl: string;
  rating: number | null;
  notes: string;
}

export interface ApplicationStatusConfig {
  color: string;
  textColor: string;
  icon: string;
  label: string;
}

export const applicationStatusConfig: Record<ApplicationStatus, ApplicationStatusConfig> = {
  [ApplicationStatus.Applied]: {
    color: 'grey',
    textColor: 'white',
    icon: 'send',
    label: 'Candidature déposée',
  },
  [ApplicationStatus.Reviewed]: {
    color: 'blue',
    textColor: 'white',
    icon: 'rate_review',
    label: 'Examinée',
  },
  [ApplicationStatus.PhoneScreening]: {
    color: 'cyan',
    textColor: 'white',
    icon: 'phone',
    label: 'Entretien téléphonique',
  },
  [ApplicationStatus.TechnicalTest]: {
    color: 'indigo',
    textColor: 'white',
    icon: 'code',
    label: 'Test technique',
  },
  [ApplicationStatus.TechnicalInterview]: {
    color: 'purple',
    textColor: 'white',
    icon: 'developer_mode',
    label: 'Entretien technique',
  },
  [ApplicationStatus.FinalInterview]: {
    color: 'deep-purple',
    textColor: 'white',
    icon: 'groups',
    label: 'Entretien final',
  },
  [ApplicationStatus.OfferMade]: {
    color: 'teal',
    textColor: 'white',
    icon: 'local_offer',
    label: 'Offre faite',
  },
  [ApplicationStatus.Accepted]: {
    color: 'positive',
    textColor: 'white',
    icon: 'check_circle',
    label: 'Acceptée',
  },
  [ApplicationStatus.Rejected]: {
    color: 'negative',
    textColor: 'white',
    icon: 'cancel',
    label: 'Rejetée',
  },
  [ApplicationStatus.Withdrawn]: {
    color: 'warning',
    textColor: 'white',
    icon: 'remove_circle',
    label: 'Retirée',
  },
};

export const applicationSourceLabels: Record<ApplicationSource, string> = {
  [ApplicationSource.Website]: 'Site web',
  [ApplicationSource.LinkedIn]: 'LinkedIn',
  [ApplicationSource.Email]: 'Email',
  [ApplicationSource.Referral]: 'Recommandation',
  [ApplicationSource.Direct]: 'Direct',
};

export interface ApplicationDto {
  id: string;
  coverLetter?: string;
  additionalNotes?: string;
  currentStatus: ApplicationStatus;
  statusDisplayName: string;
  rating?: number;
  appliedAt: string;
  lastUpdatedAt?: string;
  createdAt: string;
  updatedAt?: string;

  jobOfferId: string;
  jobOfferTitle: string;
  organizationName: string;

  candidateId: string;
  candidateName: string;
  candidateEmail: string;

  assignedTechnicalEvaluatorId?: string;
  assignedTechnicalEvaluatorName?: string;

  assignedManagerId?: string;
  assignedManagerName?: string;

  isActive: boolean;
  isCompleted: boolean;
  isInProgress: boolean;

  statusHistory: ApplicationStatusHistoryDto[];
}

export interface CreateApplicationDto {
  jobOfferId: string;
  coverLetter?: string;
  additionalNotes?: string;
}

export interface UpdateApplicationDto {
  coverLetter?: string;
  additionalNotes?: string;
  rating?: number;
}

export interface ApplicationStatusHistoryDto {
  id: string;
  applicationId: string;
  status: ApplicationStatus;
  statusDisplayName: string;
  comment: string;
  rating?: number;
  ratingDescription: string;
  hasRating: boolean;
  updatedByUserId: string;
  updatedByUserName: string;
  updatedAt: string;
}

export interface WithdrawApplicationRequest {
  reason: string;
}
