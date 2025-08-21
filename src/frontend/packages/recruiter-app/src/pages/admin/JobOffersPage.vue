<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Offres d'Emploi</h4>
        <p class="text-grey-6 q-mb-none">Gérez les offres d'emploi de votre organisation</p>
      </div>
      <q-btn color="primary" icon="add" label="Nouvelle Offre" @click="showCreateDialog = true" />
    </div>

    <q-card>
      <q-card-section>
        <div class="row q-gutter-md q-mb-md">
          <q-input
            v-model="searchText"
            placeholder="Rechercher..."
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
            style="min-width: 150px"
            emit-value
            map-options
            @update:model-value="onSearch"
          />

          <q-select
            v-model="workModeFilter"
            :options="workModeOptions"
            label="Mode de travail"
            dense
            outlined
            style="min-width: 150px"
            emit-value
            map-options
            @update:model-value="onSearch"
          />

          <q-btn flat icon="refresh" :loading="jobOfferStore.isLoading" @click="refreshData" />
        </div>

        <q-linear-progress v-if="jobOfferStore.isLoading" color="primary" indeterminate />

        <q-table
          v-model:pagination="pagination"
          :rows="jobOfferStore.jobOffers"
          :columns="columns"
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
          <template #body-cell-title="props">
            <q-td :props="props">
              <div>
                <div class="text-weight-medium">{{ props.row.title }}</div>
                <div class="text-caption text-grey-6">{{ props.row.organizationName }}</div>
              </div>
            </q-td>
          </template>

          <template #body-cell-status="props">
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

          <template #body-cell-workMode="props">
            <q-td :props="props">
              <q-chip color="info" text-color="white" size="sm">
                {{ getWorkModeLabel(props.value) }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-contractType="props">
            <q-td :props="props">
              <q-chip color="secondary" text-color="white" size="sm">
                {{ getContractTypeLabel(props.value) }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-salary="props">
            <q-td :props="props">
              <div v-if="props.row.salaryMin || props.row.salaryMax">
                {{
                  formatSalary(props.row.salaryMin, props.row.salaryMax, props.row.salaryCurrency)
                }}
              </div>
              <span v-else class="text-grey-6">Non spécifié</span>
            </q-td>
          </template>

          <template #body-cell-isActive="props">
            <q-td :props="props">
              <q-chip :color="props.value ? 'positive' : 'negative'" text-color="white" size="sm">
                {{ props.value ? 'Actif' : 'Inactif' }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-actions="props">
            <q-td :props="props">
              <q-btn flat round dense icon="more_vert" color="grey-6">
                <q-menu anchor="bottom right" self="top right">
                  <q-list style="min-width: 180px">
                    <q-item v-close-popup clickable @click="editJobOffer(props.row)">
                      <q-item-section avatar>
                        <q-icon name="edit" />
                      </q-item-section>
                      <q-item-section>Modifier</q-item-section>
                    </q-item>

                    <q-item
                      v-if="props.row.status === JobOfferStatus.Draft"
                      v-close-popup
                      clickable
                      @click="publishJobOffer(props.row)"
                    >
                      <q-item-section avatar>
                        <q-icon name="visibility" />
                      </q-item-section>
                      <q-item-section>Publier</q-item-section>
                    </q-item>

                    <q-item
                      v-if="props.row.status === JobOfferStatus.Published"
                      v-close-popup
                      clickable
                      @click="closeJobOffer(props.row)"
                    >
                      <q-item-section avatar>
                        <q-icon name="visibility_off" />
                      </q-item-section>
                      <q-item-section>Fermer</q-item-section>
                    </q-item>

                    <q-separator />

                    <q-item
                      v-close-popup
                      clickable
                      class="text-negative"
                      @click="deleteJobOffer(props.row)"
                    >
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
      </q-card-section>
    </q-card>

    <!-- Dialog de création/modification -->
    <q-dialog v-model="showCreateDialog" persistent>
      <q-card style="min-width: 600px; max-width: 800px">
        <q-card-section class="row items-center">
          <div class="text-h6">
            {{ selectedJobOffer ? "Modifier l'offre" : "Nouvelle offre d'emploi" }}
          </div>
          <q-space />
          <q-btn v-close-popup flat round dense icon="close" />
        </q-card-section>

        <q-card-section>
          <div class="row q-gutter-md">
            <q-input
              v-model="formData.title"
              label="Titre *"
              dense
              outlined
              class="col-12"
              :rules="[(val) => !!val || 'Le titre est requis']"
            />

            <q-input
              v-model="formData.description"
              label="Description *"
              type="textarea"
              dense
              outlined
              class="col-12"
              rows="4"
              :rules="[(val) => !!val || 'La description est requise']"
            />

            <q-input
              v-model="formData.requirements"
              label="Exigences *"
              type="textarea"
              dense
              outlined
              class="col-12"
              rows="3"
              :rules="[(val) => !!val || 'Les exigences sont requises']"
            />

            <q-input
              v-model="formData.location"
              label="Localisation"
              dense
              outlined
              class="col-12 col-sm-6"
            />

            <q-select
              v-model="formData.workMode"
              :options="workModeOptions"
              label="Mode de travail *"
              dense
              outlined
              class="col-12 col-sm-6"
              emit-value
              map-options
              :rules="[
                (val) => (val !== null && val !== undefined) || 'Le mode de travail est requis',
              ]"
            />

            <q-select
              v-model="formData.contractType"
              :options="contractTypeOptions"
              label="Type de contrat *"
              dense
              outlined
              class="col-12 col-sm-6"
              emit-value
              map-options
              :rules="[
                (val) => (val !== null && val !== undefined) || 'Le type de contrat est requis',
              ]"
            />

            <q-input
              v-model.number="formData.salaryMin"
              label="Salaire minimum"
              type="number"
              dense
              outlined
              class="col-12 col-sm-4"
            />

            <q-input
              v-model.number="formData.salaryMax"
              label="Salaire maximum"
              type="number"
              dense
              outlined
              class="col-12 col-sm-4"
            />

            <q-input
              v-model="formData.salaryCurrency"
              label="Devise"
              dense
              outlined
              class="col-12 col-sm-4"
              placeholder="EUR"
            />

            <q-input
              v-model="formData.expiresAt"
              label="Date d'expiration"
              type="datetime-local"
              dense
              outlined
              class="col-12 col-sm-6"
            />
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Annuler" @click="closeDialog" />
          <q-btn color="primary" label="Enregistrer" :loading="saving" @click="saveJobOffer" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useJobOfferStore } from '../../stores/jobOfferStore';
