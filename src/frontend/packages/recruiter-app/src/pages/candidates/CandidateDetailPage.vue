<template>
  <q-page class="candidate-detail-page">
    <!-- Breadcrumbs -->
    <div class="breadcrumb-container q-pa-md q-ma-lg">
      <div class="breadcrumb-content">
        <q-breadcrumbs class="text-grey-7" active-color="primary">
          <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
          <q-breadcrumbs-el label="Candidats" icon="group" to="/candidates" />
          <q-breadcrumbs-el v-if="candidate" :label="candidate.fullName" icon="person" />
        </q-breadcrumbs>
      </div>
    </div>

    <div v-if="userStore.isLoading" class="row justify-center q-mt-lg">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <div v-else-if="userStore.hasError" class="row justify-center q-mt-lg">
      <q-card class="q-pa-md">
        <q-card-section>
          <div class="text-h6 text-negative">Erreur</div>
          <p>{{ userStore.errorMessage }}</p>
        </q-card-section>
      </q-card>
    </div>

    <div v-else-if="candidate" class="candidate-content q-pa-lg">
      <!-- Header Profile -->
      <q-card class="profile-header-card q-mb-lg">
        <q-card-section>
          <div class="row items-center q-gutter-lg">
            <q-avatar size="100px" color="primary" text-color="white">
              <span class="text-h3">{{
                candidate.fullName ? candidate.fullName.charAt(0).toUpperCase() : '?'
              }}</span>
            </q-avatar>

            <div class="col">
              <h4 class="text-h4 q-my-none">{{ candidate.fullName }}</h4>
              <p class="text-h6 text-grey-7 q-my-sm">{{ candidate.email }}</p>

              <div class="q-mt-md">
                <q-chip
                  :color="candidate.isActive ? 'positive' : 'negative'"
                  text-color="white"
                  icon="circle"
                  size="md"
                >
                  {{ candidate.isActive ? 'Profil Actif' : 'Profil Inactif' }}
                </q-chip>
                <q-chip
                  v-if="candidate.experience"
                  color="blue-2"
                  text-color="blue-10"
                  icon="work"
                  size="md"
                  class="q-ml-sm"
                >
                  {{ candidate.experience }} ans d'expérience
                </q-chip>
                <q-chip
                  color="purple-2"
                  text-color="purple-10"
                  icon="check_circle"
                  size="md"
                  class="q-ml-sm"
                >
                  Profil complet à {{ candidate.profileCompletionPercentage }}%
                </q-chip>
              </div>
            </div>

            <div class="col-auto">
              <q-btn
                color="primary"
                unelevated
                icon="mail"
                label="Contacter"
                @click="contactCandidate"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>

      <div class="row q-col-gutter-lg">
        <!-- Colonne gauche -->
        <div class="col-12 col-md-8">
          <!-- Informations personnelles -->
          <q-card class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="person" color="primary" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Informations personnelles</div>
                  <div class="text-caption text-grey-7">Coordonnées et informations de base</div>
                </div>
              </div>

              <div class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div class="row q-col-gutter-lg">
                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="badge" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Nom complet</div>
                        <div class="text-body1 text-weight-medium">{{ candidate.fullName }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="email" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Email</div>
                        <div class="text-body1">
                          <a
                            :href="`mailto:${candidate.email}`"
                            class="text-primary text-decoration-none"
                          >
                            {{ candidate.email }}
                          </a>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="row q-col-gutter-lg q-mt-md">
                  <div v-if="candidate.phoneNumber" class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="phone" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Téléphone</div>
                        <div class="text-body1">{{ candidate.phoneNumber }}</div>
                      </div>
                    </div>
                  </div>

                  <div class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="schedule" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Inscrit le</div>
                        <div class="text-body1">{{ formatDate(candidate.createdAt) }}</div>
                      </div>
                    </div>
                  </div>
                </div>

                <div v-if="candidate.lastLoginAt" class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="login" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Dernière connexion</div>
                        <div class="text-body1">{{ formatDate(candidate.lastLoginAt) }}</div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="analytics" color="primary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Complétude du profil</div>
                        <q-linear-progress
                          :value="candidate.profileCompletionPercentage / 100"
                          color="primary"
                          class="q-mt-xs"
                        />
                        <div class="text-caption text-grey-6 q-mt-xs">
                          {{ candidate.profileCompletionPercentage }}% complété
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Informations professionnelles -->
          <q-card class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="work" color="secondary" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Informations professionnelles</div>
                  <div class="text-caption text-grey-7">Expérience et compétences</div>
                </div>
              </div>

              <div class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div class="row q-col-gutter-lg">
                  <div v-if="candidate.experience" class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="work_history" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Années d'expérience</div>
                        <div class="text-body1 text-weight-medium">
                          {{ candidate.experience }} ans
                        </div>
                      </div>
                    </div>
                  </div>

                  <div v-if="candidate.desiredSalary" class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="payments" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Salaire souhaité</div>
                        <div class="text-body1 text-weight-medium">
                          {{ candidate.desiredSalary.toLocaleString() }} €
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div v-if="candidate.skills" class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <div class="info-group">
                      <q-icon name="stars" color="secondary" size="sm" class="q-mr-sm" />
                      <div class="flex-1">
                        <div class="text-caption text-grey-7">Compétences</div>
                        <div class="q-mt-sm">
                          <q-chip
                            v-for="skill in skillsArray"
                            :key="skill"
                            color="secondary"
                            text-color="white"
                            size="sm"
                            class="q-mr-xs q-mb-xs"
                          >
                            {{ skill.trim() }}
                          </q-chip>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="row q-col-gutter-lg q-mt-md">
                  <div v-if="candidate.availability" class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="event_available" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Disponibilité</div>
                        <div class="text-body1">{{ formatDate(candidate.availability) }}</div>
                      </div>
                    </div>
                  </div>

                  <div v-if="candidate.linkedInProfile" class="col-12 col-sm-6">
                    <div class="info-group">
                      <q-icon name="link" color="secondary" size="sm" class="q-mr-sm" />
                      <div>
                        <div class="text-caption text-grey-7">Profil LinkedIn</div>
                        <div class="text-body1">
                          <a
                            :href="candidate.linkedInProfile"
                            target="_blank"
                            class="text-secondary text-decoration-none"
                          >
                            Voir le profil
                            <q-icon name="open_in_new" size="xs" class="q-ml-xs" />
                          </a>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div v-if="candidate.cvPath" class="row q-col-gutter-lg q-mt-md">
                  <div class="col-12">
                    <q-card class="cv-card">
                      <q-card-section class="q-pa-md">
                        <div class="row items-center justify-between">
                          <div class="row items-center q-gutter-sm">
                            <q-icon name="picture_as_pdf" color="red-7" size="32px" />
                            <div>
                              <div class="text-subtitle2">Curriculum Vitae</div>
                              <div class="text-caption text-grey-6">Document PDF disponible</div>
                            </div>
                          </div>
                          <q-btn
                            unelevated
                            color="primary"
                            icon="visibility"
                            label="Consulter le CV"
                            class="cv-action-btn"
                            @mouseenter="startHoverTimer"
                            @mouseleave="clearHoverTimer"
                            @focus="startHoverTimer"
                            @blur="clearHoverTimer"
                            @click="viewCV"
                          >
                            <q-tooltip class="bg-grey-8" :delay="500">
                              Survolez pour un aperçu rapide
                            </q-tooltip>
                          </q-btn>
                        </div>
                      </q-card-section>
                    </q-card>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Colonne droite -->
        <div class="col-12 col-md-4">
          <!-- Adresse -->
          <q-card v-if="candidate.address" class="q-mb-lg info-card">
            <q-card-section>
              <div class="row items-center q-mb-lg">
                <q-icon name="location_on" color="accent" size="40px" class="q-mr-md" />
                <div>
                  <div class="text-h6">Adresse</div>
                  <div class="text-caption text-grey-7">Localisation du candidat</div>
                </div>
              </div>

              <div class="info-content q-pa-md bg-grey-1 rounded-borders">
                <div
                  v-if="candidate.address.streetNumber || candidate.address.streetName"
                  class="info-group q-mb-md"
                >
                  <q-icon name="home" color="accent" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Adresse</div>
                    <div class="text-body1">
                      {{ candidate.address.streetNumber }} {{ candidate.address.streetName }}
                    </div>
                  </div>
                </div>

                <div
                  v-if="candidate.address.city || candidate.address.postalCode"
                  class="info-group q-mb-md"
                >
                  <q-icon name="location_city" color="accent" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Ville</div>
                    <div class="text-body1">
                      {{ candidate.address.postalCode }} {{ candidate.address.city }}
                    </div>
                  </div>
                </div>

                <div v-if="candidate.address.country" class="info-group">
                  <q-icon name="public" color="accent" size="sm" class="q-mr-sm" />
                  <div>
                    <div class="text-caption text-grey-7">Pays</div>
                    <div class="text-body1">{{ candidate.address.country }}</div>
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>

      <!-- Formations -->
      <div
        v-if="candidate.trainings && candidate.trainings.length > 0"
        class="row q-col-gutter-lg q-mt-lg"
      >
        <div class="col-12">
          <q-card class="training-card">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="school" class="q-mr-sm" color="primary" />
                Formations
              </div>

              <div class="row q-gutter-md">
                <div
                  v-for="training in candidate.trainings"
                  :key="training.id!"
                  class="col-12 col-md-6"
                >
                  <q-card class="training-item" flat bordered>
                    <q-card-section>
                      <div class="row items-start">
                        <q-icon name="school" size="md" color="primary" class="q-mr-md q-mt-xs" />
                        <div class="col">
                          <div class="text-h6 text-primary q-mb-xs">{{ training.field }}</div>
                          <div class="text-subtitle2 text-weight-medium q-mb-xs">
                            {{ training.school }}
                          </div>
                          <div class="text-caption text-grey-6 q-mb-sm">
                            <q-icon name="event" size="xs" class="q-mr-xs" />
                            {{ training.period }}
                          </div>
                        </div>
                      </div>
                    </q-card-section>
                  </q-card>
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>

      <!-- Expériences -->
      <div
        v-if="candidate.experiences && candidate.experiences.length > 0"
        class="row q-col-gutter-lg q-mt-lg"
      >
        <div class="col-12">
          <q-card class="experience-card">
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="business_center" class="q-mr-sm" color="secondary" />
                Expériences professionnelles
              </div>

              <div class="q-gutter-md">
                <q-card
                  v-for="(experience, index) in candidate.experiences"
                  :key="experience.id || experience.position"
                  class="experience-item"
                  flat
                  bordered
                >
                  <q-card-section>
                    <div class="row items-start">
                      <div class="experience-timeline q-mr-md">
                        <div class="timeline-dot"></div>
                        <div
                          v-if="index < candidate.experiences.length - 1"
                          class="timeline-line"
                        ></div>
                      </div>
                      <div class="col">
                        <div class="text-h6 text-secondary q-mb-xs">{{ experience.position }}</div>
                        <div class="text-subtitle1 text-weight-medium q-mb-xs">
                          {{ experience.company }}
                        </div>
                        <div class="text-caption text-grey-6 q-mb-sm">
                          <q-icon name="event" size="xs" class="q-mr-xs" />
                          {{ formatDate(experience.startDate) }} -
                          {{ experience.endDate ? formatDate(experience.endDate) : 'En cours' }}
                        </div>
                        <div v-if="experience.description" class="text-body2 q-mb-sm">
                          {{ experience.description }}
                        </div>
                        <div v-if="experience.technologies && experience.technologies.length > 0">
                          <div class="text-caption text-weight-medium q-mb-xs">
                            Technologies utilisées :
                          </div>
                          <q-chip
                            v-for="tech in experience.technologies"
                            :key="tech"
                            color="accent"
                            text-color="white"
                            size="sm"
                            class="q-mr-xs q-mb-xs"
                          >
                            {{ tech }}
                          </q-chip>
                        </div>
                      </div>
                    </div>
                  </q-card-section>
                </q-card>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>

      <!-- Informations complémentaires et Rôles -->
      <div
        v-if="hasAdditionalInfo || (candidate.roles && candidate.roles.length > 0)"
        class="row q-col-gutter-lg q-mt-lg"
      >
        <!-- Informations complémentaires -->
        <div v-if="hasAdditionalInfo" class="col-12 col-md-6">
          <q-card>
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="info" class="q-mr-sm" />
                Informations complémentaires
              </div>

              <div class="q-gutter-md">
                <div v-if="candidate.organizationName">
                  <div class="text-weight-medium">Organisation</div>
                  <div class="text-grey-7">{{ candidate.organizationName }}</div>
                </div>

                <div v-if="candidate.department">
                  <div class="text-weight-medium">Département</div>
                  <div class="text-grey-7">{{ candidate.department }}</div>
                </div>

                <div v-if="candidate.employeeId">
                  <div class="text-weight-medium">ID Employé</div>
                  <div class="text-grey-7">{{ candidate.employeeId }}</div>
                </div>

                <div v-if="candidate.hireDate">
                  <div class="text-weight-medium">Date d'embauche</div>
                  <div class="text-grey-7">{{ formatDate(candidate.hireDate) }}</div>
                </div>

                <div v-if="candidate.updatedAt">
                  <div class="text-weight-medium">Dernière mise à jour</div>
                  <div class="text-grey-7">{{ formatDate(candidate.updatedAt) }}</div>
                </div>
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Rôles -->
        <div v-if="candidate.roles && candidate.roles.length > 0" class="col-12 col-md-6">
          <q-card>
            <q-card-section>
              <div class="text-h6 q-mb-md">
                <q-icon name="security" class="q-mr-sm" />
                Rôles
              </div>

              <div class="q-gutter-xs">
                <q-chip
                  v-for="role in candidate.roles"
                  :key="role"
                  color="accent"
                  text-color="white"
                  size="md"
                  class="q-mr-xs q-mb-xs"
                >
                  {{ role }}
                </q-chip>
              </div>
            </q-card-section>
          </q-card>
        </div>
      </div>
    </div>

    <div v-else class="text-center q-pa-lg">
      <q-icon name="search_off" size="xl" color="grey-5" />
      <div class="text-h6 text-grey-6 q-mt-md">Candidat non trouvé</div>
      <p class="text-grey-6">Le candidat demandé n'existe pas ou n'est plus disponible.</p>
    </div>

    <!-- CV Preview Dialog -->
    <cv-preview-dialog
      v-if="candidate"
      v-model="showCvPreview"
      :candidate-id="candidate.id"
      :candidate-name="candidate.fullName"
      :cv-url="candidate.cvPath!"
    />
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useUserStore } from '../../stores/userStore';
import type { UserDto } from '../../models/user';
import { date } from 'quasar';
import CvPreviewDialog from 'src/components/candidates/CvPreviewDialog.vue';

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();

