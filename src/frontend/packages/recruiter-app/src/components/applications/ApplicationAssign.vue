<template>
  <q-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)">
    <q-card style="min-width: 500px">
      <q-card-section class="row items-center justify-between">
        <div class="text-h6">Assigner des utilisateurs</div>
        <q-btn flat round dense icon="close" @click="$emit('update:modelValue', false)" />
      </q-card-section>

      <q-separator />

      <q-card-section>
        <div v-if="application" class="q-mb-md">
          <div class="text-weight-medium">{{ application.candidateName }}</div>
          <div class="text-caption text-grey-6">{{ application.jobOfferTitle }}</div>
        </div>

        <div class="q-mb-lg">
          <div class="text-h6 q-mb-md">Manager</div>

          <div v-if="application?.assignedManagerName" class="q-mb-md">
            <div class="text-body2 q-mb-sm">Manager actuellement assigné :</div>
            <q-chip color="blue-1" text-color="blue-10" removable @remove="unassignManager">
              <q-avatar color="blue" text-color="white">
                {{ application.assignedManagerName[0] }}
              </q-avatar>
              {{ application.assignedManagerName }}
            </q-chip>
          </div>

          <q-select
            v-model="selectedManager"
            :options="managerOptions"
            label="Assigner un manager"
            dense
            outlined
            clearable
            emit-value
            map-options
            option-value="id"
            option-label="name"
            :loading="loadingManagers"
          >
            <template #option="scope">
              <q-item v-bind="scope.itemProps">
                <q-item-section avatar>
                  <q-avatar color="blue" text-color="white" size="sm">
                    {{ scope.opt.name[0] }}
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ scope.opt.name }}</q-item-label>
                  <q-item-label caption>{{ scope.opt.email }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
          </q-select>

          <q-btn
            v-if="selectedManager && selectedManager !== application?.assignedManagerId"
            color="blue"
            flat
            icon="person_add"
            label="Assigner ce manager"
            class="q-mt-sm"
            :loading="assigningManager"
            @click="assignManager"
          />
        </div>

        <div class="q-mb-lg">
          <div class="text-h6 q-mb-md">Évaluateur technique</div>

          <div v-if="application?.assignedTechnicalEvaluatorName" class="q-mb-md">
            <div class="text-body2 q-mb-sm">Évaluateur technique actuellement assigné :</div>
            <q-chip color="purple-1" text-color="purple-10" removable @remove="unassignEvaluator">
              <q-avatar color="purple" text-color="white">
                {{ application.assignedTechnicalEvaluatorName[0] }}
              </q-avatar>
              {{ application.assignedTechnicalEvaluatorName }}
            </q-chip>
          </div>

          <q-select
            v-model="selectedEvaluator"
            :options="evaluatorOptions"
            label="Assigner un évaluateur technique"
            dense
            outlined
            clearable
            emit-value
            map-options
            option-value="id"
            option-label="name"
            :loading="loadingEvaluators"
          >
            <template #option="scope">
              <q-item v-bind="scope.itemProps">
                <q-item-section avatar>
                  <q-avatar color="purple" text-color="white" size="sm">
                    {{ scope.opt.name[0] }}
                  </q-avatar>
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ scope.opt.name }}</q-item-label>
                  <q-item-label caption>{{ scope.opt.email }}</q-item-label>
                </q-item-section>
              </q-item>
            </template>
          </q-select>

          <q-btn
            v-if="
              selectedEvaluator && selectedEvaluator !== application?.assignedTechnicalEvaluatorId
            "
            color="purple"
            flat
            icon="person_add"
            label="Assigner cet évaluateur"
            class="q-mt-sm"
            :loading="assigningEvaluator"
            @click="assignEvaluator"
          />
        </div>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn flat label="Fermer" @click="$emit('update:modelValue', false)" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useApplicationStore } from 'src/stores/applicationStore';
import { useUserStore } from 'src/stores/userStore';
import { useAuthStore } from 'src/stores/authStore';
import type { ApplicationDto, AssignUserDto } from 'src/models/application';
import type { UserFilterDto } from 'src/models/user';

interface Props {
  modelValue: boolean;
  application: ApplicationDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'updated'): void;
}

