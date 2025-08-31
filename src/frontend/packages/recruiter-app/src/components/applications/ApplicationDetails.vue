<template>
  <q-dialog
    :model-value="modelValue"
    max-width="800px"
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <q-card>
      <q-card-section class="row items-center justify-between">
        <div>
          <div class="text-h6">Détails de la candidature</div>
          <div class="text-subtitle2 text-grey-6">{{ application?.candidateName }}</div>
        </div>
        <q-btn flat round dense icon="close" @click="$emit('update:modelValue', false)" />
      </q-card-section>

      <q-separator />

      <q-card-section v-if="application" class="q-pt-none">
        <div class="row q-col-gutter-md">
          <!-- Informations du candidat -->
          <div class="col-12 col-md-6">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="person" class="q-mr-sm" />
                  Candidat
                </div>

                <div class="q-mb-sm"><strong>Nom :</strong> {{ application.candidateName }}</div>
                <div class="q-mb-sm"><strong>Email :</strong> {{ application.candidateEmail }}</div>

                <q-btn
                  color="primary"
                  flat
                  icon="visibility"
                  label="Voir le profil"
                  @click="viewCandidate"
                />
              </q-card-section>
            </q-card>
          </div>

          <!-- Informations du poste -->
          <div class="col-12 col-md-6">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="work" class="q-mr-sm" />
                  Poste
                </div>

                <div class="q-mb-sm"><strong>Titre :</strong> {{ application.jobOfferTitle }}</div>
                <div class="q-mb-sm">
                  <strong>Organisation :</strong> {{ application.organizationName }}
                </div>

                <q-btn
                  color="primary"
                  flat
                  icon="visibility"
                  label="Voir l'offre"
                  @click="viewJobOffer"
                />
              </q-card-section>
            </q-card>
          </div>

          <!-- Statut et évaluation -->
          <div class="col-12">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="info" class="q-mr-sm" />
                  Statut et Évaluation
                </div>

                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-4">
                    <div class="q-mb-sm">
                      <strong>Statut actuel :</strong>
                    </div>
                    <q-chip
                      :color="getStatusConfig(application.currentStatus).color"
                      :text-color="getStatusConfig(application.currentStatus).textColor"
                      :icon="getStatusConfig(application.currentStatus).icon"
                    >
                      {{ getStatusConfig(application.currentStatus).label }}
                    </q-chip>
                  </div>

                  <div class="col-12 col-md-4">
                    <div class="q-mb-sm">
                      <strong>Évaluation :</strong>
                    </div>
                    <q-rating
                      v-if="application.rating"
                      :model-value="application.rating"
                      :max="5"
                      color="amber"
                      readonly
                    />
                    <span v-else class="text-grey-6">Pas encore évaluée</span>
                  </div>

                  <div class="col-12 col-md-4">
                    <div class="q-mb-sm">
                      <strong>Date de candidature :</strong>
                    </div>
                    <div>{{ formatDate(application.appliedAt) }}</div>
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>

          <!-- Assignations -->
          <div class="col-12">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="group" class="q-mr-sm" />
                  Assignations
                </div>

                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-6">
                    <div class="q-mb-sm">
                      <strong>Manager assigné :</strong>
                    </div>
                    <div v-if="application.assignedManagerName">
                      <q-chip color="blue-1" text-color="blue-10">
                        <q-avatar color="blue" text-color="white">
                          {{ application.assignedManagerName[0] }}
                        </q-avatar>
                        {{ application.assignedManagerName }}
                      </q-chip>
                    </div>
                    <span v-else class="text-grey-6">Aucun manager assigné</span>
                  </div>

                  <div class="col-12 col-md-6">
                    <div class="q-mb-sm">
                      <strong>Évaluateur technique assigné :</strong>
                    </div>
                    <div v-if="application.assignedTechnicalEvaluatorName">
                      <q-chip color="purple-1" text-color="purple-10">
                        <q-avatar color="purple" text-color="white">
                          {{ application.assignedTechnicalEvaluatorName[0] }}
                        </q-avatar>
                        {{ application.assignedTechnicalEvaluatorName }}
                      </q-chip>
                    </div>
                    <span v-else class="text-grey-6">Aucun évaluateur technique assigné</span>
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>

          <!-- Lettre de motivation et notes -->
          <div v-if="application.coverLetter || application.additionalNotes" class="col-12">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="description" class="q-mr-sm" />
                  Informations complémentaires
                </div>

                <div v-if="application.coverLetter" class="q-mb-md">
                  <div class="text-weight-medium q-mb-sm">Lettre de motivation :</div>
                  <div class="text-body2 q-pa-md bg-grey-2 rounded-borders">
                    {{ application.coverLetter }}
                  </div>
                </div>

                <div v-if="application.additionalNotes" class="q-mb-md">
                  <div class="text-weight-medium q-mb-sm">Notes additionnelles :</div>
                  <div class="text-body2 q-pa-md bg-grey-2 rounded-borders">
                    {{ application.additionalNotes }}
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>

          <!-- États -->
          <div class="col-12">
            <q-card flat bordered>
              <q-card-section>
                <div class="text-h6 q-mb-md">
                  <q-icon name="flag" class="q-mr-sm" />
                  États
                </div>

                <div class="row q-col-gutter-md">
                  <div class="col-12 col-md-4">
                    <q-chip
                      :color="application.isActive ? 'positive' : 'negative'"
                      text-color="white"
                      :icon="application.isActive ? 'check_circle' : 'cancel'"
                    >
                      {{ application.isActive ? 'Active' : 'Inactive' }}
                    </q-chip>
                  </div>

                  <div class="col-12 col-md-4">
                    <q-chip
                      :color="application.isInProgress ? 'info' : 'grey'"
                      text-color="white"
                      :icon="application.isInProgress ? 'hourglass_top' : 'hourglass_disabled'"
                    >
                      {{ application.isInProgress ? 'En cours' : 'Pas en cours' }}
                    </q-chip>
                  </div>

                  <div class="col-12 col-md-4">
                    <q-chip
                      :color="application.isCompleted ? 'positive' : 'grey'"
                      text-color="white"
                      :icon="application.isCompleted ? 'check' : 'remove'"
                    >
                      {{ application.isCompleted ? 'Terminée' : 'En cours' }}
                    </q-chip>
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </q-card-section>

      <q-card-actions align="right">
        <q-btn flat label="Fermer" @click="$emit('update:modelValue', false)" />
        <q-btn color="warning" label="Modifier le statut" @click="showUpdateStatus" />
        <q-btn color="primary" label="Assigner" @click="showAssign" />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { date } from 'quasar';
import { applicationStatusConfig } from 'src/models/application';
import type { ApplicationDto } from 'src/models/application';
import type { ApplicationStatus } from 'src/enums/ApplicationStatus';

interface Props {
  modelValue: boolean;
  application: ApplicationDto | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'updated'): void;
  (e: 'view-candidate', candidateId: string): void;
  (e: 'view-job-offer', jobOfferId: string): void;
  (e: 'update-status'): void;
  (e: 'assign'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

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

const formatDate = (dateString: string) => {
  return date.formatDate(dateString, 'DD/MM/YYYY HH:mm');
};

const viewCandidate = () => {
  if (props.application) {
    emit('view-candidate', props.application.candidateId);
  }
};

const viewJobOffer = () => {
  if (props.application) {
    emit('view-job-offer', props.application.jobOfferId);
  }
};

const showUpdateStatus = () => {
  emit('update-status');
};

const showAssign = () => {
  emit('assign');
};
</script>