const showCvPreview = ref(false);
let hoverTimer: NodeJS.Timeout | null = null;

const candidate = ref<UserDto | null>(null);

const skillsArray = computed(() => {
  if (!candidate.value?.skills) return [];
  return candidate.value.skills.split(',').filter((skill) => skill.trim());
});

const hasAdditionalInfo = computed(() => {
  if (!candidate.value) return false;
  return !!(
    candidate.value.organizationName ||
    candidate.value.department ||
    candidate.value.employeeId ||
    candidate.value.hireDate ||
    candidate.value.updatedAt
  );
});

onMounted(async () => {
  const candidateId = route.params.id as string;
  if (candidateId) {
    const userData = await userStore.fetchUserById(candidateId);
    candidate.value = userData!;
  }
});

function formatDate(dateString: string): string {
  if (!dateString) return '';
  return date.formatDate(dateString, 'DD/MM/YYYY');
}

function startHoverTimer() {
  hoverTimer = setTimeout(() => {
    showCvPreview.value = true;
  }, 1000); // 1 second delay
}

function clearHoverTimer() {
  if (hoverTimer) {
    clearTimeout(hoverTimer);
    hoverTimer = null;
  }
}

function viewCV() {
  clearHoverTimer();
  router.push(`/candidates/${candidate.value?.id}/cv`);
}

