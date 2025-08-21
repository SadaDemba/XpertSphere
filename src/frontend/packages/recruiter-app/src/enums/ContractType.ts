export enum ContractType {
  FullTime = 0,
  PartTime = 1,
  Contract = 2,
  Freelance = 3,
  Internship = 4,
  Temporary = 5,
}

export const contractTypeLabels = {
  [ContractType.FullTime]: 'Temps plein',
  [ContractType.PartTime]: 'Temps partiel',
  [ContractType.Contract]: 'Contrat',
  [ContractType.Freelance]: 'Freelance',
  [ContractType.Internship]: 'Stage',
  [ContractType.Temporary]: 'Intérim',
};
