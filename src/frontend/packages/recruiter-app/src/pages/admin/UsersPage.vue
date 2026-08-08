<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Gestion des Utilisateurs</h4>
        <p class="text-grey-6 q-mb-none">
          Gérez les utilisateurs et leurs permissions de la plateforme
        </p>
      </div>
      <q-btn
        color="primary"
        icon="add"
        label="Nouvel Utilisateur"
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
          class="sticky-header"
          row-key="id"
          binary-state-sort
          @request="onTableRequest"
        >
          <template #body-cell-avatar="props">
            <q-td :props="props">
              <q-avatar size="40px" color="primary" text-color="white">
                <img
                  v-if="props.row.profilePictureUrl"
                  :src="props.row.profilePictureUrl"
                  :alt="`Photo de ${props.row.firstName} ${props.row.lastName}`"
                />
                <span v-else> {{ props.row.firstName[0] }}{{ props.row.lastName[0] }} </span>
              </q-avatar>
            </q-td>
          </template>

          <template #body-cell-name="props">
            <q-td :props="props">
              <div>
                <div class="row items-center no-wrap">
                  <span class="text-weight-medium">
                    {{ props.row.firstName }} {{ props.row.lastName }}
                  </span>
                  <q-chip
                    v-if="authStore.user?.id === props.row.id"
                    size="sm"
                    color="primary"
                    text-color="white"
                    dense
                    class="q-ml-sm"
                  >
                    <q-icon name="person" size="xs" class="q-mr-xs" />
                    Moi
                  </q-chip>
                </div>
                <div class="text-caption text-grey-6">
                  {{ props.row.email }}
                </div>
              </div>
            </q-td>
          </template>

          <template #body-cell-roles="props">
            <q-td :props="props">
              <div v-if="props.row.roles && props.row.roles.length > 0" class="q-gutter-xs">
                <q-chip
                  v-for="(role, index) in props.row.roles.slice(0, 2)"
                  :key="index"
                  size="sm"
                  color="blue-grey-6"
                  text-color="white"
                  dense
                >
                  {{ role }}
                </q-chip>
                <q-chip
                  v-if="props.row.roles.length > 2"
                  size="sm"
                  color="grey-6"
                  text-color="white"
                  dense
                >
                  +{{ props.row.roles.length - 2 }} ...
                </q-chip>
              </div>
              <span v-else class="text-grey-6">Aucun rôle</span>
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
                    <q-item v-close-popup clickable @click="editUser(props.row)">
                      <q-item-section avatar>
                        <q-icon name="edit" color="primary" />
                      </q-item-section>
                      <q-item-section>Modifier</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="manageUserRoles(props.row)">
                      <q-item-section avatar>
                        <q-icon name="admin_panel_settings" color="info" />
                      </q-item-section>
                      <q-item-section>Gérer les rôles</q-item-section>
                    </q-item>

                    <q-item v-close-popup clickable @click="openResetPasswordDialog(props.row)">
                      <q-item-section avatar>
                        <q-icon name="lock_reset" color="warning" />
                      </q-item-section>
                      <q-item-section>Réinitialiser le mot de passe</q-item-section>
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

    <!-- Dialog Create/Edit User -->
    <q-dialog v-model="showCreateDialog" persistent>
      <q-card style="min-width: 600px">
        <q-card-section>
          <div class="text-h6">{{ editingUser ? 'Modifier' : 'Créer' }} un Utilisateur</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="saveUser">
            <div class="row q-gutter-md">
              <q-input
                v-model="userForm.firstName"
                label="Prénom *"
                outlined
                class="col"
                :rules="[
                  (val) => !!val || 'Le prénom est requis',
                  (val) => val.length <= 100 || 'Maximum 100 caractères',
                ]"
              />
              <q-input
                v-model="userForm.lastName"
                label="Nom *"
                outlined
                class="col"
                :rules="[
                  (val) => !!val || 'Le nom est requis',
                  (val) => val.length <= 100 || 'Maximum 100 caractères',
                ]"
              />
            </div>

            <q-input
              v-model="userForm.email"
              label="Email *"
              type="email"
              outlined
              :rules="[
                (val) => !!val || 'L\'email est requis',
                (val) => /.+@.+\..+/.test(val) || 'Email invalide',
              ]"
            />

            <q-input
              v-model="userForm.phoneNumber"
              label="Téléphone"
              outlined
              :rules="[(val) => !val || val.length <= 20 || 'Maximum 20 caractères']"
            />

            <q-select
              v-model="userForm.organizationId"
              :options="organizationOptions"
              label="Organisation *"
              outlined
              emit-value
              map-options
              :rules="[(val) => !!val || 'L\'organisation est requise']"
            />

            <q-input
              v-model="userForm.employeeId"
              label="Matricule *"
              outlined
              :rules="[
                (val) => !!val || 'Le matricule est requis',
                (val) => val.length <= 50 || 'Maximum 50 caractères',
              ]"
            />

            <q-input
              v-model="userForm.department"
              label="Département"
              outlined
              :rules="[(val) => !val || val.length <= 100 || 'Maximum 100 caractères']"
            />

            <q-input
              v-if="!editingUser"
              v-model="userForm.password"
              label="Mot de passe *"
              type="password"
              outlined
              :rules="[(val) => !!val || 'Le mot de passe est requis', ...passwordComplexityRules]"
            />

            <q-toggle
              v-if="editingUser"
              v-model="userForm.isActive"
              label="Utilisateur actif"
              color="primary"
            />

            <div class="row justify-end q-gutter-sm">
              <q-btn flat label="Annuler" @click="closeDialog" />
              <q-btn
                type="submit"
                color="primary"
                label="Enregistrer"
                :loading="userStore.isLoading"
              />
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Dialog Manage User Roles -->
    <q-dialog v-if="selectedUser" v-model="showUserRolesDialog">
      <q-card style="min-width: 600px">
        <q-card-section>
          <div class="text-h6">Rôles de {{ selectedUser.fullName }}</div>
        </q-card-section>

        <q-card-section>
          <q-table
            :rows="userRoleStore.userRoles"
            :columns="userRoleColumns"
            :loading="userRoleStore.isLoading"
            :rows-per-page-options="dataTable.defaultPagination.value.rowsPerPageOptions"
            :style="dataTable.defaultStyle.value"
            :no-data-label="dataTable.frenchLabels.value.noData"
            :no-results-label="dataTable.frenchLabels.value.noResults"
            :loading-label="dataTable.frenchLabels.value.loading"
            :rows-per-page-label="dataTable.frenchLabels.value.rowsPerPage"
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

          <div class="q-mt-md">
            <q-btn
              color="primary"
              icon="add"
              label="Assigner un rôle"
              @click="showAssignRoleDialog = true"
            />
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Fermer" @click="showUserRolesDialog = false" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Dialog Assign Role -->
    <q-dialog v-model="showAssignRoleDialog" persistent>
      <q-card style="min-width: 400px">
        <q-card-section>
          <div class="text-h6">Assigner un rôle</div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="assignRole">
            <q-select
              v-model="assignRoleForm.roleId"
              :options="availableRoles"
              label="Rôle *"
              outlined
              emit-value
              map-options
              :rules="[(val) => !!val || 'Le rôle est requis']"
            />

            <q-input
              v-model="assignRoleForm.expiresAt"
              label="Date d'expiration"
              type="date"
              outlined
              hint="Laisser vide pour un rôle permanent"
            />

            <div class="row justify-end q-gutter-sm">
              <q-btn flat label="Annuler" @click="closeAssignRoleDialog" />
              <q-btn
                type="submit"
                color="primary"
                label="Assigner"
                :loading="userRoleStore.isLoading"
              />
            </div>
          </q-form>
        </q-card-section>
      </q-card>
    </q-dialog>

    <!-- Dialog Reset Password -->
    <q-dialog v-model="showResetPasswordDialog" persistent>
      <q-card style="min-width: 500px">
        <q-card-section>
          <div class="text-h6">Réinitialiser le mot de passe</div>
          <div class="text-caption text-grey-6 q-mt-sm">
            Utilisateur : {{ selectedUserForReset?.firstName }}
            {{ selectedUserForReset?.lastName }} ({{ selectedUserForReset?.email }})
          </div>
        </q-card-section>

        <q-card-section>
          <q-form class="q-gutter-md" @submit="resetUserPassword">
            <q-input
              v-model="resetPasswordForm.newPassword"
              label="Nouveau mot de passe *"
              type="password"
              outlined
              :rules="[(val) => !!val || 'Le mot de passe est requis', ...passwordComplexityRules]"
            />

            <q-input
              v-model="resetPasswordForm.confirmPassword"
              label="Confirmer le mot de passe *"
              type="password"
              outlined
              :rules="[
                (val) => !!val || 'La confirmation est requise',
                (val) =>
                  val === resetPasswordForm.newPassword || 'Les mots de passe ne correspondent pas',
              ]"
            />

            <div class="row justify-end q-gutter-sm q-mt-md">
              <q-btn flat label="Annuler" @click="closeResetPasswordDialog" />
              <q-btn
                type="submit"
                color="warning"
                label="Réinitialiser"
                :loading="resettingPassword"
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
import { ref, onMounted, reactive, computed } from 'vue';
import type { Ref } from 'vue';
import type { QTableColumn } from 'quasar';
import { useUserStore } from '../../stores/userStore';
import { useUserRoleStore } from '../../stores/userRoleStore';
import { useRoleStore } from '../../stores/roleStore';
import { useOrganizationStore } from '../../stores/organizationStore';
import { useAuthStore } from '../../stores/authStore';
import type {
  UserSearchResultDto,
  CreateUserDto,
  UpdateUserDto,
  UserFilterDto,
} from '../../models/user';
import type { UserRoleDto, AssignRoleDto } from '../../models/userRole';
import type { AdminResetPasswordDto } from '../../models/auth';
import { authService } from '../../services/authService';
import { useDataTable } from 'src/composables/datatable';
import { useNotification } from 'src/composables/notification';
import { useDialog } from 'src/composables/dialog';
import { usePasswordComplexityRules } from 'src/composables/passwordRules';