function contactCandidate() {
  if (candidate.value?.email) {
    window.open(`mailto:${candidate.value.email}`, '_blank');
  }
}
</script>

<style lang="scss" scoped>
.breadcrumb-container {
  background: white;
  border: 1px solid #e0e0e0;
  width: fit-content;
  border-radius: 22px;
}

.breadcrumb-content {
  max-width: 1400px;
  margin: 0 auto;
}

.candidate-content {
  flex: 1;
}

.profile-header-card {
  border-left: 4px solid var(--q-primary);
  background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
}

.info-card {
  border-left: 4px solid var(--q-primary);

  .info-content {
    border-left: 2px solid var(--q-primary);
    margin-left: 20px;
    position: relative;

    &::before {
      content: '';
      position: absolute;
      left: -7px;
      top: 20px;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--q-primary);
      border: 2px solid white;
    }
  }

  .info-group {
    display: flex;
    align-items: flex-start;
    gap: 8px;
  }
}

.info-card:nth-child(2) {
  border-left-color: var(--q-secondary);

  .info-content {
    border-left-color: var(--q-secondary);

    &::before {
      background: var(--q-secondary);
    }
  }
}

.info-card:nth-child(3) {
  border-left-color: var(--q-accent);

  .info-content {
    border-left-color: var(--q-accent);

    &::before {
      background: var(--q-accent);
    }
  }
}

