<template>
  <q-card>
    <q-card-section>
      <div class="text-h6">Assignation de Rôles</div>
      <p class="text-grey-6">Gérez les rôles assignés aux utilisateurs</p>
    </q-card-section>

    <q-card-section>
      <div class="row q-gutter-md q-mb-md">
        <q-select
          v-model="selectedUser"
          :options="userOptions"
          label="Sélectionner un utilisateur"
          outlined
          option-label="name"
          option-value="id"
          use-input
          input-debounce="300"
          style="min-width: 300px"
          @filter="filterUsers"
        >
          <template #no-option>
            <q-item>
              <q-item-section class="text-grey"> Aucun utilisateur trouvé </q-item-section>
            </q-item>
          </template>
        </q-select>

        <q-select
          v-model="selectedRole"
          :options="roleStore.activeRoles"
          label="Sélectionner un rôle"
          outlined
          option-label="name"
          option-value="id"
          style="min-width: 200px"
        />

        <q-input
          v-model="expiryDate"
          label="Date d'expiration (optionnel)"
          outlined
          type="date"
          style="min-width: 200px"
        />

        <q-btn
          color="primary"
          label="Assigner"
          :disable="!selectedUser || !selectedRole"
          :loading="userRoleStore.isLoading"
          @click="assignRole"
        />
      </div>

      <q-separator class="q-my-md" />

      <div v-if="selectedUser">
        <div class="text-subtitle1 q-mb-md">Rôles de {{ selectedUser.name }}</div>

        <q-table
          :rows="userRoleStore.currentUserRoles"
          :columns="userRoleColumns"
          :loading="userRoleStore.isLoading"
          row-key="id"
          flat
          bordered
        >
          <template #body-cell-isActive="props">
            <q-td :props="props">
              <q-chip :color="props.value ? 'positive' : 'negative'" text-color="white" size="sm">
                {{ props.value ? 'Actif' : 'Inactif' }}
              </q-chip>
            </q-td>
          </template>

          <template #body-cell-expiryDate="props">
            <q-td :props="props">
              <span v-if="props.value">
                {{ new Date(props.value).toLocaleDateString('fr-FR') }}
                <q-chip
                  v-if="isExpired(props.value)"
                  color="negative"
                  text-color="white"
                  size="sm"
                  class="q-ml-sm"
                >
                  Expiré
                </q-chip>
              </span>
              <span v-else class="text-grey-6">Permanent</span>
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
                :title="props.row.isActive ? 'Désactiver' : 'Activer'"
                @click="toggleUserRoleStatus(props.row)"
              />
              <q-btn
                flat
                round
                dense
                icon="schedule"
                color="info"
                title="Étendre la durée"
                @click="extendRole(props.row)"
              />
              <q-btn
                flat
                round
                dense
                icon="delete"
                color="negative"
                title="Supprimer le rôle"
                @click="removeRole(props.row)"
              />
            </q-td>
          </template>
        </q-table>
      </div>
    </q-card-section>

    <q-dialog v-model="showExtendDialog" persistent>
      <q-card style="min-width: 400px">
        <q-card-section>
          <div class="text-h6">Étendre la durée du rôle</div>
        </q-card-section>

        <q-card-section>
          <q-input
            v-model="newExpiryDate"
            label="Nouvelle date d'expiration"
            outlined
            type="date"
            :min="today"
          />
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Annuler" @click="showExtendDialog = false" />
          <q-btn
            color="primary"
            label="Étendre"
            :loading="userRoleStore.isLoading"
            @click="confirmExtendRole"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-card>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useRoleStore } from '../../stores/roleStore';
import { useUserRoleStore } from '../../stores/userRoleStore';
import type { RoleDto } from '../../models/role';
import type { AssignRoleDto, UserRoleDto } from '../../models/userRole';
import { useQuasar } from 'quasar';

const $q = useQuasar();
const roleStore = useRoleStore();
const userRoleStore = useUserRoleStore();

const selectedUser = ref<any>(null);
const selectedRole = ref<RoleDto | null>(null);
const expiryDate = ref('');
const userOptions = ref<any[]>([]);
const showExtendDialog = ref(false);
const extendingUserRole = ref<UserRoleDto | null>(null);
const newExpiryDate = ref('');

