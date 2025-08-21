<template>
  <q-form class="q-gutter-md" @submit="onSubmit">
    <q-input
      v-model="form.name"
      label="Nom technique *"
      outlined
      :rules="[
        (val) => !!val || 'Le nom technique est requis',
        (val) => val.length <= 100 || 'Maximum 100 caractères',
      ]"
      :readonly="isEditing"
      hint="Nom unique du rôle (ex: ADMIN, USER_MANAGER)"
    />

    <q-input
      v-model="form.displayName"
      label="Nom d'affichage *"
      outlined
      :rules="[
        (val) => !!val || 'Le nom d\'affichage est requis',
        (val) => val.length <= 150 || 'Maximum 150 caractères',
      ]"
      hint="Nom affiché dans l'interface (ex: Administrateur, Gestionnaire d'utilisateurs)"
    />

    <q-input
      v-model="form.description"
      label="Description"
      type="textarea"
      outlined
      rows="4"
      :rules="[(val) => !val || val.length <= 500 || 'Maximum 500 caractères']"
      hint="Description optionnelle du rôle et de ses permissions"
    />

    <q-toggle v-if="isEditing" v-model="form.isActive" label="Rôle actif" color="primary" />

    <div class="row justify-end q-gutter-sm q-mt-lg">
      <q-btn flat label="Annuler" @click="$emit('cancel')" />
      <q-btn
        type="submit"
        color="primary"
        :label="isEditing ? 'Mettre à jour' : 'Créer'"
        :loading="loading"
      />
    </div>
  </q-form>
</template>

<script setup lang="ts">
import { reactive, watch, computed } from 'vue';
import type { CreateRoleDto, UpdateRoleDto, RoleDto } from '../../models/role';

interface Props {
  role?: RoleDto | null;
  loading?: boolean;
}

interface Emits {
  (e: 'submit', data: CreateRoleDto | UpdateRoleDto): void;
  (e: 'cancel'): void;
}

const props = withDefaults(defineProps<Props>(), {
  role: null,
  loading: false,
});

const emit = defineEmits<Emits>();

const form = reactive({
  name: '',
  displayName: '',
  description: '',
  isActive: true,
});

const isEditing = computed(() => !!props.role);

const resetForm = () => {
  Object.assign(form, {
    name: '',
    displayName: '',
    description: '',
    isActive: true,
  });
};

const loadRole = (role: RoleDto) => {
  Object.assign(form, {
    name: role.name,
    displayName: role.displayName,
    description: role.description || '',
    isActive: role.isActive,
  });
};

const onSubmit = () => {
  if (isEditing.value) {
    const updateData: UpdateRoleDto = {
      displayName: form.displayName,
      isActive: form.isActive,
    };

    if (form.description) {
      updateData.description = form.description;
    }

    emit('submit', updateData);
  } else {
    const createData: CreateRoleDto = {
      name: form.name,
      displayName: form.displayName,
    };

    if (form.description) {
      createData.description = form.description;
    }

    emit('submit', createData);
  }
};

watch(
  () => props.role,
  (newRole) => {
    if (newRole) {
      loadRole(newRole);
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

defineExpose({
  resetForm,
});
</script>
