<template>
  <q-dialog :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)">
    <q-card style="min-width: 400px">
      <q-card-section class="row items-center justify-between">
        <div class="text-h6">Modifier le statut</div>
        <q-btn flat round dense icon="close" @click="$emit('update:modelValue', false)" />
      </q-card-section>

      <q-separator />

      <q-form @submit="onSubmit" @reset="onReset">
        <q-card-section>
          <div v-if="application" class="q-mb-md">
            <div class="text-weight-medium">{{ application.candidateName }}</div>
            <div class="text-caption text-grey-6">{{ application.jobOfferTitle }}</div>

            <div class="q-mt-sm">
              <div class="text-caption">Statut actuel :</div>
              <q-chip
                :color="getStatusConfig(application.currentStatus).color"
                :text-color="getStatusConfig(application.currentStatus).textColor"
                :icon="getStatusConfig(application.currentStatus).icon"
                size="sm"
              >
                {{ getStatusConfig(application.currentStatus).label }}
              </q-chip>
            </div>
          </div>

          <div class="q-mb-md">
            <q-select
              v-model="form.status"
              :options="statusOptions"
              label="Nouveau statut *"
              dense
              outlined
              emit-value
              map-options
              :rules="[(val) => (val !== null && val !== undefined) || 'Le statut est requis']"
            />
          </div>

          <div class="q-mb-md">
            <q-input
              v-model="form.comment"
              label="Commentaire *"
              type="textarea"
              outlined
              rows="3"
              counter
              maxlength="1000"
              :rules="[
                (val) => !!val || 'Le commentaire est requis',
                (val) =>
                  val.length <= 1000 || 'Le commentaire ne peut pas dépasser 1000 caractères',
              ]"
            />
          </div>

          <div class="q-mb-md">
            <div class="text-body2 q-mb-sm">Évaluation (optionnel)</div>
            <q-rating v-model="form.rating" :max="5" size="md" color="amber" />
            <div class="text-caption text-grey-6">
              {{ form.rating ? `${form.rating}/5` : 'Aucune évaluation' }}
            </div>
          </div>
        </q-card-section>

        <q-card-actions align="right">
          <q-btn flat label="Annuler" type="reset" />
          <q-btn
            color="primary"
            label="Mettre à jour"
            type="submit"
            :loading="loading"
            :disable="!isFormValid"
          />
        </q-card-actions>
      </q-form>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useApplicationStore } from 'src/stores/applicationStore';
import { applicationStatusConfig } from 'src/models/application';
import type { ApplicationDto, UpdateApplicationStatusDto } from 'src/models/application';
import type { ApplicationStatus } from 'src/enums/ApplicationStatus';
import { statusOptions } from 'src/enums/ApplicationStatus';

interface Props {
  modelValue: boolean;
  application: ApplicationDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'updated'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const applicationStore = useApplicationStore();
const loading = ref(false);

const form = ref({
  status: null as ApplicationStatus | null,
  comment: '',
  rating: 0,
});

const isFormValid = computed(() => {
  return (
    form.value.status !== null &&
    form.value.status !== undefined &&
    form.value.comment.trim().length > 0
  );
});

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

const onSubmit = async () => {
  if (!props.application || !isFormValid.value) return;

  loading.value = true;

  try {
    const updateDto: UpdateApplicationStatusDto = {
      status: form.value.status!,
      comment: form.value.comment.trim(),
      rating: form.value.rating || 0,
    };

    const result = await applicationStore.updateApplicationStatus(props.application.id, updateDto);

    if (result) {
      emit('updated');
      emit('update:modelValue', false);
      onReset();
    }
  } catch (error) {
    console.error('Error updating application status:', error);
  } finally {
    loading.value = false;
  }
};

const onReset = () => {
  form.value = {
    status: null,
    comment: '',
    rating: 0,
  };
};

// Reset form when dialog opens
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue) {
      onReset();
    }
  },
);
</script>
