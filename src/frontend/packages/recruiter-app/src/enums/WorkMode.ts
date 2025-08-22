export enum WorkMode {
  OnSite = 0,
  Hybrid = 1,
  FullRemote = 2,
}

export function getWorkModeValue(workMode: WorkMode): number {
  switch (workMode) {
    case WorkMode.OnSite:
      return 0;
    case WorkMode.Hybrid:
      return 1;
    case WorkMode.FullRemote:
      return 2;

    default:
      console.log(workMode);
      return -1;
  }
}

export const workModeLabels = {
  [WorkMode.OnSite]: 'Présentiel',
  [WorkMode.Hybrid]: 'Hybride',
  [WorkMode.FullRemote]: 'Télétravail complet',
};