const today = computed(() => {
  return new Date().toISOString().split('T')[0];
});

const userRoleColumns = [
  {
    name: 'roleName',
    label: 'Rôle',
    field: 'roleName',
    align: 'left' as const,
  },
  {
    name: 'roleDescription',
    label: 'Description',
    field: 'roleDescription',
    align: 'left' as const,
  },
  {
    name: 'isActive',
    label: 'Statut',
    field: 'isActive',
    align: 'center' as const,
  },
  {
    name: 'assignedAt',
    label: 'Assigné le',
    field: 'assignedAt',
    align: 'center' as const,
    format: (val: string) => new Date(val).toLocaleDateString('fr-FR'),
  },
  {
    name: 'expiryDate',
    label: 'Expire le',
    field: 'expiryDate',
    align: 'center' as const,
  },
  {
    name: 'actions',
    label: 'Actions',
    field: '',
    align: 'center' as const,
  },
];

const filterUsers = (val: string, update: (callback: () => void) => void) => {
  update(() => {
    if (val === '') {
      userOptions.value = [];
      return;
    }

    const needle = val.toLowerCase();
    userOptions.value = [
      { id: '1', name: `Utilisateur Demo 1 (${needle})`, email: `user1@${needle}.com` },
      { id: '2', name: `Utilisateur Demo 2 (${needle})`, email: `user2@${needle}.com` },
      { id: '3', name: `Utilisateur Demo 3 (${needle})`, email: `user3@${needle}.com` },
    ];
  });
};

const assignRole = async () => {
  if (!selectedUser.value || !selectedRole.value) return;

  try {
    await userRoleStore.assignRoleToUser({
      userId: selectedUser.value.id,
      roleId: selectedRole.value.id,
      expiryDate: expiryDate.value || undefined,
    } as AssignRoleDto);

    $q.notify({
      type: 'positive',
      message: 'Rôle assigné avec succès',
    });

    await userRoleStore.fetchUserRoles(selectedUser.value.id);
    selectedRole.value = null;
    expiryDate.value = '';
  } catch (error) {
    $q.notify({
      type: 'negative',
      message: "Erreur lors de l'assignation du rôle",
    });
  }
};

const toggleUserRoleStatus = async (userRole: UserRoleDto) => {
  try {
    await userRoleStore.updateUserRoleStatus(userRole.id, !userRole.isActive);
    $q.notify({
      type: 'positive',
      message: 'Statut mis à jour avec succès',
    });
  } catch (error) {
    $q.notify({
      type: 'negative',
      message: 'Erreur lors de la modification du statut',
    });
  }
};

const extendRole = (userRole: UserRoleDto) => {
  extendingUserRole.value = userRole;
  newExpiryDate.value = userRole.expiresAt || '';
  showExtendDialog.value = true;
};

const confirmExtendRole = async () => {
  if (!extendingUserRole.value) return;

  try {
    await userRoleStore.extendUserRole(extendingUserRole.value.id, newExpiryDate.value);
    $q.notify({
      type: 'positive',
      message: 'Durée du rôle étendue avec succès',
    });
    showExtendDialog.value = false;
    extendingUserRole.value = null;
    newExpiryDate.value = '';
  } catch (error) {
    $q.notify({
      type: 'negative',
      message: "Erreur lors de l'extension du rôle",
    });
  }
};

const removeRole = async (userRole: UserRoleDto) => {
  $q.dialog({
    title: 'Confirmer la suppression',
    message: `Êtes-vous sûr de vouloir retirer le rôle "${userRole.roleName}" à cet utilisateur ?`,
    cancel: true,
    persistent: true,
  }).onOk(async () => {
    try {
      await userRoleStore.removeRoleFromUser(userRole.id);
      $q.notify({
        type: 'positive',
        message: 'Rôle retiré avec succès',
      });
    } catch (error) {
      $q.notify({
        type: 'negative',
        message: 'Erreur lors de la suppression du rôle',
      });
    }
  });
};

const isExpired = (expiryDate: string) => {
  return new Date(expiryDate) < new Date();
};

onMounted(async () => {
  await roleStore.fetchAllRoles();
});

watch(selectedUser, async (newUser) => {
  if (newUser) {
    await userRoleStore.fetchUserRoles(newUser.id);
  }
});
</script>
