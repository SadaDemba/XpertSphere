<template>
  <q-dialog v-model="showDialog" position="top" persistent>
    <q-card style="min-width: 800px; max-width: 90vw; max-height: 80vh">
      <q-card-section class="row items-center no-wrap">
        <div class="text-h6">Aperçu du CV - {{ candidateName }}</div>
        <q-space />
        <q-btn v-close-popup icon="close" flat round dense />
      </q-card-section>

      <q-separator />

      <q-card-section class="q-pa-none" style="height: 600px">
        <div v-if="cvUrl" class="full-height">
          <iframe
            :src="cvUrl"
            class="full-width full-height"
            style="border: none"
            title="CV Preview"
          />
        </div>
        <div v-else class="full-height flex flex-center">
          <div class="text-center">
            <q-icon name="description" size="64px" color="grey-5" />
            <div class="text-h6 text-grey-6 q-mt-md">CV non disponible</div>
          </div>
        </div>
      </q-card-section>

      <q-separator />

      <q-card-actions align="right" class="q-pa-md">
        <q-btn v-close-popup flat label="Fermer" color="grey" />
        <q-btn
          unelevated
          label="Voir en plein écran"
          color="primary"
          icon="fullscreen"
          @click="openFullView"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';

interface Props {
  modelValue: boolean;
  candidateId: string;
  candidateName: string;
  cvUrl?: string | undefined;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  'update:modelValue': [value: boolean];
}>();

const router = useRouter();

const showDialog = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

const openFullView = () => {
  showDialog.value = false;
  router.push(`/candidates/${props.candidateId}/cv`);
};
</script>
