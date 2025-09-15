export enum ApplicationSource {
  Website,
  LinkedIn,
  Email,
  Referral,
  Direct,
}

export const sourceLabels = {
  [ApplicationSource.Website]: 'Site web',
  [ApplicationSource.LinkedIn]: 'LinkedIn',
  [ApplicationSource.Email]: 'Email',
  [ApplicationSource.Referral]: 'Recommandation',
  [ApplicationSource.Direct]: 'Direct',
};
