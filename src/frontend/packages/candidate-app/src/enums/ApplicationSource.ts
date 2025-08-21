export enum ApplicationSource {
  Website = 0,
  LinkedIn = 1,
  Email = 2,
  Referral = 3,
  Direct = 4,
}

export const sourceLabels = {
  [ApplicationSource.Website]: 'Site web',
  [ApplicationSource.LinkedIn]: 'LinkedIn',
  [ApplicationSource.Email]: 'Email',
  [ApplicationSource.Referral]: 'Recommandation',
  [ApplicationSource.Direct]: 'Direct',
};
