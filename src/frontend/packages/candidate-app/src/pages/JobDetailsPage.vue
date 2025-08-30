<template>
  <q-page class="job-details-page">
    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center items-center" style="height: 50vh">
      <q-spinner size="50px" color="primary" />
    </div>

    <!-- Error -->
    <q-banner v-else-if="hasError" class="text-white bg-negative">
      <template #avatar>
        <q-icon name="error" />
      </template>
      {{ error }}
      <template #action>
        <q-btn flat color="white" label="Retour" @click="goBack" />
      </template>
    </q-banner>

    <!-- Job Details -->
    <div v-else-if="currentJobOffer" class="job-details-content">
      <!-- Header -->
      <div class="job-header q-pa-lg">
        <div class="row items-center q-mb-md">
          <div class="col">
            <div class="breadcrumb-container q-pa-md">
              <div class="breadcrumb-content">
                <q-breadcrumbs class="text-grey-7" active-color="primary">
                  <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
                  <q-breadcrumbs-el :label="currentJobOffer.title" icon="description" />
                </q-breadcrumbs>
              </div>
            </div>
            <p class="text-h6 q-my-sm text-grey-3">{{ currentJobOffer.organizationName }}</p>
          </div>
        </div>

        <div class="job-meta-header row q-gutter-md">
          <q-chip color="white" text-color="primary" icon="place">
            {{ currentJobOffer.location || 'Non spécifiée' }}
          </q-chip>
          <q-chip color="white" text-color="primary" icon="work">
            {{ workModeLabels[currentJobOffer.workMode] }}
          </q-chip>
          <q-chip color="white" text-color="primary" icon="schedule">
            {{ contractTypeLabels[currentJobOffer.contractType] }}
          </q-chip>
        </div>
      </div>

      <!-- Content -->
      <div class="job-content q-pa-lg">
        <div class="row q-gutter-lg">
          <!-- Main Content -->
          <div class="col-12 col-md-8">
            <!-- Description -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">
                  <q-icon name="description" class="q-mr-sm" />
                  Description du poste
                </h6>
                <!-- eslint-disable vue/no-v-html -->
                <div class="job-description" v-html="formattedDescription"></div>
                <!--eslint-enable-->
              </q-card-section>
            </q-card>

            <!-- Requirements -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">
                  <q-icon name="checklist" class="q-mr-sm" />
                  Exigences
                </h6>
                <!-- eslint-disable vue/no-v-html -->
                <div class="job-requirements" v-html="formattedRequirements"></div>
                <!--eslint-enable-->
              </q-card-section>
            </q-card>
          </div>

          <!-- Sidebar -->
          <div class="col-12 col-md-4">
            <!-- Application Card -->
            <q-card class="application-card q-mb-lg">
              <q-card-section>
                <div class="text-center">
                  <template v-if="!isAuthenticated">
                    <h6 class="q-mt-none q-mb-md">Intéressé par cette offre ?</h6>
                    <p class="text-grey-7 q-mb-md">Connectez-vous pour candidater</p>
                    <q-btn
                      color="primary"
                      label="Se connecter"
                      icon="login"
                      class="full-width q-mb-sm"
                      @click="goToLogin"
                    />
                    <q-btn flat label="Créer un compte" class="full-width" @click="goToRegister" />
                  </template>

                  <template v-else-if="hasApplied">
                    <q-icon name="check_circle" color="positive" size="48px" />
                    <h6 class="q-mt-md q-mb-sm">Candidature envoyée</h6>
                    <p class="text-grey-7 q-mb-md">Votre candidature a été envoyée avec succès</p>
                    <q-btn
                      flat
                      color="primary"
                      label="Voir ma candidature"
                      @click="viewApplication"
                    />
                  </template>

                  <template v-else>
                    <h6 class="q-mt-none q-mb-md">Candidater à cette offre</h6>
                    <q-btn
                      color="positive"
                      label="Postuler"
                      icon="send"
                      class="full-width"
                      :loading="applicationLoading"
                      @click="showApplicationDialog = true"
                    />
                  </template>
                </div>
              </q-card-section>
            </q-card>

            <!-- Job Info -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">Informations</h6>

                <div
                  v-if="currentJobOffer.salaryMin || currentJobOffer.salaryMax"
                  class="info-item q-mb-md"
                >
                  <div class="text-weight-medium">Salaire</div>
                  <div class="text-positive">
                    <template v-if="currentJobOffer.salaryMin && currentJobOffer.salaryMax">
                      {{ formatSalary(currentJobOffer.salaryMin) }} -
                      {{ formatSalary(currentJobOffer.salaryMax) }}
                    </template>
                    <template v-else-if="currentJobOffer.salaryMin">
                      À partir de {{ formatSalary(currentJobOffer.salaryMin) }}
                    </template>
                    <template v-else-if="currentJobOffer.salaryMax">
                      Jusqu'à {{ formatSalary(currentJobOffer.salaryMax) }}
                    </template>
                    {{ currentJobOffer.salaryCurrency || '€' }} brut/an
                  </div>
                </div>

                <div class="info-item q-mb-md">
                  <div class="text-weight-medium">Date de publication</div>
                  <div>
                    {{ formatDate(currentJobOffer.publishedAt || currentJobOffer.createdAt) }}
                  </div>
                </div>

                <div v-if="currentJobOffer.expiresAt" class="info-item q-mb-md">
                  <div class="text-weight-medium">Date d'expiration</div>
                  <div>{{ formatDate(currentJobOffer.expiresAt) }}</div>
                </div>

                <div v-if="statusConfig" class="info-item">
                  <div class="text-weight-medium">Statut</div>
                  <q-chip
                    :color="statusConfig.color"
                    :text-color="statusConfig.textColor"
                    size="sm"
                    :icon="statusConfig.icon"
                  >
                    {{ statusConfig.label }}
                  </q-chip>
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </div>
    </div>

    <!-- Application Dialog -->
    <application-dialog
      v-model="showApplicationDialog"
      :job="currentJobOffer"
      @submitted="handleApplicationSubmitted"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { date } from 'quasar';
