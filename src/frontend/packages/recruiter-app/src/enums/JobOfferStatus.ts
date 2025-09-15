export enum JobOfferStatus {
  Draft = 0,
  Published = 1,
  Closed = 2,
}

export const jobOfferStatusLabels = {
  [JobOfferStatus.Draft]: 'Brouillon',
  [JobOfferStatus.Published]: 'Publié',
  [JobOfferStatus.Closed]: 'Clôturé',
};

export function getJobOfferStatusName(status: JobOfferStatus): string {
  switch (status) {
    case JobOfferStatus.Draft:
      return 'Draft';
    case JobOfferStatus.Published:
      return 'Published';
    case JobOfferStatus.Closed:
      return 'Closed';
    default:
      return '';
  }
}
