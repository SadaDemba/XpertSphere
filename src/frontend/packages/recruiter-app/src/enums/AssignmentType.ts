// Valeurs alignées sur l'enum backend `AssignmentType` (sérialisé en nom de
// chaîne via JsonStringEnumConverter) : le nom envoyé doit correspondre
// exactement à `TechnicalEvaluator` / `Manager`.
export enum AssignmentType {
  TechnicalEvaluator = 'TechnicalEvaluator',
  Manager = 'Manager',
}
