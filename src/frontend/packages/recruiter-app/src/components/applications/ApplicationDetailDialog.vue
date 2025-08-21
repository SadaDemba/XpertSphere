<template>
  <q-dialog v-model="isOpen" persistent max-width="1000px">
    <q-card class="application-detail-dialog">
      <q-card-section class="row items-center">
        <div class="text-h6">Candidature de {{ application?.candidateName }}</div>
        <q-space />
        <q-btn
          flat
          round
          dense
          icon="close"
          aria-label="Fermer la boîte de dialogue"
          @click="closeDialog"
        />
      </q-card-section>

      <q-separator />

      <q-card-section v-if="application" class="q-pa-md">
        <div class="row q-gutter-lg">
          <div class="col-12 col-md-7">
            <div class="candidate-section q-mb-lg">
              <h3 class="text-h6 q-mb-md">Informations du candidat</h3>
              <div class="candidate-info">
                <div class="row items-center q-mb-md">
                  <q-avatar size="60px" color="primary" text-color="white" class="q-mr-md">
                    {{
                      application.candidateName
                        .split(' ')
                        .map((n) => n[0])
                        .join('')
                    }}
                  </q-avatar>
                  <div>
                    <h4 class="text-h6 q-mb-xs">{{ application.candidateName }}</h4>
                    <div class="text-body2 text-grey-7">{{ application.candidateEmail }}</div>
                    <div class="text-body2 text-grey-7">{{ application.jobTitle }}</div>
                  </div>
                </div>
              </div>
            </div>

            <div class="cover-letter-section q-mb-lg">
              <h3 class="text-h6 q-mb-md">Lettre de motivation</h3>
              <q-card flat bordered class="cover-letter-card">
                <q-card-section>
                  <p class="text-body1">{{ application.coverLetter }}</p>
                </q-card-section>
              </q-card>
            </div>

            <div class="resume-section q-mb-lg">
              <h3 class="text-h6 q-mb-md">CV</h3>
              <div class="resume-viewer">
                <q-btn
                  color="primary"
                  icon="description"
                  label="Voir le CV"
                  aria-label="Ouvrir le CV dans un nouvel onglet"
                  @click="viewResume"
                />
              </div>
            </div>

            <div class="notes-section">
              <h3 class="text-h6 q-mb-md">Notes internes</h3>
              <q-input
                v-model="internalNotes"
                outlined
                type="textarea"
                rows="4"
                placeholder="Ajoutez vos notes sur ce candidat..."
                aria-label="Notes internes sur le candidat"
              />
            </div>
          </div>

          <div class="col-12 col-md-4">
            <div class="application-details">
              <h3 class="text-h6 q-mb-md">Détails de la candidature</h3>

              <div class="detail-item q-mb-md">
                <div class="text-subtitle2 text-grey-8">Statut</div>
                <q-select
                  v-model="currentStatus"
                  outlined
                  dense
                  :options="statusOptions"
                  aria-label="Changer le statut de la candidature"
                  @update:model-value="updateStatus"
                />
              </div>

              <div class="detail-item q-mb-md">
                <div class="text-subtitle2 text-grey-8">Note</div>
                <q-rating
                  v-model="currentRating"
                  size="24px"
                  color="amber"
                  aria-label="Noter le candidat"
                  @update:model-value="updateRating"
                />
              </div>

              <div class="detail-item q-mb-md">
                <div class="text-subtitle2 text-grey-8">Source</div>
                <div class="text-body1">{{ sourceLabels[application.source] }}</div>
              </div>

              <div class="detail-item q-mb-md">
                <div class="text-subtitle2 text-grey-8">Date de candidature</div>
                <div class="text-body1">{{ formatDateTime(application.appliedAt) }}</div>
              </div>

              <div class="detail-item q-mb-md">
                <div class="text-subtitle2 text-grey-8">Dernière mise à jour</div>
                <div class="text-body1">{{ formatDateTime(application.updatedAt) }}</div>
              </div>

              <q-separator class="q-my-md" />

              <div class="actions-section">
                <h4 class="text-subtitle1 q-mb-md">Actions</h4>

                <q-btn
                  aria-label="Planifier un entretien avec le candidat"
                  color="purple"
                  icon="event"
                  label="Planifier entretien"
                  class="full-width q-mb-sm"
                  @click="scheduleInterview"
                />

                <q-btn
                  color="primary"
                  icon="email"
                  label="Envoyer email"
                  class="full-width q-mb-sm"
                  aria-label="Envoyer un email au candidat"
                  @click="sendEmail"
                />

                <q-btn
                  color="teal"
                  icon="local_offer"
                  label="Faire une offre"
                  class="full-width q-mb-sm"
                  aria-label="Faire une offre d'emploi au candidat"
                  @click="makeOffer"
                />

                <q-btn
                  color="negative"
                  icon="cancel"
                  label="Rejeter"
                  class="full-width"
                  aria-label="Rejeter la candidature"
                  @click="rejectCandidate"
                />
              </div>
            </div>
          </div>
        </div>
      </q-card-section>

      <q-separator />

      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="Fermer" aria-label="Fermer la boîte de dialogue" @click="closeDialog" />
        <q-btn
          color="primary"
          label="Enregistrer les notes"
          :loading="saving"
          aria-label="Enregistrer les notes internes"
          @click="saveNotes"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import type { ApplicationSource, ApplicationStatus } from 'src/enums';
import { sourceLabels } from 'src/enums/ApplicationSource';
import { statusOptions } from 'src/enums/ApplicationStatus';
import { ref, computed, watch } from 'vue';

interface Application {
  id: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  jobId: string;
  jobTitle: string;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string;
  source: ApplicationSource;
  coverLetter: string;
  resumeUrl: string;
  rating: number | null;
  notes: string;
}

interface Props {
  modelValue: boolean;
  application?: Application | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'statusUpdated'): void;
  (e: 'interviewScheduled'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const saving = ref(false);
const internalNotes = ref('');
const currentStatus = ref(0);
const currentRating = ref(0);

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

watch(
  () => props.application,
  (newApplication) => {
    if (newApplication) {
      internalNotes.value = newApplication.notes || '';
      currentStatus.value = newApplication.status;
      currentRating.value = newApplication.rating || 0;
    }
  },
  { immediate: true },
);

function closeDialog() {
  isOpen.value = false;
}

function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function viewResume() {
  window.open('/resume-viewer', '_blank');
}

function updateStatus() {
  emit('statusUpdated');
}

function updateRating() {
  console.log('Rating updated:', currentRating.value);
}

function scheduleInterview() {
  emit('interviewScheduled');
}

function sendEmail() {
  console.log('Send email to candidate');
}

function makeOffer() {
  currentStatus.value = 3;
  emit('statusUpdated');
}

function rejectCandidate() {
  currentStatus.value = 5;
  emit('statusUpdated');
}

async function saveNotes() {
  saving.value = true;
  try {
    await new Promise((resolve) => setTimeout(resolve, 1000));
    console.log('Notes saved:', internalNotes.value);
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.application-detail-dialog {
  width: 100%;
  max-width: 1000px;
}

.candidate-info {
  background-color: #f8f9fa;
  padding: 16px;
  border-radius: 8px;
}

.cover-letter-card {
  background-color: #f8f9fa;
}

.detail-item {
  background-color: #f8f9fa;
  padding: 12px;
  border-radius: 6px;
}

.actions-section .q-btn {
  justify-content: flex-start;
}

@media (max-width: 768px) {
  .application-detail-dialog {
    margin: 16px;
    max-width: calc(100vw - 32px);
  }

  .row {
    flex-direction: column;
  }
}
</style>
