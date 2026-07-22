// Volontairement un enum à valeurs string (pas numérique comme WorkMode/ContractType) : le
// backend sérialise Currency en JSON via JsonStringEnumConverter ("EUR"/"XOF", jamais un indice
// numérique), et ce champ est purement déclaratif/d'affichage côté candidat. Un enum numérique
// casserait silencieusement l'affichage (la valeur reçue "XOF" ne correspondrait à aucune clé
// numérique de l'enum).
export enum Currency {
  EUR = 'EUR',
  XOF = 'XOF',
}

export const currencyLabels: Record<Currency, string> = {
  [Currency.EUR]: 'Euro (EUR)',
  [Currency.XOF]: 'Franc CFA (XOF)',
};

export const currencyOptions = Object.entries(currencyLabels).map(([value, label]) => ({
  label,
  value: value as Currency,
}));