const userStore = useUserStore();
const userRoleStore = useUserRoleStore();
const roleStore = useRoleStore();
const organizationStore = useOrganizationStore();
const authStore = useAuthStore();
const dataTable = useDataTable();
const notification = useNotification();
const dialog = useDialog();
const passwordComplexityRules = usePasswordComplexityRules();

const showCreateDialog = ref(false);
const showUserRolesDialog = ref(false);
const showAssignRoleDialog = ref(false);
const showResetPasswordDialog = ref(false);
const editingUser = ref<UserSearchResultDto | null>(null);
const selectedUser = ref<UserSearchResultDto | null>(null);
const selectedUserForReset = ref<UserSearchResultDto | null>(null);
const resettingPassword = ref(false);
const searchText = ref('');
const statusFilter = ref<boolean | null>(null);

const statusOptions = [
  { label: 'Tous', value: null },
  { label: 'Actif', value: true },
  { label: 'Inactif', value: false },
];

const organizationOptions = computed(() =>
  organizationStore.organizations.map((org) => ({
    label: org.name,
    value: org.id,
  })),
);

const userForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  phoneNumber: '',
  organizationId: '',
  employeeId: '',
  department: '',
  password: '',
  isActive: true,
  emailNotificationsEnabled: true,
  smsNotificationsEnabled: false,
  preferredLanguage: 'fr',
});

