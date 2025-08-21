<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Candidatures</h4>
        <p class="text-grey-6 q-mb-none">Gérez les candidatures de votre organisation</p>
      </div>
    </div>

    <q-card>
      <q-card-section>
        <div class="row q-gutter-md q-mb-md">
          <q-input
            v-model="searchText"
            placeholder="Rechercher par candidat ou poste..."
            dense
            outlined
            style="min-width: 300px"
            @update:model-value="onSearch"
          >
            <template #prepend>
              <q-icon name="search" />
            </template>
          </q-input>

          <q-select
            v-model="statusFilter"
            :options="statusOptions"
            label="Statut"
            dense
            outlined
            clearable
            style="min-width: 180px"
            emit-value
            map-options
            @update:model-value="onSearch"
          />

          <q-select
            v-model="activeFilter"
            :options="activeOptions"
            label="État"
            dense
            outlined
            clearable
            style="min-width: 150px"
            emit-value
            map-options
            @update:model-value="onSearch"
          />

          <q-btn flat icon="refresh" :loading="applicationStore.isLoading" @click="refreshData" />
        </div>

        <q-table
          v-model:pagination="pagination"
          :rows="applicationStore.applications"
          :columns="columns"
          :loading="applicationStore.isLoading"
          :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
          :style="dataTable.defaultStyle.value"
          :no-data-label="dataTable.frenchLabels.value.noData"
          :no-results-label="dataTable.frenchLabels.value.noResults"
          :loading-label="dataTable.frenchLabels.value.loading"
          :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
          class="sticky-header"
          row-key="id"
          binary-state-sort
          @request="onTableRequest"
        >
          <template #body-cell-candidateName="props">
            <q-td :props="props">
              <div class="text-weight-medium">{{ props.value }}</div>
              <div class="text-caption text-grey-6">{{ props.row.candidateEmail }}</div>
            </q-td>
          </template>

          <template #body-cell-jobOfferTitle="props">
            <q-td :props="props">
              <div class="text-weight-medium">{{ props.value }}</div>
              <div class="text-caption text-grey-6">{{ props.row.organizationName }}</div>
            </q-td>
          </template>

          <template #body-cell-currentStatus="props">
            <q-td :props="props">
              <q-chip
                :color="getStatusConfig(props.value).color"
                :text-color="getStatusConfig(props.value).textColor"
                :icon="getStatusConfig(props.value).icon"
                size="sm"
              >
                {{ getStatusConfig(props.value).label }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-rating="props">
            <q-td :props="props">
              <q-rating
                v-if="props.value"
                :model-value="props.value"
                :max="5"
                size="sm"
                color="amber"
                readonly
              />
              <span v-else class="text-grey-5">-</span>
            </q-td>
          </template>

          <template #body-cell-appliedAt="props">
            <q-td :props="props">
              {{ formatDate(props.value) }}
            </q-td>
          </template>

          <template #body-cell-assignedManager="props">
            <q-td :props="props">
              <div v-if="props.row.assignedManagerName">
                <q-chip size="sm" color="blue-1" text-color="blue-10">
                  <q-avatar size="20px" color="blue" text-color="white">
                    {{ props.row.assignedManagerName[0] }}
                  </q-avatar>
                  {{ props.row.assignedManagerName }}
                </q-chip>
              </div>
              <span v-else class="text-grey-5">Non assigné</span>
            </q-td>
          </template>

          <template #body-cell-assignedEvaluator="props">
            <q-td :props="props">
              <div v-if="props.row.assignedTechnicalEvaluatorName">
                <q-chip size="sm" color="purple-1" text-color="purple-10">
                  <q-avatar size="20px" color="purple" text-color="white">
                    {{ props.row.assignedTechnicalEvaluatorName[0] }}
                  </q-avatar>
                  {{ props.row.assignedTechnicalEvaluatorName }}
                </q-chip>
              </div>
              <span v-else class="text-grey-5">Non assigné</span>
            </q-td>
          </template>

          <template #body-cell-actions="props">
            <q-td :props="props">
              <q-btn flat round dense icon="more_vert" color="grey-6">
                <q-menu anchor="bottom right" self="top right">
                  <q-list style="min-width: 180px">
                    <q-item v-close-popup clickable @click="viewApplicationDetails(props.row)">
                      <q-item-section avatar>
                        <q-icon name="visibility" color="primary" />
                      </q-item-section>
                      <q-item-section>Voir les détails</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="viewCandidate(props.row)">
                      <q-item-section avatar>
                        <q-icon name="person" color="info" />
                      </q-item-section>
                      <q-item-section>Voir le candidat</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="showUpdateStatusDialog(props.row)">
                      <q-item-section avatar>
                        <q-icon name="update" color="warning" />
                      </q-item-section>
                      <q-item-section>Changer le statut</q-item-section>
                    </q-item>

                    <q-separator />

                    <q-item v-close-popup clickable @click="showAssignUserDialog(props.row)">
                      <q-item-section avatar>
                        <q-icon name="person_add" color="positive" />
                      </q-item-section>
                      <q-item-section>Assigner</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="viewHistory(props.row)">
                      <q-item-section avatar>
                        <q-icon name="history" color="grey" />
                      </q-item-section>
                      <q-item-section>Historique</q-item-section>
                    </q-item>
                  </q-list>
                </q-menu>
              </q-btn>
            </q-td>
          </template>
        </q-table>
      </q-card-section>
    </q-card>

    <!-- Application Details Dialog -->
    <application-details
      v-model="showDetailsDialog"
      :application="selectedApplication"
      @updated="refreshData"
    />

    <!-- Update Status Dialog -->
    <application-status-update
      v-model="showStatusDialog"
      :application="selectedApplication"
      @updated="refreshData"
    />

    <!-- Assign User Dialog -->
    <application-assign
      v-model="showAssignDialog"
      :application="selectedApplication"
      @updated="refreshData"
    />

    <!-- History Dialog -->
    <application-history v-model="showHistoryDialog" :application="selectedApplication" />
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import type { QTableProps } from 'quasar';
import { date } from 'quasar';
import { useApplicationStore } from 'src/stores/applicationStore';
import { useDataTable } from 'src/composables/datatable';
import { applicationStatusConfig } from 'src/models/application';
import type { ApplicationDto, ApplicationFilterDto } from 'src/models/application';
import type { ApplicationStatus } from 'src/enums/ApplicationStatus';
import { statusOptions } from 'src/enums/ApplicationStatus';
import ApplicationDetails from 'src/components/applications/ApplicationDetails.vue';
import ApplicationStatusUpdate from 'src/components/applications/ApplicationStatusUpdate.vue';
import ApplicationAssign from 'src/components/applications/ApplicationAssign.vue';
import ApplicationHistory from 'src/components/applications/ApplicationHistory.vue';

const router = useRouter();
const applicationStore = useApplicationStore();
const dataTable = useDataTable();

const searchText = ref('');
const statusFilter = ref<ApplicationStatus | null>(null);
const activeFilter = ref<boolean | null>(null);
const selectedApplication = ref<ApplicationDto | null>(null);
const showDetailsDialog = ref(false);
const showStatusDialog = ref(false);
const showAssignDialog = ref(false);
const showHistoryDialog = ref(false);

const activeOptions = [
  { label: 'Toutes', value: null },
  { label: 'Actives', value: true },
  { label: 'Inactives', value: false },
];

const pagination = ref({
  page: 1,
  rowsPerPage: 10,
  sortBy: 'appliedAt',
  descending: true,
  rowsNumber: 0,
});

const columns = computed<QTableProps['columns']>(() => [
  {
    name: 'candidateName',
    label: 'Candidat',
    field: 'candidateName',
    align: 'left',
    sortable: true,
  },
  {
    name: 'jobOfferTitle',
    label: 'Poste',
    field: 'jobOfferTitle',
    align: 'left',
    sortable: true,
  },
  {
    name: 'currentStatus',
    label: 'Statut',
    field: 'currentStatus',
    align: 'center',
    sortable: true,
  },
  {
    name: 'rating',
    label: 'Évaluation',
    field: 'rating',
    align: 'center',
    sortable: true,
  },
  {
    name: 'appliedAt',
    label: 'Date de candidature',
    field: 'appliedAt',
    align: 'center',
    sortable: true,
  },
  {
    name: 'assignedManager',
    label: 'Manager',
    field: 'assignedManagerName',
    align: 'center',
    sortable: false,
  },
  {
    name: 'assignedEvaluator',
    label: 'Évaluateur technique',
    field: 'assignedTechnicalEvaluatorName',
    align: 'center',
    sortable: false,
  },
  {
    name: 'actions',
    label: 'Actions',
    field: 'id',
    align: 'center',
    sortable: false,
  },
]);

const getStatusConfig = (status: ApplicationStatus) => {
  return (
    applicationStatusConfig[status] || {
      color: 'grey',
      textColor: 'white',
      icon: 'help',
      label: 'Inconnu',
    }
  );
};

const formatDate = (dateString: string) => {
  return date.formatDate(dateString, 'DD/MM/YYYY HH:mm');
};

const onTableRequest = async (props: any) => {
  const { page, rowsPerPage, sortBy, descending } = props.pagination;

  const filter: ApplicationFilterDto = {
    page,
    pageSize: rowsPerPage,
    search: searchText.value,
    currentStatus: statusFilter.value ?? undefined,
    isActive: activeFilter.value ?? undefined,
    sortBy: sortBy || 'appliedAt',
    sortDesc: descending,
    organizationId: '',
  };

  await applicationStore.fetchPaginatedApplications(filter);

  pagination.value.page = page;
  pagination.value.rowsPerPage = rowsPerPage;
  pagination.value.sortBy = sortBy;
  pagination.value.descending = descending;
  pagination.value.rowsNumber = applicationStore.totalCount;
};

const onSearch = () => {
  pagination.value.page = 1;
  onTableRequest({ pagination: pagination.value });
};

const refreshData = () => {
  onTableRequest({ pagination: pagination.value });
};

const viewApplicationDetails = (application: ApplicationDto) => {
  selectedApplication.value = application;
  showDetailsDialog.value = true;
};

const viewCandidate = (application: ApplicationDto) => {
  router.push(`/candidates/${application.candidateId}`);
};

const showUpdateStatusDialog = (application: ApplicationDto) => {
  selectedApplication.value = application;
  showStatusDialog.value = true;
};

const showAssignUserDialog = (application: ApplicationDto) => {
  selectedApplication.value = application;
  showAssignDialog.value = true;
};

const viewHistory = async (application: ApplicationDto) => {
  selectedApplication.value = application;
  await applicationStore.fetchStatusHistory(application.id);
  showHistoryDialog.value = true;
};

onMounted(() => {
  refreshData();
});
</script>

<style scoped>
.sticky-header {
  height: calc(100vh - 250px);
}
</style>
