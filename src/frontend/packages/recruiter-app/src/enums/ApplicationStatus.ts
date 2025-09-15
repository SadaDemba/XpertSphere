export enum ApplicationStatus {
  Applied,
  Reviewed,
  PhoneScreening,
  TechnicalTest,
  TechnicalInterview,
  FinalInterview,
  OfferMade,
  Accepted,
  Rejected,
  Withdrawn,
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
