<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Organisations</h4>
        <p class="text-grey-6 q-mb-none">Gérez les organisations de la plateforme</p>
      </div>
      <q-btn
        color="primary"
        icon="add"
        label="Nouvelle Organisation"
        @click="showCreateDialog = true"
      />
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

          <q-btn flat icon="refresh" :loading="organizationStore.isLoading" @click="refreshData" />
        </div>

        <q-table
          v-model:pagination="pagination"
          :rows="organizationStore.organizations"
          :columns="columns"
          :loading="organizationStore.isLoading"
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
                    <q-item v-close-popup clickable @click="editOrganization(props.row)">
                      <q-item-section avatar>
                        <q-icon name="edit" color="primary" />
                      </q-item-section>
                      <q-item-section>Modifier</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="confirmToggleStatus(props.row)">
                      <q-item-section avatar>
                        <q-icon
                          :name="props.row.isActive ? 'pause' : 'play_arrow'"
                          :color="props.row.isActive ? 'warning' : 'positive'"
                        />
                      </q-item-section>
                      <q-item-section>
                        {{ props.row.isActive ? 'Désactiver' : 'Activer' }}
                      </q-item-section>
                    </q-item>

                    <q-separator />

                    <q-item v-close-popup clickable @click="confirmDelete(props.row)">
                      <q-item-section avatar>
                        <q-icon name="delete" color="negative" />
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

    <q-dialog v-model="showCreateDialog" persistent>
      <q-card style="min-width: 500px">
        <q-card-section>
          <div class="text-h6">
            {{ editingOrganization ? 'Modifier' : 'Créer' }} une Organisation
          </div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="saveOrganization">
            <q-input
              v-model="organizationForm.name"
              label="Nom *"
              outlined
              :rules="[
                (val) => !!val || 'Le nom est requis',
                (val) => val.length <= 150 || 'Maximum 150 caractères',
              ]"
            />

            <q-input
              v-model="organizationForm.code"
              label="Code *"
              outlined
              :rules="[
                (val) => !!val || 'Le code est requis',
                (val) => val.length <= 20 || 'Maximum 20 caractères',
              ]"
              hint="Code unique de l'organisation (ex: ACME, CORP)"
            />

            <q-input
              v-model="organizationForm.description"
              label="Description"
              type="textarea"
              outlined
              rows="3"
              :rules="[(val) => !val || val.length <= 500 || 'Maximum 500 caractères']"
              hint="Description optionnelle de l'organisation"
            />

            <div class="row q-gutter-md">
              <q-input
                v-model="organizationForm.industry"
                label="Secteur d'activité"
                outlined
                class="col"
                :rules="[(val) => !val || val.length <= 100 || 'Maximum 100 caractères']"
              />
              <q-select
                v-model="organizationForm.size"
                :options="sizeOptions"
                label="Taille *"
                outlined
                emit-value
                map-options
                class="col"
                :rules="[(val) => !!val || 'La taille est requise']"
              />
            </div>

            <div class="row q-gutter-md">
              <q-input v-model="organizationForm.website" label="Site Web" outlined class="col" />
              <q-input
                v-model="organizationForm.contactEmail"
                label="Email de contact"
                type="email"
                outlined
                class="col"
              />
            </div>

            <q-input v-model="organizationForm.contactPhone" label="Téléphone" outlined />

            <q-input v-model="organizationForm.address.street" label="Rue" outlined />

            <q-input
              v-model="organizationForm.address.complement"
              label="Complément d'adresse"
              outlined
            />

            <div class="row q-gutter-md">
              <q-input v-model="organizationForm.address.city" label="Ville" outlined class="col" />
              <q-input
                v-model="organizationForm.address.postalCode"
                label="Code Postal"
                outlined
                class="col"
              />
            </div>

            <div class="row q-gutter-md">
              <q-input
                v-model="organizationForm.address.region"
                label="Région"
                outlined
                class="col"
              />
              <q-input
                v-model="organizationForm.address.country"
                label="Pays"
                outlined
                class="col"
              />
            </div>

            <q-toggle
              v-if="editingOrganization"
              v-model="organizationForm.isActive"
              label="Organisation active"
              color="primary"
            />

            <div class="row justify-end q-gutter-sm">
              <q-btn flat label="Annuler" @click="closeDialog" />
              <q-btn
                type="submit"
                color="primary"
                label="Enregistrer"
                :loading="organizationStore.isLoading"
              />
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, onMounted, reactive } from 'vue';
import type { Ref } from 'vue';
import type { QTableColumn } from 'quasar';
import { useOrganizationStore } from '../../stores/organizationStore';
import type {
  OrganizationDto,
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationFilterDto,
} from '../../models/organization';
import { OrganizationSize } from '../../enums';
import { organizationSizeOptions } from '../../enums/OrganizationSize';
import { useQuasar } from 'quasar';
import { useNotification } from '../../composables/notification';
import { useDataTable } from 'src/composables/datatable';