import { JobOfferStatus, WorkMode, ContractType } from '../../enums';
import { jobOfferStatusConfig, workModeLabels, contractTypeLabels } from '../../models/job';
import type {
  JobOffer,
  CreateJobOfferDto,
  UpdateJobOfferDto,
  JobOfferFilterDto,
} from '../../models/job';
import { useDataTable } from 'src/composables/datatable';
import { date } from 'quasar';
import type { QTableColumn } from 'quasar';
import type { Ref } from 'vue';

const jobOfferStore = useJobOfferStore();
const dataTable = useDataTable();

const showCreateDialog = ref(false);
const selectedJobOffer = ref<JobOffer | null>(null);
const saving = ref(false);

const searchText = ref('');
const statusFilter = ref<JobOfferStatus | null>(null);
const workModeFilter = ref<WorkMode | null>(null);

const formData = ref<CreateJobOfferDto>({
  title: '',
  description: '',
  requirements: '',
  location: '',
  workMode: WorkMode.OnSite,
  contractType: ContractType.FullTime,
  salaryMin: 0,
  salaryMax: 0,
  salaryCurrency: 'EUR',
  expiresAt: '',
});

const statusOptions = [
  { label: 'Tous', value: null },
  { label: 'Brouillon', value: JobOfferStatus.Draft },
  { label: 'Publiée', value: JobOfferStatus.Published },
  { label: 'Fermée', value: JobOfferStatus.Closed },
];

const workModeOptions = [
  { label: 'Tous', value: null },
  { label: 'Sur site', value: WorkMode.OnSite },
  { label: 'Hybride', value: WorkMode.Hybrid },
  { label: 'Télétravail complet', value: WorkMode.FullRemote },
];

const contractTypeOptions = [
  { label: 'CDI', value: ContractType.FullTime },
  { label: 'Temps partiel', value: ContractType.PartTime },
  { label: 'CDD', value: ContractType.Contract },
  { label: 'Freelance', value: ContractType.Freelance },
  { label: 'Stage', value: ContractType.Internship },
  { label: 'Temporaire', value: ContractType.Temporary },
];

const pagination = ref({
  sortBy: 'title',
  descending: false,
  page: 1,
  rowsPerPage: 10,
  rowsNumber: 0,
});

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'title',
    field: 'title',
    label: 'Titre',
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
    name: 'workMode',
    field: 'workMode',
    label: 'Mode de travail',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'contractType',
    field: 'contractType',
    label: 'Type de contrat',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'salary',
    field: 'salaryMin',
    label: 'Salaire',
    ...dataTable.defaultConfig.value,
    sortable: false,
    align: 'center',
  },
  {
    name: 'publishedAt',
    field: 'publishedAt',
    label: 'Publié le',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
    format: (val: string) => (val ? date.formatDate(val, 'DD/MM/YYYY') : '-'),
  },
  {
    name: 'isActive',
    field: 'isActive',
    label: 'Actif',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'actions',
    field: '',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);

