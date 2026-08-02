/**
 * Règles de complexité de mot de passe alignées sur la politique réelle du backend
 * (`ConfigurePasswordOptions`, `SecurityExtensions.cs`, `XpertSphere.MonolithApi`) : au moins une
 * majuscule, une minuscule, un chiffre, un caractère non alphanumérique, longueur minimale 8.
 *
 * Longueur minimale fixée à 8 dans tous les cas (même si le backend accepte 6 en Development) :
 * ce package est un bundle statique qui ne connaît pas l'environnement du backend auquel il parle,
 * et 8 caractères satisfait toujours l'exigence de longueur du backend, quel que soit
 * l'environnement cible.
 *
 * Factorisé ici pour être partagé entre les 3 emplacements actifs de création/modification de mot
 * de passe de ce package (`UsersPage.vue` création et dialog « Reset Password », `ProfilePage.vue`
 * changement de mot de passe) et éviter toute divergence future entre 3 copies indépendantes.
 *
 * Limite connue et acceptée (voir
 * `src/backend/XpertSphere.MonolithApi/.claude/specifications/localize-identity-error-messages.md`,
 * section « Coordination frontend ») : la regex `/[^a-zA-Z0-9]/` traite une lettre accentuée (ex.
 * "é") comme un caractère spécial, alors que le backend (`char.IsLetterOrDigit`, Unicode) la
 * considère alphanumérique et ne validerait donc pas l'exigence `RequireNonAlphanumeric` pour un
 * mot de passe qui ne contiendrait qu'un tel caractère comme seul symbole non-ASCII. Cas limite
 * volontairement non traité (complexité disproportionnée pour reproduire `char.IsLetterOrDigit`
 * côté client).
 */
export type PasswordRule = (val: string) => boolean | string;

export function usePasswordComplexityRules(): PasswordRule[] {
  return [
    (val: string) => val.length >= 8 || 'Minimum 8 caractères',
    (val: string) => /[A-Z]/.test(val) || 'Au moins une majuscule',
    (val: string) => /[a-z]/.test(val) || 'Au moins une minuscule',
    (val: string) => /[0-9]/.test(val) || 'Au moins un chiffre',
    (val: string) => /[^a-zA-Z0-9]/.test(val) || 'Au moins un caractère spécial',
  ];
}