const $q = useQuasar();
const notification = useNotification();
const organizationStore = useOrganizationStore();
const dataTable = useDataTable();

const showCreateDialog = ref(false);
const editingOrganization = ref<OrganizationDto | null>(null);
const searchText = ref('');
const statusFilter = ref<boolean | null>(null);

const statusOptions = [
  { label: 'Tous', value: null },
  { label: 'Actif', value: true },
  { label: 'Inactif', value: false },
];

const sizeOptions = organizationSizeOptions;

const organizationForm = reactive({
  name: '',
  code: '',
  description: '',
  industry: '',
  size: OrganizationSize.Small,
  website: '',
  contactEmail: '',
  contactPhone: '',
  address: {
    street: '',
    complement: '',
    city: '',
    postalCode: '',
    region: '',
    country: '',
  },
  isActive: true,
});

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'name',
    field: 'name',
    label: 'Nom',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'code',
    field: 'code',
    label: 'Code',
    ...dataTable.defaultConfig.value,
    sortable: true,
    style: 'font-family: monospace; font-size: 0.875em;',
  },
  {
    name: 'industry',
    field: 'industry',
    label: 'Secteur',
    ...dataTable.defaultConfig.value,
    sortable: true,
    format: (val: string) => val || '-',
  },
  {
    name: 'size',
    field: 'size',
    label: 'Taille',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
  },
  {
    name: 'usersCount',
    field: 'usersCount',
    label: 'Utilisateurs',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
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
    name: 'createdAt',
    field: 'createdAt',
    label: 'Créé le',
    ...dataTable.defaultConfig.value,
    sortable: true,
    align: 'center',
    format: (val: string) => new Date(val).toLocaleDateString('fr-FR'),
  },
  {
    name: 'actions',
    field: '',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);

const pagination = ref({
  page: 1,
  rowsPerPage: dataTable.defaultPagination.value.rowsPerPage,
  rowsNumber: 0,
  sortBy: 'name',
  descending: false,
});

const onTableRequest = async (props: any) => {
  const { page, rowsPerPage, sortBy, descending } = props.pagination;

  pagination.value.page = page;
  pagination.value.rowsPerPage = rowsPerPage;
  pagination.value.sortBy = sortBy;
  pagination.value.descending = descending;

  await fetchOrganizations();
};

const fetchOrganizations = async () => {
  const filter: OrganizationFilterDto = {
    pageNumber: pagination.value.page,
    pageSize: pagination.value.rowsPerPage,
    sortBy: pagination.value.sortBy,
    sortDirection: pagination.value.descending ? 'Descending' : 'Ascending',
  };

  if (searchText.value) {
    filter.searchTerms = searchText.value;
  }

  if (statusFilter.value !== null && statusFilter.value !== undefined) {
    filter.isActive = statusFilter.value;
  }

  await organizationStore.fetchPaginatedOrganizations(filter);
  pagination.value.rowsNumber = organizationStore.totalCount;
};

const onSearch = () => {
  pagination.value.page = 1;
  fetchOrganizations();
};

const refreshData = () => {
  fetchOrganizations();
};

const resetForm = () => {
  Object.assign(organizationForm, {
    name: '',
    code: '',
    description: '',
    industry: '',
    size: OrganizationSize.Small,
    website: '',
    contactEmail: '',
    contactPhone: '',
    address: {
      street: '',
      complement: '',
      city: '',
      postalCode: '',
      region: '',
      country: '',
    },
    isActive: true,
  });
  editingOrganization.value = null;
};