const assignRoleForm = reactive({
  roleId: '',
  expiresAt: '',
});

const resetPasswordForm = reactive({
  newPassword: '',
  confirmPassword: '',
});

const columns: Ref<QTableColumn<any>[]> = ref([
  { name: 'avatar', field: 'avatar', label: '', ...dataTable.defaultConfig.value, sortable: false },
  {
    name: 'name',
    field: 'firstName',
    label: 'Nom',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'email',
    field: 'email',
    label: 'Email',
    ...dataTable.defaultConfig.value,
    sortable: true,
  },
  {
    name: 'organization',
    field: 'organizationName',
    label: 'Organisation',
    ...dataTable.defaultConfig.value,
    sortable: true,
    format: (val: string) => val || '-',
  },
  {
    name: 'department',
    field: 'department',
    label: 'Département',
    ...dataTable.defaultConfig.value,
    sortable: true,
    format: (val: string) => val || '-',
  },
  {
    name: 'roles',
    field: 'roles',
    label: 'Rôles',
    ...dataTable.defaultConfig.value,
    sortable: false,
    align: 'left',
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
    name: 'actions',
    field: '',
    label: 'Actions',
    ...dataTable.defaultConfig.value,
    ...dataTable.defaultActionsConfig.value,
  },
]);

const userRoleColumns: Ref<QTableColumn<any>[]> = ref([
  { name: 'roleName', field: 'roleName', label: 'Rôle', ...dataTable.defaultConfig.value },
  {
    name: 'roleDisplayName',
    field: 'roleDisplayName',
    label: "Nom d'affichage",
    ...dataTable.defaultConfig.value,
  },
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
    name: 'expiresAt',
    field: 'expiresAt',
    label: 'Expire le',
    ...dataTable.defaultConfig.value,
    align: 'center',
    format: (val: string) => (val ? new Date(val).toLocaleDateString('fr-FR') : 'Permanent'),
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
  sortBy: 'firstName',
  descending: false,
});

const availableRoles = ref<{ label: string; value: string }[]>([]);

