import type { QTableColumn } from 'quasar';
import { ref } from 'vue';
import type { Ref } from 'vue';

export interface IDatatableComposable {
  defaultStyle: Ref<{ maxHeight: string }>;
  defaultPagination: Ref<{ rowsPerPage: number; rowsPerPageOptions: number[] }>;
  defaultConfig: Ref<Partial<QTableColumn>>;
  defaultActionsConfig: Ref<Partial<QTableColumn>>;
  frenchLabels: Ref<{
    noData: string;
    noResults: string;
    loading: string;
    rowsPerPage: string;
    of: string;
  }>;
}

export function useDataTable(): IDatatableComposable {
  const defaultStyle = ref({
    maxHeight: '75vh',
  });

  const defaultPagination = ref({
    rowsPerPage: 5,
    rowsPerPageOptions: [5, 10, 25, 50, 100, 200],
  });

  const defaultConfig: Ref<Partial<QTableColumn>> = ref({
    sortable: false,
    align: 'left',
    headerClasses: 'text-uppercase',
  });

  const defaultActionsConfig: Ref<Partial<QTableColumn>> = ref({
    headerStyle: 'width: 100px',
    style: 'width: 100px',
  });

  const frenchLabels = ref({
    noData: 'Aucune donnée disponible',
    noResults: 'Aucun résultat trouvé',
    loading: 'Chargement...',
    rowsPerPage: 'Lignes par page :',
    of: 'sur',
  });

  return { defaultStyle, defaultPagination, defaultConfig, defaultActionsConfig, frenchLabels };
}
