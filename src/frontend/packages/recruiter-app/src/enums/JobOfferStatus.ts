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
