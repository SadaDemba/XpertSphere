<template>
  <q-page padding>
    <div v-if="loading" class="flex flex-center q-pa-lg">
      <q-spinner size="xl" />
    </div>

    <div v-else-if="application">
      <!-- Header -->
      <div class="row items-center justify-between q-mb-lg">
        <div>
          <q-btn flat icon="arrow_back" label="Retour" class="q-mb-sm" @click="goBack" />
          <h4 class="q-my-none">{{ application.candidateName }}</h4>
          <p class="text-grey-6 q-mb-none">Candidature pour {{ application.jobOfferTitle }}</p>
        </div>
        <div class="q-gutter-sm">
          <q-btn
            color="warning"
            icon="update"
            label="Modifier le statut"
            @click="showStatusDialog = true"
          />
          <q-btn
            color="primary"
            icon="person_add"
            label="Assigner"
            @click="showAssignDialog = true"
          />
        </div>
      </div>

      <!-- Statut actuel -->
      <q-card class="q-mb-lg">
        <q-card-section>
          <div class="row items-center justify-between">
            <div>
              <div class="text-h6 q-mb-sm">Statut actuel</div>
              <q-chip
                :color="getStatusConfig(application.currentStatus).color"
                :text-color="getStatusConfig(application.currentStatus).textColor"
                :icon="getStatusConfig(application.currentStatus).icon"
                size="lg"
              >
                {{ getStatusConfig(application.currentStatus).label }}
              </q-chip>
            </div>
            <div class="text-right">
              <div class="text-caption text-grey-6">Dernière mise à jour</div>
              <div>{{ formatDate(application.lastUpdatedAt || application.appliedAt) }}</div>
            </div>
          </div>
        </q-card-section>
      </q-card>

      <div class="row q-col-gutter-lg">
        <!-- Colonne gauche -->
        <div class="col-12 col-md-8">
          <!-- Informations candidat -->
          <q-card class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="person" class="q-mr-sm" />
                Informations du candidat
              </div>

              <div class="row q-col-gutter-md">
                <div class="col-12 col-sm-6">
                  <div class="text-weight-medium">Nom complet</div>
                  <div>{{ application.candidateName }}</div>
                </div>
                <div class="col-12 col-sm-6">
                  <div class="text-weight-medium">Email</div>
                  <div>{{ application.candidateEmail }}</div>
                </div>
              </div>

              <div class="q-mt-md">
                <q-btn
                  color="primary"
                  flat
                  icon="visibility"
                  label="Voir le profil complet"
                  @click="viewCandidate"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Informations du poste -->
          <q-card class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="work" class="q-mr-sm" />
                Informations du poste
              </div>

              <div class="row q-col-gutter-md">
                <div class="col-12 col-sm-6">
                  <div class="text-weight-medium">Titre du poste</div>
                  <div>{{ application.jobOfferTitle }}</div>
                </div>
                <div class="col-12 col-sm-6">
                  <div class="text-weight-medium">Organisation</div>
                  <div>{{ application.organizationName }}</div>
                </div>
              </div>

              <div class="q-mt-md">
                <q-btn
                  color="primary"
                  flat
                  icon="visibility"
                  label="Voir l'offre d'emploi"
                  @click="viewJobOffer"
                />
              </div>
            </q-card-section>
          </q-card>

          <!-- Lettre de motivation et notes -->
          <q-card v-if="application.coverLetter || application.additionalNotes" class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="description" class="q-mr-sm" />
                Informations complémentaires
              </div>

              <div v-if="application.coverLetter" class="q-mb-md">
                <div class="text-weight-medium q-mb-sm">Lettre de motivation</div>
                <div class="text-body2 q-pa-md bg-grey-1 rounded-borders">
                  {{ application.coverLetter }}
                </div>
              </div>

              <div v-if="application.additionalNotes" class="q-mb-md">
                <div class="text-weight-medium q-mb-sm">Notes additionnelles</div>
                <div class="text-body2 q-pa-md bg-grey-1 rounded-borders">
                  {{ application.additionalNotes }}
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Historique -->
          <q-card>
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="history" class="q-mr-sm" />
                Historique
              </div>

              <div v-if="statusHistory.length > 0">
                <q-timeline color="primary">
                  <q-timeline-entry
                    v-for="entry in statusHistory"
                    :key="entry.id"
                    :color="getStatusConfig(entry.status).color"
                    :icon="getStatusConfig(entry.status).icon"
                  >
                    <template #title>
                      <div class="text-weight-medium">
                        {{ getStatusConfig(entry.status).label }}
                      </div>
                    </template>

                    <template #subtitle>
                      <div class="text-caption text-grey-6">
                        {{ formatDate(entry.updatedAt) }} par {{ entry.updatedByUserName }}
                      </div>
                    </template>

                    <div v-if="entry.comment" class="q-mt-sm">
                      <div class="text-body2 q-pa-sm bg-grey-1 rounded-borders">
                        {{ entry.comment }}
                      </div>
                    </div>

                    <div v-if="entry.hasRating && entry.rating" class="q-mt-sm">
                      <div class="text-caption text-grey-6 q-mb-xs">Évaluation :</div>
                      <q-rating
                        :model-value="entry.rating"
                        :max="5"
                        size="sm"
                        color="amber"
                        readonly
                      />
                      <span class="text-caption q-ml-sm">{{ entry.rating }}/5</span>
                    </div>
                  </q-timeline-entry>
                </q-timeline>
              </div>

              <div v-else class="text-center q-pa-lg">
                <q-icon name="history" size="lg" color="grey-5" />
                <div class="text-h6 text-grey-6 q-mt-md">Aucun historique disponible</div>
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Colonne droite -->
        <div class="col-12 col-md-4">
          <!-- Évaluation -->
          <q-card class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="star" class="q-mr-sm" />
                Évaluation
              </div>

              <div class="text-center">
                <q-rating
                  v-if="application.rating"
                  :model-value="application.rating"
                  :max="5"
                  size="lg"
                  color="amber"
                  readonly
                />
                <div v-else class="text-grey-6">Pas encore évaluée</div>
                <div v-if="application.rating" class="text-caption q-mt-sm">
                  {{ application.rating }}/5
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Assignations -->
          <q-card class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="group" class="q-mr-sm" />
                Assignations
              </div>

              <div class="q-mb-md">
                <div class="text-weight-medium q-mb-sm">Manager</div>
                <div v-if="application.assignedManagerName">
                  <q-chip color="blue-1" text-color="blue-10">
                    <q-avatar color="blue" text-color="white">
                      {{ application.assignedManagerName[0] }}
                    </q-avatar>
                    {{ application.assignedManagerName }}
                  </q-chip>
                </div>
                <div v-else class="text-grey-6">Aucun manager assigné</div>
              </div>

              <div class="q-mb-md">
                <div class="text-weight-medium q-mb-sm">Évaluateur technique</div>
                <div v-if="application.assignedTechnicalEvaluatorName">
                  <q-chip color="purple-1" text-color="purple-10">
                    <q-avatar color="purple" text-color="white">
                      {{ application.assignedTechnicalEvaluatorName[0] }}
                    </q-avatar>
                    {{ application.assignedTechnicalEvaluatorName }}
                  </q-chip>
                </div>
                <div v-else class="text-grey-6">Aucun évaluateur technique assigné</div>
              </div>

              <q-btn
                color="primary"
                flat
                icon="person_add"
                label="Modifier les assignations"
                @click="showAssignDialog = true"
              />
            </q-card-section>
          </q-card>

          <!-- États -->
          <q-card class="q-mb-lg">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="flag" class="q-mr-sm" />
                États
              </div>

              <div class="q-gutter-sm">
                <q-chip
                  :color="application.isActive ? 'positive' : 'negative'"
                  text-color="white"
                  :icon="application.isActive ? 'check_circle' : 'cancel'"
                >
                  {{ application.isActive ? 'Active' : 'Inactive' }}
                </q-chip>

                <q-chip
                  :color="application.isInProgress ? 'info' : 'grey'"
                  text-color="white"
                  :icon="application.isInProgress ? 'hourglass_top' : 'hourglass_disabled'"
                >
                  {{ application.isInProgress ? 'En cours' : 'Pas en cours' }}
                </q-chip>

                <q-chip
                  :color="application.isCompleted ? 'positive' : 'grey'"
                  text-color="white"
                  :icon="application.isCompleted ? 'check' : 'remove'"
                >
                  {{ application.isCompleted ? 'Terminée' : 'En cours' }}
                </q-chip>
              </div>
            </q-card-section>
          </q-card>

          <!-- Informations sur les dates -->
          <q-card>
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="schedule" class="q-mr-sm" />
                Dates importantes
              </div>

              <div class="q-mb-sm">
                <div class="text-weight-medium">Date de candidature</div>
                <div>{{ formatDate(application.appliedAt) }}</div>
              </div>

              <div v-if="application.lastUpdatedAt" class="q-mb-sm">
                <div class="text-weight-medium">Dernière mise à jour</div>
                <div>{{ formatDate(application.lastUpdatedAt) }}</div>
              </div>

              <div class="q-mb-sm">
                <div class="text-weight-medium">Créée le</div>
                <div>{{ formatDate(application.createdAt) }}</div>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>

    <div v-else class="text-center q-pa-lg">
      <q-icon name="search_off" size="xl" color="grey-5" />
      <div class="text-h6 text-grey-6 q-mt-md">Candidature non trouvée</div>
      <q-btn color="primary" label="Retour" class="q-mt-md" @click="goBack" />
    </div>

    <!-- Update Status Dialog -->
    <application-status-update
      v-model="showStatusDialog"
      :application="application"
      @updated="refreshData"
    />

    <!-- Assign Dialog -->
    <application-assign
      v-model="showAssignDialog"
      :application="application"
      @updated="refreshData"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { date } from 'quasar';
