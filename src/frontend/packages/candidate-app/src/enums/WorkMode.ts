export enum WorkMode {
  OnSite = 0,
  Hybrid = 1,
  FullRemote = 2,
}

export const workModeLabels = {
  [WorkMode.OnSite]: 'Présentiel',
  [WorkMode.Hybrid]: 'Hybride',
  [WorkMode.FullRemote]: 'Télétravail complet',
};

export const workModeStringToEnum: Record<string, WorkMode> = {
  OnSite: WorkMode.OnSite,
  Hybrid: WorkMode.Hybrid,
  FullRemote: WorkMode.FullRemote,
};
