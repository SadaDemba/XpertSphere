<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Candidats</h4>
        <p class="text-grey-6 q-mb-none">Gérez les profils des candidats</p>
      </div>
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
            v-model="activeFilter"
            :options="activeOptions"
            label="Statut"
            dense
            outlined
            style="min-width: 150px"
            emit-value
            map-options
            @update:model-value="onSearch"
          />

          <q-input
            v-model="skillsFilter"
            placeholder="Compétences"
            dense
            outlined
            style="min-width: 200px"
            @update:model-value="onSearch"
          />

          <q-btn flat icon="refresh" :loading="userStore.isLoading" @click="refreshData" />
        </div>

        <q-linear-progress v-if="userStore.isLoading" color="primary" indeterminate />

        <q-table
          v-model:pagination="pagination"
          :rows="userStore.users"
          :columns="columns"
          :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
          :style="dataTable.defaultStyle.value"
          :no-data-label="dataTable.frenchLabels.value.noData"
          :no-results-label="dataTable.frenchLabels.value.noResults"
          :loading-label="dataTable.frenchLabels.value.loading"
          :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
          :loading="userStore.isLoading"
          class="sticky-header"
          row-key="id"
          binary-state-sort
          @request="onTableRequest"
        >
          <template #body-cell-fullName="props">
            <q-td :props="props">
              <div>
                <div class="text-weight-medium">{{ props.row.fullName }}</div>
                <div class="text-caption text-grey-6">{{ props.row.email }}</div>
              </div>
            </q-td>
          </template>

          <template #body-cell-experience="props">
            <q-td :props="props">
              <q-chip color="info" text-color="white" size="sm">
                {{ props.row.experienceDisplay || 'Non spécifié' }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-skills="props">
            <q-td :props="props">
              <div v-if="props.row.skills" class="text-caption">
                {{ truncateSkills(props.row.skills) }}
              </div>
              <span v-else class="text-grey-6">Non spécifiées</span>
            </q-td>
          </template>

          <template #body-cell-isActive="props">
            <q-td :props="props">
              <q-chip :color="props.value ? 'positive' : 'negative'" text-color="white" size="sm">
                {{ props.value ? 'Actif' : 'Inactif' }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-isAvailable="props">
            <q-td :props="props">
              <q-chip :color="props.value ? 'primary' : 'grey'" text-color="white" size="sm">
                {{ props.value ? 'Disponible' : 'Non disponible' }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-actions="props">
            <q-td :props="props">
              <q-btn
                flat
                round
                dense
                icon="visibility"
                color="primary"
                @click="viewCandidateDetail(props.row)"
              >
                <q-tooltip>Voir plus</q-tooltip>
              </q-btn>
            </q-td>
          </template>
        </q-table>

        <!-- Pagination -->
        <div v-if="userStore.totalPages > 1" class="row justify-center q-mt-md">
          <q-pagination
            v-model="pagination.page"
            :max="userStore.totalPages || 1"
            :max-pages="6"
            direction-links
            :boundary-numbers="false"
            @update:model-value="onPageChange"
          />
        </div>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useUserStore } from '../../stores/userStore';
import type { UserSearchResultDto, UserFilterDto } from '../../models/user';
import { useDataTable } from 'src/composables/datatable';
import { date } from 'quasar';
import type { QTableColumn } from 'quasar';
import type { Ref } from 'vue';

const router = useRouter();
const userStore = useUserStore();
const dataTable = useDataTable();

const searchText = ref('');
const activeFilter = ref<boolean | null>(null);
const skillsFilter = ref('');

const activeOptions = [
  { label: 'Tous', value: null },
  { label: 'Actifs', value: true },
  { label: 'Inactifs', value: false },
];

const pagination = ref({
  sortBy: 'fullName',
  descending: false,
  page: 1,
  rowsPerPage: 10,
  rowsNumber: 0,
});

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'fullName',
    field: 'fullName',
    label: 'Candidat',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'experience',
    field: 'experience',
    label: 'Expérience',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'skills',
    field: 'skills',
    label: 'Compétences',
    ...dataTable.defaultConfig.value,
    sortable: false,
    align: 'left',
  },
  {
    name: 'desiredSalary',
    field: 'desiredSalary',
    label: 'Salaire souhaité',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
    format: (val: number) => (val ? `${val.toLocaleString()} €` : 'Non spécifié'),
  },
  {
    name: 'isActive',
    field: 'isActive',
    label: 'Statut',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'isAvailable',
    field: 'isAvailable',
    label: 'Disponibilité',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'createdAt',
    field: 'createdAt',
    label: 'Inscrit le',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
    format: (val: string) => (val ? date.formatDate(val, 'DD/MM/YYYY') : '-'),
  },
  {
    name: 'actions',
    field: '',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);

const currentFilter = computed((): UserFilterDto => {
  const filter: UserFilterDto = {
    pageNumber: pagination.value.page,
    pageSize: pagination.value.rowsPerPage,
    searchTerms: searchText.value,
    isActive: activeFilter.value ?? undefined,
    skills: skillsFilter.value,
    role: 'Candidate',
    sortBy: pagination.value.sortBy,
    sortDirection: pagination.value.descending ? 'Descending' : 'Ascending',
  };

  return filter;
});

watch(
  () => [userStore.totalCount, userStore.currentPage],
  ([totalCount, currentPage]) => {
    if (typeof totalCount === 'number') {
      pagination.value.rowsNumber = totalCount;
    }
    if (typeof currentPage === 'number') {
      pagination.value.page = currentPage;
    }
  },
);

onMounted(() => {
  loadCandidates();
});

async function loadCandidates() {
  try {
    await userStore.fetchPaginatedUsers(currentFilter.value);
  } catch (error) {
    console.error('Error loading candidates:', error);
  }
}

async function onTableRequest(props: any) {
  const { page, rowsPerPage, sortBy, descending } = props.pagination;

  pagination.value.page = page;
  pagination.value.rowsPerPage = rowsPerPage;
  pagination.value.sortBy = sortBy;
  pagination.value.descending = descending;

  await loadCandidates();
}

function onSearch() {
  pagination.value.page = 1;
  loadCandidates();
}

function refreshData() {
  loadCandidates();
}

function onPageChange(newPage: number) {
  pagination.value.page = newPage;
  loadCandidates();
}

function viewCandidateDetail(candidate: UserSearchResultDto) {
  router.push(`/candidates/${candidate.id}`);
}

function truncateSkills(skills: string): string {
  if (!skills) return '';
  const maxLength = 50;
  return skills.length > maxLength ? skills.substring(0, maxLength) + '...' : skills;
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
