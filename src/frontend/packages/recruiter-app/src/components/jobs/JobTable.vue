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
        <q-select
          :model-value="props.row.status"
          :options="statusOptions"
          dense
          borderless
          behavior="menu"
          class="status-select"
          emit-value
          map-options
          :aria-label="`Modifier le statut de l'offre ${props.row.title}`"
          @update:model-value="
            (newStatus: JobOfferStatus) => handleStatusChange(props.row, newStatus)
          "
        >
          <template #selected>
            <q-chip
              :color="jobOfferStatusConfig[props.row.status as JobOfferStatus].color"
              :text-color="jobOfferStatusConfig[props.row.status as JobOfferStatus].textColor"
              dense
            >
              <q-icon
                :name="jobOfferStatusConfig[props.row.status as JobOfferStatus].icon"
                size="14px"
                class="q-mr-xs"
              />
              {{ jobOfferStatusConfig[props.row.status as JobOfferStatus].label }}
            </q-chip>
          </template>
          <template #option="scope">
            <q-item v-bind="scope.itemProps">
              <q-item-section avatar>
                <q-icon :name="jobOfferStatusConfig[scope.opt.value as JobOfferStatus]?.icon" />
              </q-item-section>
              <q-item-section>
                <q-item-label>{{ scope.opt.label }}</q-item-label>
              </q-item-section>
            </q-item>
          </template>
        </q-select>
      </q-td>
    </template>

    <template #body-cell-applications="props">
      <q-td :props="props">
        <q-chip
          :clickable="props.row.applicationsCount > 0"
          :color="props.row.applicationsCount > 0 ? 'primary' : 'grey-4'"
          :text-color="props.row.applicationsCount > 0 ? 'white' : 'grey-8'"
          icon="assignment"
          :label="props.row.applicationsCount"
          :aria-label="`Voir les ${props.row.applicationsCount} candidatures pour ${props.row.title}`"
          @click="props.row.applicationsCount > 0 ? $emit('viewApplications', props.row) : null"
        >
          <q-tooltip v-if="props.row.applicationsCount > 0">
            Voir les {{ props.row.applicationsCount }} candidatures
          </q-tooltip>
          <q-tooltip v-else> Aucune candidature </q-tooltip>
        </q-chip>
      </q-td>
    </template>

    <template #body-cell-workMode="props">
      <q-td :props="props">{{ getWorkModeLabel(props.row.workMode) }} </q-td>
    </template>

    <template #body-cell-contractType="props">
      <q-td :props="props">
        {{ getContractTypeLabel(props.row.contractType) }}
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
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref } from 'vue';
import type { Ref } from 'vue';
import type { QTableColumn } from 'quasar';
import type { JobOffer } from '../../models';
import { jobOfferStatusConfig, workModeLabels, contractTypeLabels } from '../../models';
import { JobOfferStatus, WorkMode, ContractType } from '../../enums';
import { useDataTable } from 'src/composables/datatable';
import { useDialog } from '../../composables/dialog';

const dataTable = useDataTable();
const dialog = useDialog();

interface Props {
  jobs: JobOffer[];
}

interface Emits {
  (e: 'edit', job: JobOffer): void;
  (e: 'delete', job: JobOffer): void;
  (e: 'duplicate', job: JobOffer): void;
  (e: 'viewApplications', job: JobOffer): void;
  (e: 'updateStatus', job: JobOffer, status: JobOfferStatus): void;
}

defineProps<Props>();
const emit = defineEmits<Emits>();

async function handleStatusChange(job: JobOffer, newStatus: JobOfferStatus) {
  if (newStatus === JobOfferStatus.Published) {
    await dialog.confirmAction('publier', job.title, {
      title: 'Confirmer la publication',
      message: `Êtes-vous sûr de vouloir publier l'offre "${job.title}" ?`,
      ok: { label: 'Publier', color: 'positive' },
    });
  } else if (newStatus === JobOfferStatus.Draft) {
    await dialog.confirmAction('remettre en brouillon', job.title, {
      title: 'Confirmer le changement',
      message: `Êtes-vous sûr de vouloir remettre l'offre "${job.title}" en brouillon ?`,
      ok: { label: 'Confirmer', color: 'warning' },
    });
  } else if (newStatus === JobOfferStatus.Closed) {
    await dialog.confirmAction('fermer', job.title, {
      title: 'Confirmer la fermeture',
      message: `Êtes-vous sûr de vouloir fermer l'offre "${job.title}" ?`,
      ok: { label: 'Fermer', color: 'negative' },
    });
  }

  // Si l'utilisateur a confirmé, on émet l'événement
  emit('updateStatus', job, newStatus);
}

function getWorkModeLabel(workMode: WorkMode): string {
  return workModeLabels[workMode] || '';
}

function getContractTypeLabel(contractType: ContractType): string {
  return contractTypeLabels[contractType] || '';
}

const statusOptions = [
  { label: 'Brouillon', value: JobOfferStatus.Draft },
  { label: 'Publiée', value: JobOfferStatus.Published },
  { label: 'Fermée', value: JobOfferStatus.Closed },
];

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'title',
    field: 'title',
    label: 'Titre',
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
  {
    name: 'workMode',
    field: 'workMode',
    label: 'Mode de travail',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'contractType',
    field: 'contractType',
    label: 'Type de contrat',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'status',
    field: 'status',
    label: 'Statut',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'createdByUserName',
    field: 'createdByUserName',
    label: 'Créé par',
    ...dataTable.defaultConfig.value,
    sortable: true,
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