const editOrganization = (organization: OrganizationDto) => {
  editingOrganization.value = organization;
  Object.assign(organizationForm, {
    name: organization.name,
    code: organization.code,
    description: organization.description || '',
    industry: organization.industry || '',
    size: organization.size,
    website: organization.website || '',
    contactEmail: organization.contactEmail || '',
    contactPhone: organization.contactPhone || '',
    address: {
      street: organization.address?.streetName || '',
      complement: organization.address?.addressLine2 || '',
      city: organization.address?.city || '',
      postalCode: organization.address?.postalCode || '',
      region: organization.address?.region || '',
      country: organization.address?.country || '',
    },
    isActive: organization.isActive,
  });
  showCreateDialog.value = true;
};

const saveOrganization = async () => {
  try {
    if (editingOrganization.value) {
      const updateData: UpdateOrganizationDto = {
        name: organizationForm.name,
        code: organizationForm.code,
        description: organizationForm.description,
        industry: organizationForm.industry,
        size: organizationForm.size,
        website: organizationForm.website,
        contactEmail: organizationForm.contactEmail,
        contactPhone: organizationForm.contactPhone,
        address: {
          streetName: organizationForm.address.street,
          addressLine2: organizationForm.address.complement,
          city: organizationForm.address.city,
          postalCode: organizationForm.address.postalCode,
          region: organizationForm.address.region,
          country: organizationForm.address.country,
        },
        isActive: organizationForm.isActive,
      };

      await organizationStore.updateOrganization(editingOrganization.value.id, updateData);
      notification.showSuccessNotification('Organisation mise à jour avec succès');
    } else {
      const createData: CreateOrganizationDto = {
        name: organizationForm.name,
        code: organizationForm.code,
        description: organizationForm.description,
        industry: organizationForm.industry,
        size: organizationForm.size,
        website: organizationForm.website,
        contactEmail: organizationForm.contactEmail,
        contactPhone: organizationForm.contactPhone,
        address: {
          streetName: organizationForm.address.street,
          addressLine2: organizationForm.address.complement,
          city: organizationForm.address.city,
          postalCode: organizationForm.address.postalCode,
          region: organizationForm.address.region,
          country: organizationForm.address.country,
        },
      };

      await organizationStore.createOrganization(createData);
      notification.showSuccessNotification('Organisation créée avec succès');
    }

    closeDialog();
    await fetchOrganizations();
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification("Erreur lors de l'enregistrement");
  }
};

const toggleOrganizationStatus = async (organization: OrganizationDto, isActive: boolean) => {
  try {
    const updateData: UpdateOrganizationDto = {
      isActive: isActive,
    };

    await organizationStore.updateOrganization(organization.id, updateData);
    notification.showSuccessNotification(
      isActive ? 'Organisation activée avec succès' : 'Organisation désactivée avec succès',
    );
    await fetchOrganizations();
  } catch (error) {
    console.log(error);
    notification.showErrorNotification('Erreur lors de la modification du statut');
  }
};

const confirmToggleStatus = (organization: OrganizationDto) => {
  const action = organization.isActive ? 'désactiver' : 'activer';
  const actionCapitalized = organization.isActive ? 'Désactiver' : 'Activer';

  $q.dialog({
    title: "Confirmer l'action",
    message: `Êtes-vous sûr de vouloir ${action} l'organisation "${organization.name}" ?`,
    cancel: true,
    ok: {
      push: true,
      label: actionCapitalized,
      color: organization.isActive ? 'warning' : 'positive',
    },
    persistent: true,
  }).onOk(() => {
    toggleOrganizationStatus(organization, !organization.isActive);
  });
};

const confirmDelete = (organization: OrganizationDto) => {
  $q.dialog({
    title: 'Confirmer la suppression',
    message: `Êtes-vous sûr de vouloir supprimer l'organisation "${organization.name}" ? Cette action est irréversible.`,
    cancel: true,
    ok: {
      push: true,
      label: 'Supprimer',
      color: 'negative',
    },
    persistent: true,
  }).onOk(() => {
    deleteOrganization(organization);
  });
};

const deleteOrganization = async (organization: OrganizationDto) => {
  try {
    await organizationStore.deleteOrganization(organization.id);
    notification.showSuccessNotification('Organisation supprimée avec succès');
    await fetchOrganizations();
  } catch (error) {
    console.log(error);
    notification.showErrorNotification('Erreur lors de la suppression');
  }
};

const closeDialog = () => {
  showCreateDialog.value = false;
  resetForm();
};

onMounted(() => {
  fetchOrganizations();
});
</script>
