<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Rôles</h4>
        <p class="text-grey-6 q-mb-none">Gérez les rôles et permissions de la plateforme</p>
      </div>
      <q-btn color="primary" icon="add" label="Nouveau Rôle" @click="showCreateDialog = true" />
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

          <q-btn flat icon="refresh" :loading="roleStore.isLoading" @click="refreshData" />
        </div>

        <q-linear-progress v-if="roleStore.isLoading" color="primary" indeterminate />

        <q-table
          v-model:pagination="pagination"
          :rows="roleStore.roles"
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
                    <q-item v-close-popup clickable @click="editRole(props.row)">
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

                    <q-item
                      v-close-popup
                      clickable
                      :disable="!canDeleteRole(props.row)"
                      @click="confirmDelete(props.row)"
                    >
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
          <div class="text-h6">{{ editingRole ? 'Modifier' : 'Créer' }} un Rôle</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="saveRole">
            <q-input
              v-model="roleForm.name"
              label="Nom technique *"
              outlined
              :rules="[
                (val) => !!val || 'Le nom technique est requis',
                (val) => val.length <= 100 || 'Maximum 100 caractères',
              ]"
              hint="Nom unique du rôle (ex: ADMIN, USER_MANAGER)"
            />

            <q-input
              v-model="roleForm.displayName"
              label="Nom d'affichage *"
              outlined
              :rules="[
                (val) => !!val || 'Le nom d\'affichage est requis',
                (val) => val.length <= 150 || 'Maximum 150 caractères',
              ]"
              hint="Nom affiché dans l'interface (ex: Administrateur, Gestionnaire d'utilisateurs)"
            />

            <q-input
              v-model="roleForm.description"
              label="Description"
              type="textarea"
              outlined
              rows="3"
              :rules="[(val) => !val || val.length <= 500 || 'Maximum 500 caractères']"
              hint="Description optionnelle du rôle et de ses permissions"
            />

            <q-toggle
              v-if="editingRole"
              v-model="roleForm.isActive"
              label="Rôle actif"
              color="primary"
            />

            <div class="row justify-end q-gutter-sm">
              <q-btn flat label="Annuler" @click="closeDialog" />
              <q-btn
                type="submit"
                color="primary"
                label="Enregistrer"
                :loading="roleStore.isLoading"
              />
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <q-dialog v-if="selectedRole" v-model="showUserRolesDialog">
      <q-card style="min-width: 600px">
        <q-card-section>
          <div class="text-h6">Utilisateurs avec le rôle "{{ selectedRole.name }}"</div>
        </q-card-section>

        <q-card-section>
          <q-table
            :rows="userRoleStore.roleUsers"
            :columns="userRoleColumns"
            :loading="userRoleStore.isLoading"
            :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
            :style="dataTable.defaultStyle.value"
            class="sticky-header"
            row-key="id"
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
                <q-btn
                  flat
                  round
                  dense
                  :icon="props.row.isActive ? 'pause' : 'play_arrow'"
                  :color="props.row.isActive ? 'warning' : 'positive'"
                  @click="toggleUserRoleStatus(props.row)"
                />
                <q-btn
                  flat
                  round
                  dense
                  icon="delete"
                  color="negative"
                  @click="removeUserRole(props.row)"
                />
              </q-td>
            </template>
          </q-table>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Fermer" @click="showUserRolesDialog = false" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, onMounted, reactive } from 'vue';
import type { Ref } from 'vue';
import type { QTableColumn } from 'quasar';
import { useRoleStore } from '../../stores/roleStore';
import { useUserRoleStore } from '../../stores/userRoleStore';
import type { RoleDto, CreateRoleDto, UpdateRoleDto, RoleFilterDto } from '../../models/role';
import type { UserRoleDto } from '../../models/userRole';
import { useQuasar } from 'quasar';
import { useDataTable } from 'src/composables/datatable';
import { useNotification } from 'src/composables/notification';

const $q = useQuasar();
const roleStore = useRoleStore();
const userRoleStore = useUserRoleStore();
const dataTable = useDataTable();
const notification = useNotification();

const showCreateDialog = ref(false);
const showUserRolesDialog = ref(false);
const editingRole = ref<RoleDto | null>(null);
const selectedRole = ref<RoleDto | null>(null);
const searchText = ref('');
const statusFilter = ref<boolean | null>(null);

const statusOptions = [
  { label: 'Tous', value: null },
  { label: 'Actif', value: true },
  { label: 'Inactif', value: false },
];

const roleForm = reactive({
  name: '',
  displayName: '',
  description: '',
  isActive: true,
});

