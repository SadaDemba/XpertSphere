import { date } from 'quasar';

export const formatDate = (dateString: string): string => {
  const dateToFormat = new Date(dateString);
  const currentDate = new Date();
  const diffInDays = Math.floor(
    (currentDate.getTime() - dateToFormat.getTime()) / (1000 * 3600 * 24),
  );

  if (diffInDays === 0) {
    return "aujourd'hui";
  } else if (diffInDays === 1) {
    return 'hier';
  } else if (diffInDays < 7) {
    return `il y a ${diffInDays} jours`;
  } else if (diffInDays < 30) {
    const weeks = Math.floor(diffInDays / 7);
    return `il y a ${weeks} semaine${weeks > 1 ? 's' : ''}`;
  }

  return date.formatDate(dateToFormat, 'DD/MM/YYYY');
};

export const formatFullDate = (dateString: string): string => {
  return date.formatDate(new Date(dateString), 'DD MMMM YYYY à HH:mm', {
    months: [
      'janvier',
      'février',
      'mars',
      'avril',
      'mai',
      'juin',
      'juillet',
      'août',
      'septembre',
      'octobre',
      'novembre',
      'décembre',
    ],
  });
};

export const formatDateWithoutTime = (dateString: string): string => {
  return date.formatDate(new Date(dateString), 'DD MMMM YYYY', {
    months: [
      'janvier',
      'février',
      'mars',
      'avril',
      'mai',
      'juin',
      'juillet',
      'août',
      'septembre',
      'octobre',
      'novembre',
      'décembre',
    ],
  });
};
