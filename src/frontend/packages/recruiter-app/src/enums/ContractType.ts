export enum ContractType {
  FullTime = 0,
  PartTime = 1,
  Contract = 2,
  Freelance = 3,
  Internship = 4,
  Temporary = 5,
}

export function getContractTypeValue(contractTpe: ContractType): number {
  switch (contractTpe) {
    case ContractType.FullTime:
      return 0;
    case ContractType.PartTime:
      return 1;
    case ContractType.Contract:
      return 2;
    case ContractType.Freelance:
      return 3;
    case ContractType.Internship:
      return 4;

    default:
      return 5;
  }
}

export const contractTypeLabels = {
  [ContractType.FullTime]: 'Temps plein',
  [ContractType.PartTime]: 'Temps partiel',
  [ContractType.Contract]: 'Contrat',
  [ContractType.Freelance]: 'Freelance',
  [ContractType.Internship]: 'Stage',
  [ContractType.Temporary]: 'Intérim',
};
