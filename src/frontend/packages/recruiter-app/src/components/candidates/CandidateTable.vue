<template>
  <q-table
    :rows="candidates"
    :columns="columns"
    :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
    :style="dataTable.defaultStyle.value"
    :no-data-label="dataTable.frenchLabels.value.noData"
    :no-results-label="dataTable.frenchLabels.value.noResults"
    :loading-label="dataTable.frenchLabels.value.loading"
    :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
    class="sticky-header candidate-table"
    row-key="id"
    flat
    bordered
    binary-state-sort
    role="table"
    aria-label="Table des candidats"
  >
    <template #body-cell-name="props">
      <q-td :props="props">
        <div class="row items-center">
          <q-avatar size="32px" color="primary" text-color="white" class="q-mr-sm">
            {{ props.row.firstName[0] }}{{ props.row.lastName[0] }}
          </q-avatar>
          <div>
            <button
              class="name-link"
              :aria-label="`Voir le profil de ${props.row.firstName} ${props.row.lastName}`"
              @click="$emit('viewProfile', props.row)"
            >
              {{ props.row.firstName }} {{ props.row.lastName }}
            </button>
            <div class="text-caption text-grey-6">{{ props.row.email }}</div>
          </div>
        </div>
      </q-td>
    </template>

    <template #body-cell-contact="props">
      <q-td :props="props">
        <div class="contact-info">
          <div class="text-body2">{{ props.row.phone }}</div>
          <div class="text-caption text-grey-6">{{ props.row.location }}</div>
        </div>
      </q-td>
    </template>

    <template #body-cell-skills="props">
      <q-td :props="props">
        <div class="skills-container">
          <q-chip
            v-for="skill in props.row.skills.slice(0, 2)"
            :key="skill"
            dense
            outline
            color="primary"
            size="sm"
            class="q-mr-xs q-mb-xs"
          >
            {{ skill }}
          </q-chip>
          <q-chip
            v-if="props.row.skills.length > 2"
            dense
            outline
            color="grey-6"
            size="sm"
            :label="`+${props.row.skills.length - 2}`"
          />
        </div>
      </q-td>
    </template>

    <template #body-cell-status="props">
      <q-td :props="props">
        <q-chip
          :color="getStatusConfig(props.row.status).color"
          :text-color="getStatusConfig(props.row.status).textColor"
          dense
          :aria-label="`Statut: ${getStatusConfig(props.row.status).label}`"
        >
          <q-icon :name="getStatusConfig(props.row.status).icon" size="14px" class="q-mr-xs" />
          {{ getStatusConfig(props.row.status).label }}
        </q-chip>
      </q-td>
    </template>

    <template #body-cell-lastActivity="props">
      <q-td :props="props">
        {{ formatDate(props.row.lastActivity) }}
      </q-td>
    </template>

    <template #body-cell-createdAt="props">
      <q-td :props="props">
        {{ formatDate(props.row.createdAt) }}
      </q-td>
    </template>

    <template #body-cell-actions="props">
      <q-td :props="props">
        <q-btn
          flat
          dense
          round
          icon="more_vert"
          :aria-label="`Actions pour ${props.row.firstName} ${props.row.lastName}`"
        >
          <q-menu>
            <q-list style="min-width: 120px">
              <q-item clickable @click="$emit('viewProfile', props.row)">
                <q-item-section avatar>
                  <q-icon name="person" />
                </q-item-section>
                <q-item-section>Voir profil</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('edit', props.row)">
                <q-item-section avatar>
                  <q-icon name="edit" />
                </q-item-section>
                <q-item-section>Modifier</q-item-section>
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
import type { StatusConfig, TableColumn } from 'src/models/configs';
import { useDataTable } from 'src/composables/datatable';

const dataTable = useDataTable();

type CandidateStatus = 'active' | 'inactive' | 'blacklisted';

interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  position: string;
  experience: string;
  location: string;
  skills: string[];
  status: CandidateStatus;
  createdAt: string;
  updatedAt: string;
  lastActivity: string;
}

interface Props {
  candidates: Candidate[];
}

interface Emits {
  (e: 'edit', candidate: Candidate): void;
  (e: 'delete', candidate: Candidate): void;
  (e: 'viewProfile', candidate: Candidate): void;
  (e: 'viewApplications', candidate: Candidate): void;
}

defineProps<Props>();
defineEmits<Emits>();

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'name',
    field: 'firstName',
    label: 'Candidat',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'position',
    field: 'position',
    label: 'Poste recherché',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'experience',
    field: 'experience',
    label: 'Expérience',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  { name: 'contact', field: 'phone', label: 'Contact', ...dataTable.defaultConfig.value },
  { name: 'skills', field: 'skills', label: 'Compétences', ...dataTable.defaultConfig.value },
  {
    name: 'status',
    field: 'status',
    label: 'Statut',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'lastActivity',
    field: 'lastActivity',
    label: 'Dernière activité',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'createdAt',
    field: 'createdAt',
    label: 'Ajouté le',
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

function getStatusConfig(status: string): StatusConfig {
  switch (status) {
    case 'active':
      return {
        label: 'Actif',
        color: 'positive',
        textColor: 'white',
        icon: 'check_circle',
      };
    case 'inactive':
      return {
        label: 'Inactif',
        color: 'grey-4',
        textColor: 'grey-8',
        icon: 'radio_button_unchecked',
      };

    default:
      return {
        label: 'Liste noire',
        color: 'negative',
        textColor: 'white',
        icon: 'block',
      };
  }
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}
</script>

<style scoped>
.candidate-table {
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

.contact-info {
  line-height: 1.2;
}

.skills-container {
  max-width: 200px;
  overflow: hidden;
}

@media (max-width: 768px) {
  .candidate-table :deep(.q-table__container) {
    font-size: 12px;
  }

  .candidate-table :deep(.q-td),
  .candidate-table :deep(.q-th) {
    padding: 4px 8px;
  }

  .skills-container {
    max-width: 120px;
  }
}
</style>