const onTableRequest = async (props: any) => {
  const { page, rowsPerPage, sortBy, descending } = props.pagination;

  pagination.value.page = page;
  pagination.value.rowsPerPage = rowsPerPage;
  pagination.value.sortBy = sortBy;
  pagination.value.descending = descending;

  await fetchUsers();
};

const fetchUsers = async () => {
  const filter: UserFilterDto = {
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

  await userStore.fetchPaginatedUsers(filter);
  pagination.value.rowsNumber = userStore.totalCount;
};

const onSearch = () => {
  pagination.value.page = 1;
  fetchUsers();
};

const refreshData = () => {
  fetchUsers();
};

const resetForm = () => {
  Object.assign(userForm, {
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    organizationId: '',
    employeeId: '',
    department: '',
    password: '',
    isActive: true,
    emailNotificationsEnabled: true,
    smsNotificationsEnabled: false,
    preferredLanguage: 'fr',
  });
  editingUser.value = null;
};

const editUser = (user: UserSearchResultDto) => {
  editingUser.value = user;
  Object.assign(userForm, {
    firstName: user.firstName,
    lastName: user.lastName,
    email: user.email,
    phoneNumber: user.phoneNumber || '',
    organizationId: user.organizationId || '',
    employeeId: user.employeeId || '',
    department: user.department || '',
    password: '', // Never populate password for security
    isActive: user.isActive,
    emailNotificationsEnabled: true,
    smsNotificationsEnabled: false,
    preferredLanguage: 'fr',
  });
  showCreateDialog.value = true;
};

const saveUser = async () => {
  if (editingUser.value) {
    const updateData: UpdateUserDto = {
      firstName: userForm.firstName,
      lastName: userForm.lastName,
      email: userForm.email,
      phoneNumber: userForm.phoneNumber,
      organizationId: userForm.organizationId,
      employeeId: userForm.employeeId,
      department: userForm.department,
      isActive: userForm.isActive,
    };

    await userStore.updateUser(editingUser.value.id, updateData);

    updateUserInList(editingUser.value.id, {
      firstName: updateData.firstName!,
      lastName: updateData.lastName!,
      email: updateData.email!,
      phoneNumber: updateData.phoneNumber!,
      employeeId: updateData.employeeId!,
      department: updateData.department!,
      isActive: updateData.isActive!,
      fullName: `${updateData.firstName} ${updateData.lastName}`,
    });
  } else {
    const createData: CreateUserDto = {
      firstName: userForm.firstName,
      lastName: userForm.lastName,
      email: userForm.email,
      phoneNumber: userForm.phoneNumber,
      organizationId: userForm.organizationId,
      employeeId: userForm.employeeId,
      department: userForm.department,
      password: userForm.password,
      isActive: userForm.isActive,
      emailNotificationsEnabled: userForm.emailNotificationsEnabled || true,
      smsNotificationsEnabled: userForm.smsNotificationsEnabled || false,
      preferredLanguage: userForm.preferredLanguage || 'fr',
      timeZone: 'UTC',
      emailConfirmed: false,
    };

    await userStore.createUser(createData);
    await fetchUsers();
  }

  closeDialog();
};

const toggleUserStatus = async (user: UserSearchResultDto, isActive: boolean) => {
  if (isActive) {
    await userStore.activateUser(user.id);
  } else {
    await userStore.deactivateUser(user.id);
  }

  updateUserInList(user.id, { isActive });
};

const confirmToggleStatus = async (user: UserSearchResultDto) => {
  try {
    await dialog.confirmToggle(user.isActive, user.fullName, 'utilisateur');
    await toggleUserStatus(user, !user.isActive);
  } catch {
    // User cancelled
  }
};

const confirmDelete = async (user: UserSearchResultDto) => {
  try {
    await dialog.confirmDelete(user.fullName, 'utilisateur');
    await deleteUser(user);
  } catch {
    // User cancelled
  }
};

const deleteUser = async (user: UserSearchResultDto) => {
  await userStore.deleteUser(user.id);

  removeUserFromList(user.id);
};

const closeDialog = () => {
  showCreateDialog.value = false;
  resetForm();
};

// User Role Management
const manageUserRoles = async (user: UserSearchResultDto) => {
  selectedUser.value = user;
  await userRoleStore.fetchUserRoles(user.id);
  showUserRolesDialog.value = true;
};

const loadAvailableRoles = async () => {
  await roleStore.fetchAllRoles();
  availableRoles.value = roleStore.roles.map((role) => ({
    label: role.displayName,
    value: role.id,
  }));
};

const updateUserRolesInList = async (userId: string) => {
  const roleNames = await userRoleStore.fetchUserRoleNames(userId);

  const user = userStore.users.find((u) => u.id === userId);
  if (user) {
    user.roles = roleNames;
  }
};

const updateUserInList = (userId: string, updates: Partial<UserSearchResultDto>) => {
  const user = userStore.users.find((u) => u.id === userId);
  if (user) {
    Object.assign(user, updates);
  }
};

const removeUserFromList = (userId: string) => {
  const index = userStore.users.findIndex((u) => u.id === userId);
  if (index !== -1) {
    userStore.users.splice(index, 1);
    userStore.totalCount--;
  }
};

const assignRole = async () => {
  if (!selectedUser.value) return;

  const assignData: AssignRoleDto = {
    userId: selectedUser.value.id,
    roleId: assignRoleForm.roleId,
  };

  if (assignRoleForm.expiresAt) {
    assignData.expiresAt = assignRoleForm.expiresAt;
  } else {
    const nextYear = new Date();
    nextYear.setFullYear(nextYear.getFullYear() + 1);
    assignData.expiresAt = nextYear.toISOString().split('T')[0]!;
  }

  await userRoleStore.assignRoleToUser(assignData);

  await updateUserRolesInList(selectedUser.value.id);

  // Refresh user roles dialog
  await userRoleStore.fetchUserRoles(selectedUser.value.id);
  closeAssignRoleDialog();
};

const toggleUserRoleStatus = async (userRole: UserRoleDto) => {
  try {
    await dialog.confirmToggle(userRole.isActive, userRole.roleDisplayName, 'rôle');
    await userRoleStore.updateUserRoleStatus(userRole.id, !userRole.isActive);

    if (selectedUser.value) {
      await updateUserRolesInList(selectedUser.value.id);
      await userRoleStore.fetchUserRoles(selectedUser.value.id);
    }
  } catch {
    // User cancelled
  }
};

const removeUserRole = async (userRole: UserRoleDto) => {
  try {
    await dialog.confirmAction('retirer le rôle', userRole.roleDisplayName, {
      message: `Êtes-vous sûr de vouloir retirer le rôle "${userRole.roleDisplayName}" à cet utilisateur ?`,
      ok: {
        push: true,
        label: 'Retirer',
        color: 'negative',
      },
    });
    await userRoleStore.removeRoleFromUser(userRole.id);

    if (selectedUser.value) {
      await updateUserRolesInList(selectedUser.value.id);
      await userRoleStore.fetchUserRoles(selectedUser.value.id);
    }
  } catch {
    // User cancelled
  }
};

const resetAssignRoleForm = () => {
  const nextYear = new Date();
  nextYear.setFullYear(nextYear.getFullYear() + 1);
  const defaultExpiresAt = nextYear.toISOString().split('T')[0]; // Format YYYY-MM-DD

  Object.assign(assignRoleForm, {
    roleId: '',
    expiresAt: defaultExpiresAt,
  });
};

const closeAssignRoleDialog = () => {
  showAssignRoleDialog.value = false;
  resetAssignRoleForm();
};

// Reset Password functions
const openResetPasswordDialog = (user: UserSearchResultDto) => {
  selectedUserForReset.value = user;
  resetPasswordForm.newPassword = '';
  resetPasswordForm.confirmPassword = '';
  showResetPasswordDialog.value = true;
};

const closeResetPasswordDialog = () => {
  showResetPasswordDialog.value = false;
  selectedUserForReset.value = null;
  resetPasswordForm.newPassword = '';
  resetPasswordForm.confirmPassword = '';
};

const resetUserPassword = async () => {
  if (!selectedUserForReset.value) return;

  resettingPassword.value = true;
  try {
    const resetData: AdminResetPasswordDto = {
      email: selectedUserForReset.value.email,
      newPassword: resetPasswordForm.newPassword,
      confirmPassword: resetPasswordForm.confirmPassword,
    };

    const result = await authService.adminResetPassword(resetData);

    if (result?.isSuccess) {
      notification.showSuccessNotification(
        result.message || 'Mot de passe réinitialisé avec succès',
      );
      closeResetPasswordDialog();
    } else {
      notification.showErrorNotification(result?.message || 'Erreur lors de la réinitialisation');
    }
  } catch (error: any) {
    console.log(error.message);
    notification.showErrorNotification('Erreur lors de la réinitialisation du mot de passe');
  } finally {
    resettingPassword.value = false;
  }
};

// Lifecycle
onMounted(async () => {
  await loadAvailableRoles();
  await fetchUsers();
  await organizationStore.fetchAllOrganizations();
});
</script>