import { useApplicationStore } from 'src/stores/applicationStore';
import { applicationStatusConfig } from 'src/models/application';
import type { ApplicationStatus } from 'src/enums/ApplicationStatus';
import ApplicationStatusUpdate from 'src/components/applications/ApplicationStatusUpdate.vue';
import ApplicationAssign from 'src/components/applications/ApplicationAssign.vue';

const route = useRoute();
const router = useRouter();
const applicationStore = useApplicationStore();

const loading = ref(true);
const showStatusDialog = ref(false);
const showAssignDialog = ref(false);

const application = computed(() => applicationStore.currentApplication);
const statusHistory = computed(() => applicationStore.statusHistory);

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

const goBack = () => {
  router.push('/applications');
};

const viewCandidate = () => {
  if (application.value) {
    router.push(`/candidates/${application.value.candidateId}`);
  }
};

const viewJobOffer = () => {
  if (application.value) {
    router.push(`/jobs/${application.value.jobOfferId}`);
  }
};

const refreshData = async () => {
  const applicationId = route.params.id as string;
  if (applicationId) {
    await Promise.all([
      applicationStore.fetchApplicationById(applicationId),
      applicationStore.fetchStatusHistory(applicationId),
    ]);
  }
};

onMounted(async () => {
  const applicationId = route.params.id as string;
  if (applicationId) {
    try {
      await refreshData();
    } catch (error) {
      console.error('Error loading application:', error);
    } finally {
      loading.value = false;
    }
  } else {
    loading.value = false;
  }
});
</script>
