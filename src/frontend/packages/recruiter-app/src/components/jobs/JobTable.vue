<template>
  <q-table
    :rows="jobs"
    :columns="columns"
    :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
    :style="dataTable.defaultStyle.value"
    :no-data-label="dataTable.frenchLabels.value.noData"
    :no-results-label="dataTable.frenchLabels.value.noResults"
    :loading-label="dataTable.frenchLabels.value.loading"
    :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
    class="sticky-header job-table"
    row-key="id"
    flat
    bordered
    binary-state-sort
    role="table"
    aria-label="Table des offres d'emploi"
  >
    <template #body-cell-title="props">
      <q-td :props="props">
        <button
          class="title-link"
          :aria-label="`Modifier l'offre ${props.row.title}`"
          @click="$emit('edit', props.row)"
        >
          {{ props.row.title }}
        </button>
      </q-td>
    </template>

    <template #body-cell-status="props">
      <q-td :props="props">
        <q-chip
          :color="jobOfferStatusConfig[props.row.status as JobOfferStatus].color"
          :text-color="jobOfferStatusConfig[props.row.status as JobOfferStatus].textColor"
          dense
          :aria-label="`Statut: ${jobOfferStatusConfig[props.row.status as JobOfferStatus].label}`"
        >
          <q-icon
            :name="jobOfferStatusConfig[props.row.status as JobOfferStatus].icon"
            size="14px"
            class="q-mr-xs"
          />
          {{ jobOfferStatusConfig[props.row.status as JobOfferStatus].label }}
        </q-chip>
      </q-td>
    </template>

    <template #body-cell-applications="props">
      <q-td :props="props">
        <q-btn
          flat
          dense
          :label="`${props.row.applications}`"
          icon="assignment"
          color="primary"
          :aria-label="`Voir les ${props.row.applications} candidatures pour ${props.row.title}`"
          @click="$emit('viewApplications', props.row)"
        />
      </q-td>
    </template>

    <template #body-cell-createdAt="props">
      <q-td :props="props">
        {{ formatDate(props.row.createdAt) }}
      </q-td>
    </template>

    <template #body-cell-updatedAt="props">
      <q-td :props="props">
        {{ formatDate(props.row.updatedAt) }}
      </q-td>
    </template>

    <template #body-cell-actions="props">
      <q-td :props="props">
        <q-btn
          flat
          dense
          round
          icon="more_vert"
          :aria-label="`Actions pour l'offre ${props.row.title}`"
        >
          <q-menu>
            <q-list style="min-width: 100px">
              <q-item clickable @click="$emit('edit', props.row)">
                <q-item-section avatar>
                  <q-icon name="edit" />
                </q-item-section>
                <q-item-section>Modifier</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('duplicate', props.row)">
                <q-item-section avatar>
                  <q-icon name="content_copy" />
                </q-item-section>
                <q-item-section>Dupliquer</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('viewApplications', props.row)">
                <q-item-section avatar>
                  <q-icon name="assignment" />
                </q-item-section>
                <q-item-section>Candidatures</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable class="text-negative" @click="$emit('delete', props.row)">
                <q-item-section avatar>
                  <q-icon name="delete" />
                </q-item-section>
                <q-item-section>Supprimer</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </q-td>
    </template>
  </q-table>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { Ref } from 'vue';
import type { QTableColumn } from 'quasar';
import type { JobOffer } from '../../models';
import { jobOfferStatusConfig } from '../../models';
import type { JobOfferStatus } from '../../enums';
import { formatDate } from '../../helpers';
import { useDataTable } from 'src/composables/datatable';

const dataTable = useDataTable();

interface Props {
  jobs: JobOffer[];
}

interface Emits {
  (e: 'edit', job: JobOffer): void;
  (e: 'delete', job: JobOffer): void;
  (e: 'duplicate', job: JobOffer): void;
  (e: 'viewApplications', job: JobOffer): void;
}

defineProps<Props>();
defineEmits<Emits>();

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'title',
    field: 'title',
    label: 'Titre',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'department',
    field: 'department',
    label: 'Département',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'location',
    field: 'location',
    label: 'Localisation',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  { name: 'type', field: 'type', label: 'Type', ...dataTable.defaultConfig.value, sortable: true },
  {
    name: 'status',
    field: 'status',
    label: 'Statut',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'applications',
    field: 'applications',
    label: 'Candidatures',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'createdAt',
    field: 'createdAt',
    label: 'Créée le',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'updatedAt',
    field: 'updatedAt',
    label: 'Modifiée le',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'actions',
    field: 'actions',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);
</script>

<style scoped>
.job-table {
  background: white;
}

.title-link {
  background: none;
  border: none;
  color: var(--q-primary);
  font: inherit;
  text-align: left;
  cursor: pointer;
  padding: 0;
  margin: 0;
  text-decoration: none;
}

.title-link:hover {
  text-decoration: underline;
}

.title-link:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
  border-radius: 4px;
}

@media (max-width: 768px) {
  .job-table :deep(.q-table__container) {
    font-size: 12px;
  }

  .job-table :deep(.q-td),
  .job-table :deep(.q-th) {
    padding: 4px 8px;
  }
}
</style>
