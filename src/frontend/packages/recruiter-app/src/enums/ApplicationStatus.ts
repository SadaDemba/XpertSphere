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
