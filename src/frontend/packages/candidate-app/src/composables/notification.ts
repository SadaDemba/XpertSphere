import { useQuasar } from 'quasar';

export interface INotificationComposable {
  showSuccessNotification: (message: string) => void;
  showWarningNotification: (message: string) => void;
  showInfoNotification: (message: string) => void;
  showErrorNotification: (message: string) => void;
  showConfirmDialog: (title: string, message: string) => Promise<boolean>;
}

export function useNotification(): INotificationComposable {
  const $q = useQuasar();

  const showSuccessNotification = (message: string): void => {
    showNotification('positive', message);
  };

  const showWarningNotification = (message: string): void => {
    showNotification('warning', message);
  };

  const showErrorNotification = (message: string): void => {
    showNotification('negative', message);
  };

  const showInfoNotification = (message: string): void => {
    showNotification('info', message);
  };

  const showNotification = (type: string, message: string): void => {
    $q.notify({
      type: type,
      position: 'bottom-right',
      message,
      html: true,
    });
  };

  const showConfirmDialog = (title: string, message: string): Promise<boolean> => {
    return new Promise((resolve) => {
      $q.dialog({
        title,
        message,
        cancel: {
          label: 'Annuler',
          color: 'negative',
          flat: true,
        },
        persistent: true,
        ok: {
          label: 'Confirmer',
          color: 'primary',
        },
      })
        .onOk(() => {
          resolve(true);
        })
        .onCancel(() => {
          resolve(false);
        });
    });
  };

  return {
    showSuccessNotification,
    showWarningNotification,
    showInfoNotification,
    showErrorNotification,
    showConfirmDialog,
  };
}
