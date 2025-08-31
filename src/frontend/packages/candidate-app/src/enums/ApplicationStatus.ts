export enum ApplicationStatus {
  Applied = 0,
  Reviewed = 1,
  PhoneScreening = 2,
  TechnicalTest = 3,
  TechnicalInterview = 4,
  FinalInterview = 5,
  OfferMade = 6,
  Accepted = 7,
  Rejected = 8,
  Withdrawn = 9,
}

export const statusOptions = [
  { label: 'Candidature déposée', value: ApplicationStatus.Applied },
  { label: 'Examinée', value: ApplicationStatus.Reviewed },
  { label: 'Entretien téléphonique', value: ApplicationStatus.PhoneScreening },
  { label: 'Test technique', value: ApplicationStatus.TechnicalTest },
  { label: 'Entretien technique', value: ApplicationStatus.TechnicalInterview },
  { label: 'Entretien final', value: ApplicationStatus.FinalInterview },
  { label: 'Offre faite', value: ApplicationStatus.OfferMade },
  { label: 'Acceptée', value: ApplicationStatus.Accepted },
  { label: 'Rejetée', value: ApplicationStatus.Rejected },
  { label: 'Retirée', value: ApplicationStatus.Withdrawn },
];

export function mapToString(status: ApplicationStatus): string {
  switch (status) {
    case ApplicationStatus.Applied:
      return 'Applied';

    case ApplicationStatus.Reviewed:
      return 'Reviewed';

    case ApplicationStatus.PhoneScreening:
      return 'PhoneScreening';

    case ApplicationStatus.TechnicalTest:
      return 'TechnicalTest';

    case ApplicationStatus.TechnicalInterview:
      return 'TechnicalInterview';

    case ApplicationStatus.FinalInterview:
      return 'FinalInterview';

    case ApplicationStatus.OfferMade:
      return 'OfferMade';

    case ApplicationStatus.Accepted:
      return 'Accepted';

    case ApplicationStatus.Rejected:
      return 'Rejected';

    case ApplicationStatus.Withdrawn:
      return 'Withdrawn';

    default:
      return 'Unknown';
  }
}