.text-decoration-none {
  text-decoration: none;

  &:hover {
    text-decoration: underline;
  }
}

.rounded-borders {
  border-radius: 8px;
}
.q-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.q-chip {
  font-size: 12px;
}

.training-card,
.experience-card {
  border-left: 4px solid var(--q-primary);
}

.training-card {
  border-left-color: var(--q-positive);
}

.experience-card {
  border-left-color: var(--q-secondary);
}

.training-item,
.experience-item {
  background: white;
  border-radius: 8px;
  transition: transform 0.2s ease;
  border-left: 2px solid var(--q-positive);
  position: relative;
}

.experience-item {
  border-left-color: var(--q-secondary);
}

.training-item:hover,
.experience-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.experience-timeline {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 8px;
}

.timeline-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: var(--q-secondary);
  border: 2px solid white;
}

.timeline-line {
  width: 2px;
  height: 100%;
  background: linear-gradient(to bottom, var(--q-secondary), transparent);
  margin-top: 8px;
  min-height: 50px;
}

.cv-card {
  background: linear-gradient(135deg, #fff5f5 0%, #fff 100%);
  border: 1px solid rgba(244, 67, 54, 0.1);
  transition: all 0.3s ease;

  &:hover {
    box-shadow: 0 4px 12px rgba(244, 67, 54, 0.1);
    transform: translateY(-2px);
  }
}

.cv-action-btn {
  transition: all 0.3s ease;

  &:hover {
    transform: translateX(4px);
  }
}
</style>
