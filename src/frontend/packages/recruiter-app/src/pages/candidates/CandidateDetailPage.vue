<template>
  <q-page padding>
    <div class="row items-center q-mb-lg">
      <q-btn flat round dense icon="arrow_back" class="q-mr-md" @click="$router.back()" />
      <div>
        <h4 class="q-my-none">Profil Candidat</h4>
        <p class="text-grey-6 q-mb-none">Détails complets du candidat</p>
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

    <div v-else-if="candidate" class="row q-gutter-lg justify-center">
      <!-- Informations personnelles -->
      <div class="col-12 col-md-6">
        <q-card>
          <q-card-section>
            <div class="text-h6 q-mb-md">
              <q-icon name="person" class="q-mr-sm" />
              Informations personnelles
            </div>

            <div class="q-gutter-md">
              <div>
                <div class="text-weight-medium">Nom complet</div>
                <div class="text-grey-7">{{ candidate.fullName }}</div>
              </div>

              <div>
                <div class="text-weight-medium">Email</div>
                <div class="text-grey-7">{{ candidate.email }}</div>
              </div>

              <div v-if="candidate.phoneNumber">
                <div class="text-weight-medium">Téléphone</div>
                <div class="text-grey-7">{{ candidate.phoneNumber }}</div>
              </div>

              <div>
                <div class="text-weight-medium">Statut</div>
                <q-chip
                  :color="candidate.isActive ? 'positive' : 'negative'"
                  text-color="white"
                  size="sm"
                >
                  {{ candidate.isActive ? 'Actif' : 'Inactif' }}
                </q-chip>
              </div>

              <div>
                <div class="text-weight-medium">Complétude du profil</div>
                <q-linear-progress
                  :value="candidate.profileCompletionPercentage / 100"
                  color="primary"
                  class="q-mt-xs"
                />
                <div class="text-caption text-grey-6 q-mt-xs">
                  {{ candidate.profileCompletionPercentage }}% complété
                </div>
              </div>

              <div>
                <div class="text-weight-medium">Inscrit le</div>
                <div class="text-grey-7">{{ formatDate(candidate.createdAt) }}</div>
              </div>

              <div v-if="candidate.lastLoginAt">
                <div class="text-weight-medium">Dernière connexion</div>
                <div class="text-grey-7">{{ formatDate(candidate.lastLoginAt) }}</div>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Informations professionnelles -->
      <div class="col-12 col-md-6">
        <q-card>
          <q-card-section>
            <div class="text-h6 q-mb-md">
              <q-icon name="work" class="q-mr-sm" />
              Informations professionnelles
            </div>

            <div class="q-gutter-md">
              <div v-if="candidate.yearsOfExperience">
                <div class="text-weight-medium">Années d'expérience</div>
                <div class="text-grey-7">{{ candidate.yearsOfExperience }} ans</div>
              </div>

              <div v-if="candidate.skills">
                <div class="text-weight-medium">Compétences</div>
                <div class="q-mt-xs">
                  <q-chip
                    v-for="skill in skillsArray"
                    :key="skill"
                    color="primary"
                    text-color="white"
                    size="sm"
                    class="q-mr-xs q-mb-xs"
                  >
                    {{ skill.trim() }}
                  </q-chip>
                </div>
              </div>

              <div v-if="candidate.desiredSalary">
                <div class="text-weight-medium">Salaire souhaité</div>
                <div class="text-grey-7">{{ candidate.desiredSalary.toLocaleString() }} €</div>
              </div>

              <div v-if="candidate.availability">
                <div class="text-weight-medium">Disponibilité</div>
                <div class="text-grey-7">{{ formatDate(candidate.availability) }}</div>
              </div>

              <div v-if="candidate.linkedInProfile">
                <div class="text-weight-medium">Profil LinkedIn</div>
                <a :href="candidate.linkedInProfile" target="_blank" class="text-primary">
                  {{ candidate.linkedInProfile }}
                  <q-icon name="open_in_new" size="xs" class="q-ml-xs" />
                </a>
              </div>

              <div v-if="candidate.cvPath">
                <div class="text-weight-medium">CV</div>
                <q-btn
                  flat
                  color="primary"
                  icon="description"
                  label="Télécharger CV"
                  @click="downloadCV"
                />
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Adresse -->
      <div v-if="candidate.address" class="col-12 col-md-6">
        <q-card>
          <q-card-section>
            <div class="text-h6 q-mb-md">
              <q-icon name="location_on" class="q-mr-sm" />
              Adresse
            </div>

            <div class="q-gutter-md">
              <div v-if="candidate.address.streetNumber || candidate.address.streetName">
                <div class="text-weight-medium">Adresse</div>
                <div class="text-grey-7">
                  {{ candidate.address.streetNumber }} {{ candidate.address.streetName }}
                </div>
              </div>

              <div v-if="candidate.address.addressLine2">
                <div class="text-weight-medium">Complément d'adresse</div>
                <div class="text-grey-7">{{ candidate.address.addressLine2 }}</div>
              </div>

              <div class="row q-gutter-md">
                <div v-if="candidate.address.city" class="col">
                  <div class="text-weight-medium">Ville</div>
                  <div class="text-grey-7">{{ candidate.address.city }}</div>
                </div>

                <div v-if="candidate.address.postalCode" class="col">
                  <div class="text-weight-medium">Code postal</div>
                  <div class="text-grey-7">{{ candidate.address.postalCode }}</div>
                </div>
              </div>

              <div v-if="candidate.address.region">
                <div class="text-weight-medium">Région</div>
                <div class="text-grey-7">{{ candidate.address.region }}</div>
              </div>

              <div v-if="candidate.address.country">
                <div class="text-weight-medium">Pays</div>
                <div class="text-grey-7">{{ candidate.address.country }}</div>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Formations -->
      <div v-if="candidate.trainings && candidate.trainings.length > 0" class="col-12">
        <q-card class="training-card">
          <q-card-section>
            <div class="text-h6 q-mb-md">
              <q-icon name="school" class="q-mr-sm" color="primary" />
              Formations
            </div>

            <div class="row q-gutter-md">
              <div
                v-for="training in candidate.trainings"
                :key="training.id || training.title"
                class="col-12 col-md-6"
              >
                <q-card class="training-item" flat bordered>
                  <q-card-section>
                    <div class="row items-start">
                      <q-icon name="school" size="md" color="primary" class="q-mr-md q-mt-xs" />
                      <div class="col">
                        <div class="text-h6 text-primary q-mb-xs">{{ training.title }}</div>
                        <div class="text-subtitle2 text-weight-medium q-mb-xs">
                          {{ training.institution }}
                        </div>
                        <div class="text-caption text-grey-6 q-mb-sm">
                          <q-icon name="event" size="xs" class="q-mr-xs" />
                          {{ formatDate(training.startDate) }} -
                          {{ training.endDate ? formatDate(training.endDate) : 'En cours' }}
                        </div>
                        <div v-if="training.description" class="text-body2">
                          {{ training.description }}
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

      <!-- Expériences -->
      <div v-if="candidate.experiences && candidate.experiences.length > 0" class="col-12">
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

    <div v-else class="row justify-center q-mt-lg">
      <q-card class="q-pa-md">
        <q-card-section>
          <div class="text-h6">Candidat non trouvé</div>
          <p>Le candidat demandé n'existe pas ou n'est plus disponible.</p>
        </q-card-section>
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute } from 'vue-router';
import { useUserStore } from '../../stores/userStore';
import type { UserDto } from '../../models/user';
import { date } from 'quasar';

const route = useRoute();
const userStore = useUserStore();

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
    candidate.value = userData;
  }
});

function formatDate(dateString: string): string {
  if (!dateString) return '';
  return date.formatDate(dateString, 'DD/MM/YYYY');
}

function downloadCV() {
  if (candidate.value?.cvPath) {
    // Logique pour télécharger le CV
    console.log('Download CV:', candidate.value.cvPath);
  }
}
</script>

<style scoped>
.q-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.q-chip {
  font-size: 12px;
}

.training-card,
.experience-card {
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.training-item {
  background: white;
  border-radius: 8px;
  transition: transform 0.2s ease;
}

.training-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.experience-item {
  background: white;
  border-radius: 8px;
  transition: transform 0.2s ease;
  position: relative;
}

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
  box-shadow: 0 0 0 3px rgba(109, 40, 217, 0.2);
}

.timeline-line {
  width: 2px;
  height: 100%;
  background: linear-gradient(to bottom, var(--q-secondary), transparent);
  margin-top: 8px;
  min-height: 50px;
}
</style>
