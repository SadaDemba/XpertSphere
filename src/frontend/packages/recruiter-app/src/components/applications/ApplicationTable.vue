<template>
  <q-table
    :rows="applications"
    :columns="columns"
    :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
    :style="dataTable.defaultStyle.value"
    :no-data-label="dataTable.frenchLabels.value.noData"
    :no-results-label="dataTable.frenchLabels.value.noResults"
    :loading-label="dataTable.frenchLabels.value.loading"
    :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
    class="sticky-header application-table"
    row-key="id"
    flat
    bordered
    binary-state-sort
    role="table"
    aria-label="Table des candidatures"
  >
    <template #body-cell-candidate="props">
      <q-td :props="props">
        <div class="row items-center">
          <q-avatar size="32px" color="primary" text-color="white" class="q-mr-sm">
            {{
              props.row.candidateName
                .split(' ')
                .map((n: any[]) => n[0])
                .join('')
            }}
          </q-avatar>
          <div>
            <button
              class="name-link"
              :aria-label="`Voir la candidature de ${props.row.candidateName}`"
              @click="$emit('view', props.row)"
            >
              {{ props.row.candidateName }}
            </button>
            <div class="text-caption text-grey-6">{{ props.row.candidateEmail }}</div>
          </div>
        </div>
      </q-td>
    </template>

    <template #body-cell-job="props">
      <q-td :props="props">
        <div class="job-info">
          <div class="text-body2 text-weight-medium">{{ props.row.jobTitle }}</div>
          <div class="text-caption text-grey-6">
            {{ applicationSourceLabels[props.row.source as keyof typeof applicationSourceLabels] }}
          </div>
        </div>
      </q-td>
    </template>

    <template #body-cell-status="props">
      <q-td :props="props">
        <q-chip
          :color="applicationStatusConfig[props.row.status as ApplicationStatus].color"
          :text-color="applicationStatusConfig[props.row.status as ApplicationStatus].textColor"
          dense
          :aria-label="`Statut: ${applicationStatusConfig[props.row.status as ApplicationStatus].label}`"
        >
          <q-icon
            :name="applicationStatusConfig[props.row.status as ApplicationStatus].icon"
            size="14px"
            class="q-mr-xs"
          />
          {{ applicationStatusConfig[props.row.status as ApplicationStatus].label }}
        </q-chip>
      </q-td>
    </template>

    <template #body-cell-rating="props">
      <q-td :props="props">
        <q-rating
          v-if="props.row.rating"
          :model-value="props.row.rating"
          size="16px"
          color="amber"
          readonly
          :aria-label="`Note: ${props.row.rating} étoiles`"
        />
        <span v-else class="text-grey-5">-</span>
      </q-td>
    </template>

    <template #body-cell-appliedAt="props">
      <q-td :props="props">
        {{ formatDate(props.row.appliedAt) }}
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
          size="sm"
          color="grey-7"
          :aria-label="`Actions pour ${props.row.candidateName}`"
        >
          <q-menu>
            <q-list style="min-width: 150px">
              <q-item clickable @click="$emit('view', props.row)">
                <q-item-section avatar>
                  <q-icon name="visibility" />
                </q-item-section>
                <q-item-section>Voir détails</q-item-section>
              </q-item>
              <q-item clickable @click="viewResume()">
                <q-item-section avatar>
                  <q-icon name="description" />
                </q-item-section>
                <q-item-section>Voir le CV</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('scheduleInterview', props.row)">
                <q-item-section avatar>
                  <q-icon name="event" />
                </q-item-section>
                <q-item-section>Planifier entretien</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('sendEmail', props.row)">
                <q-item-section avatar>
                  <q-icon name="email" />
                </q-item-section>
                <q-item-section>Envoyer email</q-item-section>
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
import type { Application } from '../../models';
import { applicationStatusConfig, applicationSourceLabels } from '../../models';
import { formatDate } from '../../helpers';
import type { ApplicationStatus } from 'src/enums';
import { useDataTable } from 'src/composables/datatable';

const dataTable = useDataTable();

interface Props {
  applications: Application[];
}

interface Emits {
  (e: 'view', application: Application): void;
  (e: 'scheduleInterview', application: Application): void;
  (e: 'sendEmail', application: Application): void;
  (e: 'delete', application: Application): void;
}

defineProps<Props>();
defineEmits<Emits>();

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'candidate',
    field: 'candidateName',
    label: 'Candidat',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'job',
    field: 'jobTitle',
    label: 'Offre / Source',
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
    name: 'rating',
    field: 'rating',
    label: 'Note',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'appliedAt',
    field: 'appliedAt',
    label: 'Date de candidature',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'updatedAt',
    field: 'updatedAt',
    label: 'Dernière mise à jour',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'actions',
    field: '',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);

function viewResume() {
  window.open('/resume-viewer', '_blank');
}
</script>

<style scoped>
.application-table {
  background: white;
}

.name-link {
  background: none;
  border: none;
  color: var(--q-primary);
  font: inherit;
  text-align: left;
  cursor: pointer;
  padding: 0;
  margin: 0;
  text-decoration: none;
  font-weight: 500;
}

.name-link:hover {
  text-decoration: underline;
}

.name-link:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
  border-radius: 4px;
}

.job-info {
  line-height: 1.2;
}

@media (max-width: 768px) {
  .application-table :deep(.q-table__container) {
    font-size: 12px;
  }

  .application-table :deep(.q-td),
  .application-table :deep(.q-th) {
    padding: 4px 6px;
  }
}
</style>