import { storeToRefs } from 'pinia';
import { useJobOfferStore } from '../stores/jobOfferStore';
import { useApplicationStore } from '../stores/applicationStore';
import { useAuthStore } from '../stores/authStore';
import { jobOfferStatusConfig, workModeLabels, contractTypeLabels } from '../models/job';
import ApplicationDialog from '../components/ApplicationDialog.vue';

// Router
const route = useRoute();
const router = useRouter();

// Stores
const jobOfferStore = useJobOfferStore();
const applicationStore = useApplicationStore();
const authStore = useAuthStore();

const { currentJobOffer, isLoading, error, hasError } = storeToRefs(jobOfferStore);
const { isAuthenticated } = storeToRefs(authStore);

// State
const showApplicationDialog = ref(false);
const applicationLoading = ref(false);
const hasApplied = ref(false);

// Computed
const statusConfig = computed(() =>
  currentJobOffer.value ? jobOfferStatusConfig[currentJobOffer.value.status] : null,
);

const formattedDescription = computed(() => {
  if (!currentJobOffer.value?.description) return '';
  return currentJobOffer.value.description.replace(/\n/g, '<br>');
});

const formattedRequirements = computed(() => {
  if (!currentJobOffer.value?.requirements) return '';
  return currentJobOffer.value.requirements.replace(/\n/g, '<br>');
});

// Methods
const loadJobOffer = async () => {
  const jobId = route.params.id as string;
  await jobOfferStore.fetchJobOfferById(jobId);

  // Check if user has already applied
  if (isAuthenticated.value) {
    hasApplied.value = await applicationStore.checkIfApplied(jobId);
  }
};

const goBack = () => {
  router.go(-1);
};

const goToLogin = () => {
  router.push('/login');
};

const goToRegister = () => {
  router.push('/register');
};

const handleApplicationSubmitted = () => {
  hasApplied.value = true;
  showApplicationDialog.value = false;
};

const viewApplication = async () => {
  await applicationStore.fetchMyApplications();
  const application = applicationStore.applications.find(
    (app) => app.jobOfferId === currentJobOffer.value?.id,
  );
  if (application) {
    router.push(`/applications/${application.id}`);
  }
};

const formatSalary = (amount: number): string => {
  return new Intl.NumberFormat('fr-FR').format(amount);
};

const formatDate = (dateString: string): string => {
  return date.formatDate(new Date(dateString), 'DD MMMM YYYY', {
    months: [
      'janvier',
      'février',
      'mars',
      'avril',
      'mai',
      'juin',
      'juillet',
      'août',
      'septembre',
      'octobre',
      'novembre',
      'décembre',
    ],
  });
};

// Lifecycle
onMounted(async () => {
  await loadJobOffer();
});
</script>

<style lang="scss" scoped>
.job-details-page {
  min-height: 100vh;
  background-color: rgb(233, 233, 233);
}

.job-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.job-meta-header .q-chip {
  font-weight: 500;
}

.job-content {
  flex: 1;
}

.breadcrumb-container {
  background: white;
  border-bottom: 1px solid #e0e0e0;
  width: fit-content;
  border-radius: 22px;
}

.breadcrumb-content {
  max-width: 1400px;
  margin: 0 auto;
}

.application-card {
  border: 2px solid var(--q-primary);
  border-radius: 12px;
}

.info-item {
  padding-bottom: 8px;
  border-bottom: 1px solid #f0f0f0;

  &:last-child {
    border-bottom: none;
    padding-bottom: 0;
  }
}

.job-description,
.job-requirements {
  line-height: 1.6;

  :deep(br) {
    margin-bottom: 8px;
  }
}
</style>
