/**
 * Formate une date en chaîne de caractères au format français
 * @param dateString - La date en format ISO string ou Date
 * @returns La date formatée en français (dd/mm/yyyy)
 */
export function formatDate(dateString: string | Date): string {
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;

  return date.toLocaleDateString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

/**
 * Formate une date avec l'heure au format français
 * @param dateString - La date en format ISO string ou Date
 * @returns La date et l'heure formatées en français (dd/mm/yyyy à hh:mm)
 */
export function formatDateTime(dateString: string | Date): string {
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;

  return date.toLocaleDateString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Formate une date relative (il y a X jours, etc.)
 * @param dateString - La date en format ISO string ou Date
 * @returns La date formatée de manière relative
 */
export function formatRelativeDate(dateString: string | Date): string {
  const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
  const now = new Date();
  const diffInMs = now.getTime() - date.getTime();
  const diffInDays = Math.floor(diffInMs / (1000 * 60 * 60 * 24));

  if (diffInDays === 0) {
    return "Aujourd'hui";
  } else if (diffInDays === 1) {
    return 'Hier';
  } else if (diffInDays < 7) {
    return `Il y a ${diffInDays} jours`;
  } else if (diffInDays < 30) {
    const weeks = Math.floor(diffInDays / 7);
    return weeks === 1 ? 'Il y a 1 semaine' : `Il y a ${weeks} semaines`;
  } else if (diffInDays < 365) {
    const months = Math.floor(diffInDays / 30);
    return months === 1 ? 'Il y a 1 mois' : `Il y a ${months} mois`;
  } else {
    const years = Math.floor(diffInDays / 365);
    return years === 1 ? 'Il y a 1 an' : `Il y a ${years} ans`;
  }
}