const columns: Ref<QTableColumn<any>[]> = ref([
  {
    name: 'name',
    field: 'name',
    label: 'Nom technique',
    ...dataTable.defaultConfig.value,
    sortable: true,
    style: 'font-family: monospace; font-size: 0.875em;',
  },
  {
    name: 'displayName',
    field: 'displayName',
    label: "Nom d'affichage",
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'description',
    field: 'description',
    label: 'Description',
    ...dataTable.defaultConfig.value,
    sortable: true,
    format: (val: string) => val || '-',
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

const userRoleColumns: Ref<QTableColumn<any>[]> = ref([
  { name: 'userName', field: 'userName', label: 'Utilisateur', ...dataTable.defaultConfig.value },
  { name: 'userEmail', field: 'userEmail', label: 'Email', ...dataTable.defaultConfig.value },
  {
    name: 'isActive',
    field: 'isActive',
    label: 'Statut',
    ...dataTable.defaultConfig.value,
    align: 'center',
  },
  {
    name: 'assignedAt',
    field: 'assignedAt',
    label: 'Assigné le',
    ...dataTable.defaultConfig.value,
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

  await fetchRoles();
};

const fetchRoles = async () => {
  const filter: RoleFilterDto = {
    pageNumber: pagination.value.page,
    pageSize: pagination.value.rowsPerPage,
    sortBy: pagination.value.sortBy,
    sortDirection: pagination.value.descending ? 'Descending' : 'Ascending',
  };

  if (searchText.value) {
    filter.searchTerms = searchText.value;
  }

  if (statusFilter.value !== null) {
    filter.isActive = statusFilter.value;
  }

  await roleStore.fetchPaginatedRoles(filter);
  pagination.value.rowsNumber = roleStore.totalCount;
};

const onSearch = () => {
  pagination.value.page = 1;
  fetchRoles();
};

const refreshData = () => {
  fetchRoles();
};

const resetForm = () => {
  Object.assign(roleForm, {
    name: '',
    displayName: '',
    description: '',
    isActive: true,
  });
  editingRole.value = null;
};

const editRole = (role: RoleDto) => {
  editingRole.value = role;
  Object.assign(roleForm, {
    name: role.name,
    displayName: role.displayName,
    description: role.description || '',
    isActive: role.isActive,
  });
  showCreateDialog.value = true;
};

const saveRole = async () => {
  try {
    if (editingRole.value) {
      const updateData: UpdateRoleDto = {
        displayName: roleForm.displayName,
        isActive: roleForm.isActive,
      };

      if (roleForm.description) {
        updateData.description = roleForm.description;
      }

      await roleStore.updateRole(editingRole.value.id, updateData);
      notification.showSuccessNotification('Rôle mis à jour avec succès');
    } else {
      const createData: CreateRoleDto = {
        name: roleForm.name,
        displayName: roleForm.displayName,
      };

      if (roleForm.description) {
        createData.description = roleForm.description;
      }

      await roleStore.createRole(createData);
      notification.showSuccessNotification('Rôle créé avec succès');
    }

    closeDialog();
    await fetchRoles();
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification("Erreur lors de l'enregistrement");
  }
};

const toggleRoleStatus = async (role: RoleDto, isActive: boolean) => {
  try {
    if (isActive) {
      await roleStore.activateRole(role.id);
      notification.showSuccessNotification('Rôle activé avec succès');
    } else {
      await roleStore.deactivateRole(role.id);
      notification.showSuccessNotification('Rôle désactivé avec succès');
    }
    await fetchRoles();
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification('Erreur lors de la modification du statut');
  }
};

const confirmToggleStatus = (role: RoleDto) => {
  const action = role.isActive ? 'désactiver' : 'activer';
  const actionCapitalized = role.isActive ? 'Désactiver' : 'Activer';

  $q.dialog({
    title: "Confirmer l'action",
    message: `Êtes-vous sûr de vouloir ${action} le rôle "${role.displayName}" ?`,
    cancel: true,
    ok: {
      push: true,
      label: actionCapitalized,
      color: role.isActive ? 'warning' : 'positive',
    },
    persistent: true,
  }).onOk(() => {
    toggleRoleStatus(role, !role.isActive);
  });
};

const confirmDelete = (role: RoleDto) => {
  $q.dialog({
    title: 'Confirmer la suppression',
    message: `Êtes-vous sûr de vouloir supprimer le rôle "${role.displayName}" ? Cette action est irréversible.`,
    cancel: true,
    ok: {
      push: true,
      label: 'Supprimer',
      color: 'negative',
    },
    persistent: true,
  }).onOk(() => {
    deleteRole(role);
  });
};

const deleteRole = async (role: RoleDto) => {
  try {
    await roleStore.deleteRole(role.id);
    notification.showSuccessNotification('Rôle supprimé avec succès');
    await fetchRoles();
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification('Erreur lors de la suppression');
  }
};

const canDeleteRole = (role: RoleDto) => {
  return !['Admin', 'SuperAdmin', 'Platform'].includes(role.name);
};

const toggleUserRoleStatus = async (userRole: UserRoleDto) => {
  try {
    await userRoleStore.updateUserRoleStatus(userRole.id, !userRole.isActive);
    notification.showSuccessNotification('Statut mis à jour avec succès');
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification('Erreur lors de la modification du statut');
  }
};

const removeUserRole = async (userRole: UserRoleDto) => {
  try {
    await userRoleStore.removeRoleFromUser(userRole.id);
    notification.showSuccessNotification('Rôle retiré avec succès');
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification('Erreur lors de la suppression du rôle');
  }
};

const closeDialog = () => {
  showCreateDialog.value = false;
  resetForm();
};

onMounted(() => {
  fetchRoles();
});
</script>
