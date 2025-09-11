import { useQuasar } from 'quasar';

export interface DialogOptions {
  title: string;
  message: string;
  persistent?: boolean;
  cancel?: boolean | string;
  ok?:
    | {
        label?: string;
        color?: string;
        push?: boolean;
      }
    | boolean
    | string;
}

export interface IDialogComposable {
  confirm: (options: DialogOptions) => Promise<void>;
  confirmDelete: (itemName: string, itemType?: string) => Promise<void>;
  confirmAction: (
    action: string,
    itemName: string,
    options?: Partial<DialogOptions>,
  ) => Promise<void>;
  confirmToggle: (isActive: boolean, itemName: string, itemType?: string) => Promise<void>;
}

export function useDialog(): IDialogComposable {
  const $q = useQuasar();

  const confirm = (options: DialogOptions): Promise<void> => {
    return new Promise((resolve, reject) => {
      $q.dialog({
        title: options.title,
        message: options.message,
        persistent: options.persistent ?? true,
        cancel: options.cancel ?? true,
        ok: options.ok ?? {
          push: true,
          label: 'Confirmer',
          color: 'primary',
        },
      })
        .onOk(() => resolve())
        .onCancel(() => reject(new Error('Dialog cancelled')))
        .onDismiss(() => reject(new Error('Dialog dismissed')));
    });
  };

  const confirmDelete = (itemName: string, itemType: string = 'élément'): Promise<void> => {
    return confirm({
      title: 'Confirmer la suppression',
      message: `Êtes-vous sûr de vouloir supprimer ${itemType === 'élément' ? "l'" : 'le '}${itemType} "${itemName}" ? Cette action est irréversible.`,
      ok: {
        push: true,
        label: 'Supprimer',
        color: 'negative',
      },
    });
  };

  const confirmAction = (
    action: string,
    itemName: string,
    options?: Partial<DialogOptions>,
  ): Promise<void> => {
    return confirm({
      title: options?.title ?? "Confirmer l'action",
      message: options?.message ?? `Êtes-vous sûr de vouloir ${action} "${itemName}" ?`,
      persistent: options?.persistent ?? true,
      cancel: options?.cancel ?? true,
      ok: options?.ok ?? {
        push: true,
        label: action.charAt(0).toUpperCase() + action.slice(1),
        color: 'primary',
      },
    });
  };

  const confirmToggle = (
    isActive: boolean,
    itemName: string,
    itemType: string = 'élément',
  ): Promise<void> => {
    const action = isActive ? 'désactiver' : 'activer';
    const actionCapitalized = isActive ? 'Désactiver' : 'Activer';

    return confirm({
      title: "Confirmer l'action",
      message: `Êtes-vous sûr de vouloir ${action} ${itemType === 'élément' ? "l'" : 'le '}${itemType} "${itemName}" ?`,
      ok: {
        push: true,
        label: actionCapitalized,
        color: isActive ? 'warning' : 'positive',
      },
    });
  };

  return {
    confirm,
    confirmDelete,
    confirmAction,
    confirmToggle,
  };
}