const currentFilter = computed(
  (): JobOfferFilterDto => ({
    pageNumber: pagination.value.page,
    pageSize: pagination.value.rowsPerPage,
    searchTerms: searchText.value,
    status: statusFilter.value!,
    workMode: workModeFilter.value!,
    sortBy: pagination.value.sortBy,
    sortDirection: pagination.value.descending ? 'Descending' : 'Ascending',
  }),
);

watch(
  () => [jobOfferStore.totalCount, jobOfferStore.currentPage],
  ([totalCount, currentPage]) => {
    pagination.value.rowsNumber = totalCount!;
    pagination.value.page = currentPage!;
  },
);

onMounted(() => {
  loadJobOffers();
});

async function loadJobOffers() {
  await jobOfferStore.fetchPaginatedJobOffers(currentFilter.value);
}

async function onTableRequest(props: any) {
  const { page, rowsPerPage, sortBy, descending } = props.pagination;

  pagination.value.page = page;
  pagination.value.rowsPerPage = rowsPerPage;
  pagination.value.sortBy = sortBy;
  pagination.value.descending = descending;

  await loadJobOffers();
}

function onSearch() {
  pagination.value.page = 1;
  loadJobOffers();
}

function refreshData() {
  loadJobOffers();
}

function editJobOffer(jobOffer: JobOffer) {
  selectedJobOffer.value = jobOffer;
  formData.value = {
    title: jobOffer.title,
    description: jobOffer.description,
    requirements: jobOffer.requirements,
    location: jobOffer.location,
    workMode: jobOffer.workMode,
    contractType: jobOffer.contractType,
    salaryMin: jobOffer.salaryMin,
    salaryMax: jobOffer.salaryMax,
    salaryCurrency: jobOffer.salaryCurrency,
    expiresAt: jobOffer.expiresAt,
  };
  showCreateDialog.value = true;
}

async function saveJobOffer() {
  saving.value = true;
  try {
    if (selectedJobOffer.value) {
      const updateData: UpdateJobOfferDto = { ...formData.value };
      await jobOfferStore.updateJobOffer(selectedJobOffer.value.id, updateData);
    } else {
      await jobOfferStore.createJobOffer(formData.value);
    }
    closeDialog();
    await loadJobOffers();
  } finally {
    saving.value = false;
  }
}

function closeDialog() {
  showCreateDialog.value = false;
  selectedJobOffer.value = null;
  resetForm();
}

function resetForm() {
  formData.value = {
    title: '',
    description: '',
    requirements: '',
    location: '',
    workMode: WorkMode.OnSite,
    contractType: ContractType.FullTime,
    salaryMin: 35000,
    salaryMax: 45000,
    salaryCurrency: 'EUR',
    expiresAt: '',
  };
}

async function publishJobOffer(jobOffer: JobOffer) {
  if (confirm(`Êtes-vous sûr de vouloir publier "${jobOffer.title}" ?`)) {
    await jobOfferStore.publishJobOffer(jobOffer.id);
    await loadJobOffers();
  }
}

async function closeJobOffer(jobOffer: JobOffer) {
  if (confirm(`Êtes-vous sûr de vouloir fermer "${jobOffer.title}" ?`)) {
    await jobOfferStore.closeJobOffer(jobOffer.id);
    await loadJobOffers();
  }
}

async function deleteJobOffer(jobOffer: JobOffer) {
  if (
    confirm(
      `Êtes-vous sûr de vouloir supprimer "${jobOffer.title}" ? Cette action est irréversible.`,
    )
  ) {
    await jobOfferStore.deleteJobOffer(jobOffer.id);
    await loadJobOffers();
  }
}

function getStatusConfig(status: JobOfferStatus) {
  return jobOfferStatusConfig[status] || jobOfferStatusConfig[JobOfferStatus.Draft];
}

function getWorkModeLabel(workMode: WorkMode): string {
  return workModeLabels[workMode] || 'Inconnu';
}

function getContractTypeLabel(contractType: ContractType): string {
  return contractTypeLabels[contractType] || 'Inconnu';
}

function formatSalary(min?: number, max?: number, currency = 'EUR'): string {
  if (min && max) {
    return `${min.toLocaleString()} - ${max.toLocaleString()} ${currency}`;
  } else if (min) {
    return `À partir de ${min.toLocaleString()} ${currency}`;
  } else if (max) {
    return `Jusqu'à ${max.toLocaleString()} ${currency}`;
  }
  return 'Non spécifié';
}
</script>

<style scoped>
.sticky-header {
  height: 500px;
}

.sticky-header :deep(.q-table__top),
.sticky-header :deep(.q-table__bottom),
.sticky-header :deep(thead tr:first-child th) {
  background-color: white;
}

.sticky-header :deep(thead tr th) {
  position: sticky;
  z-index: 1;
}

.sticky-header :deep(thead tr:first-child th) {
  top: 0;
}

.sticky-header :deep(.q-table__bottom) {
  position: sticky;
  bottom: 0;
}
</style>