interface UserOption {
  id: string;
  name: string;
  email: string;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const applicationStore = useApplicationStore();
const userStore = useUserStore();
const authStore = useAuthStore();

const selectedManager = ref<string | null>(null);
const selectedEvaluator = ref<string | null>(null);
const loadingManagers = ref(false);
const loadingEvaluators = ref(false);
const assigningManager = ref(false);
const assigningEvaluator = ref(false);

const managerOptions = ref<UserOption[]>([]);
const evaluatorOptions = ref<UserOption[]>([]);

const loadManagers = async () => {
  loadingManagers.value = true;
  try {
    // Récupérer les utilisateurs avec les rôles de manager de l'organisation
    const filter: UserFilterDto = {
      role: 'Organization.Manager',
      organizationId: authStore.user?.organizationId ?? '',
      isActive: true,
      pageSize: 100,
    };

    await userStore.fetchPaginatedUsers(filter);

    managerOptions.value = userStore.users.map((user) => ({
      id: user.id,
      name: user.fullName || `${user.firstName} ${user.lastName}`,
      email: user.email,
    }));
  } catch (error) {
    console.error('Error loading managers:', error);
    managerOptions.value = [];
  } finally {
    loadingManagers.value = false;
  }
};

const loadEvaluators = async () => {
  loadingEvaluators.value = true;
  try {
    // Récupérer les utilisateurs avec les rôles d'évaluateur technique de l'organisation
    const filter: UserFilterDto = {
      role: 'Organization.TechnicalEvaluator',
      organizationId: authStore.user?.organizationId ?? '',
      isActive: true,
      pageSize: 100,
    };

    await userStore.fetchPaginatedUsers(filter);

    evaluatorOptions.value = userStore.users.map((user) => ({
      id: user.id,
      name: user.fullName || `${user.firstName} ${user.lastName}`,
      email: user.email,
    }));
  } catch (error) {
    console.error('Error loading evaluators:', error);
    evaluatorOptions.value = [];
  } finally {
    loadingEvaluators.value = false;
  }
};

const assignManager = async () => {
  if (!props.application || !selectedManager.value) return;

  assigningManager.value = true;
  try {
    const assignDto: AssignUserDto = {
      applicationId: props.application.id,
      userId: selectedManager.value,
    };

    const result = await applicationStore.assignUser(assignDto);
    if (result) {
      selectedManager.value = null;
      emit('updated');
    }
  } catch (error) {
    console.error('Error assigning manager:', error);
  } finally {
    assigningManager.value = false;
  }
};

const assignEvaluator = async () => {
  if (!props.application || !selectedEvaluator.value) return;

  assigningEvaluator.value = true;
  try {
    const assignDto: AssignUserDto = {
      applicationId: props.application.id,
      userId: selectedEvaluator.value,
    };

    const result = await applicationStore.assignUser(assignDto);
    if (result) {
      selectedEvaluator.value = null;
      emit('updated');
    }
  } catch (error) {
    console.error('Error assigning evaluator:', error);
  } finally {
    assigningEvaluator.value = false;
  }
};

const unassignManager = async () => {
  if (!props.application || !props.application.assignedManagerId) return;

  try {
    const unassignDto: AssignUserDto = {
      applicationId: props.application.id,
      userId: props.application.assignedManagerId,
    };

    const result = await applicationStore.unassignUser(unassignDto);
    if (result) {
      emit('updated');
    }
  } catch (error) {
    console.error('Error unassigning manager:', error);
  }
};

const unassignEvaluator = async () => {
  if (!props.application || !props.application.assignedTechnicalEvaluatorId) return;

  try {
    const unassignDto: AssignUserDto = {
      applicationId: props.application.id,
      userId: props.application.assignedTechnicalEvaluatorId,
    };

    const result = await applicationStore.unassignUser(unassignDto);
    if (result) {
      emit('updated');
    }
  } catch (error) {
    console.error('Error unassigning evaluator:', error);
  }
};

// Load data when dialog opens
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      loadManagers();
      loadEvaluators();
    }
  },
);

onMounted(() => {
  if (props.modelValue) {
    loadManagers();
    loadEvaluators();
  }
});
</script>
