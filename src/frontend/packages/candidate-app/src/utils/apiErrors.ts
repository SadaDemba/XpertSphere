/**
 * Normalizes backend error responses into a flat list of human-readable messages.
 *
 * Handles the two error response shapes produced by `XpertSphere.MonolithApi`:
 * - `ServiceResult` / `AuthResult` : `errors` is a non-empty array of strings.
 * - `ValidationProblemDetails` (ASP.NET Core `[ApiController]` model validation,
 *   RFC 9110) : `errors` is an object mapping a field key to an array of messages.
 *
 * Falls back to `message` (ServiceResult) or `title` (ValidationProblemDetails)
 * when `errors` is absent, not in a recognized shape, or empty. Never throws;
 * returns an empty array when nothing usable can be extracted.
 *
 * Pure function: no dependency on Vue/Pinia/Axios, safe to unit-test in isolation.
 */
export function extractApiErrorMessages(payload: unknown): string[] {
  if (!payload || typeof payload !== 'object') return [];
  const obj = payload as Record<string, unknown>;

  // Format ServiceResult / AuthResult : errors est un tableau de chaînes non vide
  if (
    Array.isArray(obj.errors) &&
    obj.errors.length > 0 &&
    obj.errors.every((e) => typeof e === 'string')
  ) {
    return obj.errors as string[];
  }

  // Format ValidationProblemDetails : errors est un objet clé -> tableau de messages
  if (obj.errors && typeof obj.errors === 'object' && !Array.isArray(obj.errors)) {
    const flattened = Object.entries(obj.errors as Record<string, string[]>).flatMap(
      ([key, messages]) => (messages ?? []).map((msg) => `${key}: ${msg}`),
    );
    if (flattened.length > 0) return flattened;
  }

  // Repli : message (ServiceResult) ou title (ValidationProblemDetails)
  const fallback =
    (typeof obj.message === 'string' && obj.message) ||
    (typeof obj.title === 'string' && obj.title) ||
    '';
  return fallback ? [fallback] : [];
}
