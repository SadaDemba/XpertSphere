<template>
  <q-page class="applications-page">
    <div class="page-header q-pa-lg">
      <h4 class="text-h4 q-my-none text-white">Mes candidatures</h4>
      <p class="text-subtitle1 q-mt-sm text-grey-3">Suivez le statut de vos candidatures</p>
    </div>

    <div class="page-content q-pa-lg">
      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center q-py-xl">
        <q-spinner size="50px" color="primary" />
      </div>

      <!-- Error -->
      <q-banner v-else-if="hasError" class="text-white bg-negative q-mb-md rounded-borders">
        <template #avatar>
          <q-icon name="error" />
        </template>
        {{ error }}
        <template #action>
          <q-btn flat color="white" label="Réessayer" @click="loadApplications" />
        </template>
      </q-banner>

      <!-- No applications -->
      <div v-else-if="!hasApplications" class="text-center q-py-xl">
        <q-icon name="work_off" size="64px" color="grey" />
        <h6 class="q-mt-md q-mb-none text-grey">Aucune candidature</h6>
        <p class="text-grey q-mb-lg">Vous n'avez pas encore postulé à d'offres</p>
        <q-btn
          color="primary"
          label="Voir les offres d'emploi"
          icon="search"
          @click="goToJobListings"
        />
      </div>

      <!-- Applications -->
      <div v-else>
        <!-- Stats Cards -->
        <div class="row q-gutter-md q-mb-lg">
          <div class="col-12 col-sm-6 col-md-3">
            <q-card class="stats-card">
              <q-card-section class="text-center">
                <div class="stats-number text-primary">{{ applications.length }}</div>
                <div class="stats-label">Total</div>
              </q-card-section>
            </q-card>
          </div>

          <div class="col-12 col-sm-6 col-md-3">
            <q-card class="stats-card">
              <q-card-section class="text-center">
                <div class="stats-number text-orange">{{ activeApplications.length }}</div>
                <div class="stats-label">En cours</div>
              </q-card-section>
            </q-card>
          </div>

          <div class="col-12 col-sm-6 col-md-3">
            <q-card class="stats-card">
              <q-card-section class="text-center">
                <div class="stats-number text-positive">{{ acceptedCount }}</div>
                <div class="stats-label">Acceptées</div>
              </q-card-section>
            </q-card>
          </div>

          <div class="col-12 col-sm-6 col-md-3">
            <q-card class="stats-card">
              <q-card-section class="text-center">
                <div class="stats-number text-negative">{{ rejectedCount }}</div>
                <div class="stats-label">Rejetées</div>
              </q-card-section>
            </q-card>
          </div>
        </div>

        <!-- Filter Tabs -->
        <q-tabs
          v-model="currentTab"
          class="text-grey-7 q-mb-lg"
          active-color="primary"
          indicator-color="primary"
          align="left"
        >
          <q-tab name="all" label="Toutes" />
          <q-tab name="active" label="En cours" />
          <q-tab name="completed" label="Terminées" />
        </q-tabs>

        <!-- Applications List -->
        <div class="applications-list">
          <div v-for="application in filteredApplications" :key="application.id" class="q-mb-md">
            <application-card
              :application="application"
              @view="viewApplication"
              @withdraw="confirmWithdraw"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Withdraw Confirmation Dialog -->
    <q-dialog v-model="showWithdrawDialog" persistent>
      <q-card style="min-width: 350px">
        <q-card-section>
          <div class="text-h6">Retirer la candidature</div>
        </q-card-section>

        <q-card-section class="q-pt-none">
          <p>Êtes-vous sûr de vouloir retirer cette candidature ?</p>
          <p class="text-caption text-grey-7">
            Cette action est irréversible. Vous ne pourrez plus candidater à cette offre.
          </p>

          <q-input
            v-model="withdrawReason"
            type="textarea"
            label="Raison (optionnelle)"
            filled
            rows="3"
            placeholder="Ex: J'ai trouvé un autre poste, l'offre ne correspond plus à mes attentes..."
          />
        </q-card-section>

        <q-card-actions align="right" class="text-primary">
          <q-btn flat label="Annuler" @click="showWithdrawDialog = false" />
          <q-btn
            flat
            label="Confirmer le retrait"
            :loading="withdrawLoading"
            color="negative"
            @click="handleWithdraw"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup lang="ts">
/* eslint-disable @typescript-eslint/no-explicit-any */
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useApplicationStore } from '../stores/applicationStore';
import { useNotification } from '../composables/notification';
import { ApplicationStatus } from '../enums';
import ApplicationCard from '../components/ApplicationCard.vue';

// Composables
const router = useRouter();
const { showSuccessNotification, showErrorNotification } = useNotification();

// Store
const applicationStore = useApplicationStore();
const {
  applications,
  isLoading,
  error,
  hasError,
  hasApplications,
  activeApplications,
  completedApplications,
} = storeToRefs(applicationStore);

// State
const currentTab = ref('all');
const showWithdrawDialog = ref(false);
const withdrawReason = ref('');
const withdrawLoading = ref(false);
const applicationToWithdraw = ref<string | null>(null);

// Computed
const filteredApplications = computed(() => {
  switch (currentTab.value) {
    case 'active':
      return activeApplications.value;
    case 'completed':
      return completedApplications.value;
    default:
      return applications.value;
  }
});

const acceptedCount = computed(
  () => applications.value.filter((app) => app.currentStatus === ApplicationStatus.Accepted).length,
);

const rejectedCount = computed(
  () => applications.value.filter((app) => app.currentStatus === ApplicationStatus.Rejected).length,
);

// Methods
const loadApplications = async () => {
  await applicationStore.fetchMyApplications();
};

const viewApplication = (applicationId: string) => {
  router.push(`/applications/${applicationId}`);
};

const goToJobListings = () => {
  router.push('/');
};

const confirmWithdraw = (applicationId: string) => {
  applicationToWithdraw.value = applicationId;
  withdrawReason.value = '';
  showWithdrawDialog.value = true;
};

const handleWithdraw = async () => {
  if (!applicationToWithdraw.value) return;

  withdrawLoading.value = true;

  try {
    const success = await applicationStore.withdrawApplication(
      applicationToWithdraw.value,
      withdrawReason.value || 'Candidature retirée par le candidat',
    );

    if (success) {
      showSuccessNotification('Candidature retirée avec succès');

      showWithdrawDialog.value = false;
      applicationToWithdraw.value = null;
    } else {
      showErrorNotification(applicationStore.error || 'Erreur lors du retrait');
    }
  } catch (error: any) {
    console.log(error.message);
    showErrorNotification('Une erreur inattendue est survenue');
  } finally {
    withdrawLoading.value = false;
  }
};

// Lifecycle
onMounted(async () => {
  await loadApplications();
});
</script>

<style lang="scss" scoped>
.applications-page {
  min-height: 100vh;
  background-color: #f8f9fa;
}

.page-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.stats-card {
  border-radius: 12px;
  border: 1px solid #e0e0e0;
}

.stats-number {
  font-size: 2rem;
  font-weight: bold;
  line-height: 1;
}

.stats-label {
  font-size: 0.9rem;
  color: #6c757d;
  margin-top: 4px;
}

.applications-list {
  max-width: 800px;
}
</style>
