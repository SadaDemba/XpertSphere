<template>
  <q-page class="profile-page">
    <!-- Header -->
    <div class="profile-header q-pa-lg">
      <div class="header-content">
        <div class="breadcrumb-container q-pa-md q-mb-md">
          <q-breadcrumbs class="text-grey-7" active-color="primary">
            <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
            <q-breadcrumbs-el label="Mon profil" icon="person" />
          </q-breadcrumbs>
        </div>

        <div class="profile-header-info">
          <div class="row items-center q-gutter-md">
            <q-avatar size="120px" class="profile-avatar">
              <img
                v-if="user?.profilePictureUrl"
                :src="user.profilePictureUrl"
                alt="photo de profil"
              />
              <q-icon v-else name="person" size="64px" color="white" />
            </q-avatar>

            <div class="col">
              <h4 class="text-h4 q-my-none text-white">{{ user?.fullName }}</h4>
              <p class="text-h6 q-my-sm text-grey-3">{{ user?.email }}</p>
              <div class="chips-container q-mt-md">
                <q-chip
                  v-if="user?.yearsOfExperience"
                  color="white"
                  text-color="primary"
                  icon="work"
                >
                  {{ user.yearsOfExperience }} ans d'expérience
                </q-chip>
                <q-chip color="white" text-color="primary" icon="location_on" class="q-ma-xs">
                  {{ user?.address?.city }}, {{ user?.address?.country }}
                </q-chip>
                <q-chip color="white" text-color="positive" icon="check_circle" class="q-ma-xs">
                  Profil complet à {{ user?.profileCompletionPercentage || 0 }}%
                </q-chip>
              </div>
            </div>

            <div class="col-auto">
              <q-btn
                color="white"
                text-color="primary"
                icon="edit"
                :label="$q.screen.gt.sm ? 'Modifier le profil' : undefined"
                @click="showEditProfileDialog = true"
              />
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="profile-content-container">
      <div class="profile-content">
        <div class="content-grid q-pa-lg">
          <!-- Main Content -->
          <div class="main-content">
            <!-- Personal Information -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">
                  <q-icon name="person" class="q-mr-sm" />
                  Informations personnelles
                </h6>

                <div class="info-grid">
                  <div class="info-item">
                    <div class="info-label">Nom complet</div>
                    <div class="info-value">{{ user?.fullName }}</div>
                  </div>

                  <div class="info-item">
                    <div class="info-label">Email</div>
                    <div class="info-value">{{ user?.email }}</div>
                  </div>

                  <div class="info-item">
                    <div class="info-label">Téléphone</div>
                    <div class="info-value">{{ user?.phoneNumber || 'Non renseigné' }}</div>
                  </div>

                  <div class="info-item">
                    <div class="info-label">LinkedIn</div>
                    <div class="info-value">
                      <a
                        v-if="user?.linkedInProfile"
                        :href="user.linkedInProfile"
                        target="_blank"
                        class="text-primary"
                      >
                        Voir le profil
                      </a>
                      <span v-else class="text-grey">Non renseigné</span>
                    </div>
                  </div>

                  <div class="info-item">
                    <div class="info-label">Disponibilité</div>
                    <div v-if="user?.availability" class="info-value">
                      {{ formatDateWithoutTime(user?.availability) }}
                    </div>
                    <div v-else class="info-value">Non renseignée</div>
                  </div>

                  <div class="info-item">
                    <div class="info-label">Salaire souhaité</div>
                    <div class="info-value">{{ formatSalary(user?.desiredSalary) }}</div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!-- Skills -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <div class="row justify-between items-center q-mb-md">
                  <h6 class="text-h6 q-mt-none q-mb-none">
                    <q-icon name="psychology" class="q-mr-sm" />
                    Compétences
                  </h6>
                  <div v-if="!isEditingSkills">
                    <q-btn
                      flat
                      color="primary"
                      icon="edit"
                      label="Modifier"
                      @click="startEditingSkills"
                    />
                  </div>
                  <div v-else class="q-gutter-sm">
                    <q-btn flat color="positive" icon="check" label="Valider" @click="saveSkills" />
                    <q-btn
                      flat
                      color="negative"
                      icon="close"
                      label="Annuler"
                      @click="cancelEditingSkills"
                    />
                  </div>
                </div>

                <!-- View mode -->
                <div v-if="!isEditingSkills">
                  <div class="skills-display">
                    <template v-for="(skill, index) in skillsList" :key="`skill-${index}`">
                      <!-- Category header -->
                      <div v-if="isCategory(skill)" class="skill-category q-mt-md q-mb-sm">
                        <div class="text-subtitle2 text-weight-medium text-primary">
                          {{ getCategoryName(skill) }}
                        </div>
                      </div>
                      <!-- Regular skill chip -->
                      <q-chip
                        v-else
                        color="primary"
                        text-color="white"
                        class="skill-chip q-mr-sm q-mb-sm"
                        size="md"
                      >
                        {{ skill }}
                      </q-chip>
                    </template>
                  </div>
                  <div v-if="skillsList.length === 0" class="text-grey text-center q-py-lg">
                    Aucune compétence renseignée
                  </div>
                </div>

                <!-- Edit mode -->
                <div v-else>
                  <q-input
                    v-model="editableSkills"
                    label="Compétences"
                    type="textarea"
                    filled
                    rows="6"
                  />
                  <div class="text-caption text-black q-mt-sm">
                    <div class="q-mb-xs"><strong>Formats supportés :</strong></div>
                    <div class="q-mb-xs">• <strong>Simple :</strong> Dart, Ruby, Java, Docker</div>
                    <div>• <strong>Par catégories :</strong></div>
                    <div class="q-ml-md text-black">
                      Frameworks: Angular, Spring, Express.js<br />
                      Languages: Dart, Ruby, Java
                    </div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!-- Experience -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <div class="row justify-between items-center q-mb-md">
                  <h6 class="text-h6 q-mt-none q-mb-none">
                    <q-icon name="work" class="q-mr-sm" />
                    Expériences professionnelles
                  </h6>
                  <div v-if="!isEditingExperiences">
                    <q-btn
                      flat
                      color="primary"
                      icon="edit"
                      label="Modifier"
                      @click="startEditingExperiences"
                    />
                  </div>
                  <div v-else class="q-gutter-sm">
                    <q-btn
                      flat
                      color="positive"
                      icon="check"
                      label="Valider"
                      @click="saveExperiences"
                    />
                    <q-btn
                      flat
                      color="negative"
                      icon="close"
                      label="Annuler"
                      @click="cancelEditingExperiences"
                    />
                  </div>
                </div>

                <!-- View mode -->
                <q-timeline v-if="!isEditingExperiences" color="primary">
                  <q-timeline-entry
                    v-for="(exp, index) in user?.experiences || []"
                    :key="index"
                    :title="exp.title"
                    :subtitle="exp.date"
                    icon="business"
                  >
                    <div class="text-weight-medium">{{ exp.company }}</div>
                    <div v-if="exp.location" class="text-caption text-grey q-mb-sm">
                      <q-icon name="location_on" size="16px" /> {{ exp.location }}
                    </div>
                    <div class="q-mt-sm">{{ exp.description }}</div>
                  </q-timeline-entry>
                </q-timeline>

                <!-- Edit mode -->
                <div v-else>
                  <div
                    v-for="(exp, index) in editableExperiences"
                    :key="index"
                    class="q-mb-md q-pa-md bg-grey-1 rounded-borders"
                  >
                    <div class="row justify-between items-start q-mb-md">
                      <div class="text-h6">Expérience {{ index + 1 }}</div>
                      <q-btn
                        flat
                        round
                        dense
                        icon="delete"
                        color="negative"
                        @click="removeExperience(index)"
                      />
                    </div>

                    <q-input v-model="exp.title" label="Titre du poste" filled class="q-mb-md" />

                    <q-input v-model="exp.company" label="Entreprise" filled class="q-mb-md" />

                    <q-input v-model="exp.location" label="Localisation" filled class="q-mb-md" />

                    <q-input v-model="exp.date" label="Période" filled class="q-mb-md" />

                    <q-input
                      v-model="exp.description"
                      label="Description"
                      type="textarea"
                      filled
                      rows="3"
                    />
                  </div>

                  <q-btn
                    color="primary"
                    icon="add"
                    label="Ajouter une expérience"
                    class="q-mt-md"
                    @click="addExperience"
                  />
                </div>
              </q-card-section>
            </q-card>

            <!-- Training -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <div class="row justify-between items-center q-mb-md">
                  <h6 class="text-h6 q-mt-none q-mb-none">
                    <q-icon name="school" class="q-mr-sm" />
                    Formations
                  </h6>
                  <div v-if="!isEditingTrainings">
                    <q-btn
                      flat
                      color="primary"
                      icon="edit"
                      label="Modifier"
                      @click="startEditingTrainings"
                    />
                  </div>
                  <div v-else class="q-gutter-sm">
                    <q-btn
                      flat
                      color="positive"
                      icon="check"
                      label="Valider"
                      @click="saveTrainings"
                    />
                    <q-btn
                      flat
                      color="negative"
                      icon="close"
                      label="Annuler"
                      @click="cancelEditingTrainings"
                    />
                  </div>
                </div>

                <!-- View mode -->
                <div v-if="!isEditingTrainings" class="trainings-list">
                  <div
                    v-for="(training, index) in user?.trainings || []"
                    :key="index"
                    class="training-item"
                  >
                    <div class="row items-start q-gutter-md">
                      <q-icon name="school" size="32px" color="primary" />
                      <div class="col">
                        <div class="text-h6">{{ training.field }}</div>
                        <div class="text-weight-medium">{{ training.level }}</div>
                        <div class="text-caption text-grey">{{ training.school }}</div>
                        <div class="text-caption text-grey">{{ training.period }}</div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Edit mode -->
                <div v-else>
                  <div
                    v-for="(training, index) in editableTrainings"
                    :key="index"
                    class="q-mb-md q-pa-md bg-grey-1 rounded-borders"
                  >
                    <div class="row justify-between items-start q-mb-md">
                      <div class="text-h6">Formation {{ index + 1 }}</div>
                      <q-btn
                        flat
                        round
                        dense
                        icon="delete"
                        color="negative"
                        @click="removeTraining(index)"
                      />
                    </div>

                    <q-input v-model="training.field" label="Domaine" filled class="q-mb-md" />

                    <q-input v-model="training.level" label="Niveau" filled class="q-mb-md" />

                    <q-input
                      v-model="training.school"
                      label="École / Université"
                      filled
                      class="q-mb-md"
                    />

                    <q-input v-model="training.period" label="Période" filled class="q-mb-md" />
                  </div>

                  <q-btn
                    color="primary"
                    icon="add"
                    label="Ajouter une formation"
                    class="q-mt-md"
                    @click="addTraining"
                  />
                </div>
              </q-card-section>
            </q-card>
          </div>

          <!-- Sidebar -->
          <div class="sidebar">
            <!-- Quick Stats -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">Statistiques</h6>

                <div class="stats-list">
                  <div class="stat-item">
                    <q-icon name="work" color="primary" size="24px" />
                    <div class="stat-info">
                      <div class="stat-value">{{ totalApplications }}</div>
                      <div class="stat-label">Candidatures envoyées</div>
                    </div>
                  </div>

                  <div class="stat-item">
                    <q-icon name="visibility" color="orange" size="24px" />
                    <div class="stat-info">
                      <div class="stat-value">{{ profileViews }}</div>
                      <div class="stat-label">Vues du profil</div>
                    </div>
                  </div>

                  <div class="stat-item">
                    <q-icon name="thumb_up" color="positive" size="24px" />
                    <div class="stat-info">
                      <div class="stat-value">{{ interviewsObtained }}</div>
                      <div class="stat-label">Entretiens obtenus</div>
                    </div>
                  </div>
                </div>
              </q-card-section>
            </q-card>

            <!-- Profile Completion -->
            <q-card class="q-mb-lg">
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">Complétude du profil</h6>

                <div class="text-center q-mb-md">
                  <q-circular-progress
                    :value="user?.profileCompletionPercentage || 0"
                    size="120px"
                    :thickness="0.2"
                    color="primary"
                    track-color="grey-3"
                    class="q-ma-md"
                  >
                    <div class="text-h6">{{ user?.profileCompletionPercentage || 0 }}%</div>
                  </q-circular-progress>
                </div>

                <div class="completion-tips">
                  <p class="text-caption text-grey q-mb-sm">Pour améliorer votre profil :</p>
                  <ul class="text-caption q-pl-md">
                    <li>Ajoutez une photo de profil</li>
                    <li>Complétez vos compétences</li>
                    <li>Renseignez votre LinkedIn</li>
                  </ul>
                </div>
              </q-card-section>
            </q-card>

            <!-- Quick Actions -->
            <q-card>
              <q-card-section>
                <h6 class="text-h6 q-mt-none q-mb-md">Actions rapides</h6>

                <div class="q-gutter-sm">
                  <q-btn
                    v-if="user?.cvPath"
                    color="primary"
                    icon="download"
                    label="Télécharger mon CV"
                    class="full-width"
                    @click="downloadCV"
                  />
                  <q-btn
                    v-else
                    color="primary"
                    icon="upload"
                    label="Ajouter un CV"
                    class="full-width"
                    disabled
                  />
                  <q-btn
                    outline
                    color="primary"
                    icon="share"
                    label="Partager mon profil"
                    class="full-width"
                  />
                  <q-btn
                    flat
                    color="negative"
                    icon="delete"
                    label="Supprimer mon compte"
                    class="full-width"
                  />
                </div>
              </q-card-section>
            </q-card>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Profile Dialog -->
    <EditProfileDialog
      v-model="showEditProfileDialog"
      :user="user"
      @profile-updated="onProfileUpdated"
    />
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useQuasar } from 'quasar';
import { useAuthStore } from '../stores/authStore';
import { useApplicationStore } from '../stores/applicationStore';
import { useExperienceStore } from '../stores/experienceStore';
import { useUserStore } from '../stores/userStore';
import { ApplicationStatus } from '../enums';
import { formatDateWithoutTime } from 'src/helpers/DateHelper';
import type { Experience, Training, User } from '../models/auth';
import type { CreateExperienceDto } from '../models/experience';
import { useNotification } from 'src/composables/notification';
import EditProfileDialog from '../components/EditProfileDialog.vue';

// Use real user data from auth store
const $q = useQuasar();
const authStore = useAuthStore();
const applicationStore = useApplicationStore();
const experienceStore = useExperienceStore();
const userStore = useUserStore();
const notification = useNotification();
const user = computed(() => authStore.user);

// Edit mode states
const isEditingExperiences = ref(false);
const isEditingTrainings = ref(false);
const isEditingSkills = ref(false);
const showEditProfileDialog = ref(false);
const editableExperiences = ref<Experience[]>([]);
const editableTrainings = ref<Training[]>([]);
const editableSkills = ref('');

// Fetch applications on component mount
onMounted(() => {
  applicationStore.fetchMyApplications();
});

// Computed
const skillsList = computed(() => {
  if (!user.value?.skills) return [];

  const skillsText = user.value.skills.trim();

  // Check if it's structured format (contains ":")
  if (skillsText.includes(':')) {
    return parseStructuredSkills(skillsText);
  } else {
    // Simple comma-separated format
    return skillsText
      .split(',')
      .map((skill) => skill.trim())
      .filter((skill) => skill.length > 0);
  }
});

// Parse structured skills format
const parseStructuredSkills = (skillsText: string) => {
  const skills: string[] = [];
  const lines = skillsText.split('\n');

  for (const line of lines) {
    const trimmedLine = line.trim();
    if (trimmedLine.includes(':')) {
      const parts = trimmedLine.split(':', 2);
      const category = parts[0];
      const items = parts[1];

      if (category && items) {
        const categoryName = category.trim();
        const skillItems = items
          .split(',')
          .map((item) => item.trim())
          .filter((item) => item.length > 0);

        // Add category as a header
        if (categoryName) {
          skills.push(`__CATEGORY__${categoryName}`);
        }

        // Add individual skills
        skills.push(...skillItems);
      }
    } else if (trimmedLine) {
      // Handle lines without category
      skills.push(trimmedLine);
    }
  }

  return skills;
};

const isCategory = (skill: string) => skill.startsWith('__CATEGORY__');
const getCategoryName = (skill: string) => skill.replace('__CATEGORY__', '');

// Statistics computed properties
const totalApplications = computed(() => applicationStore.applications.length);

const interviewsObtained = computed(() => {
  return applicationStore.applications.filter(
    (app) =>
      app.currentStatus === ApplicationStatus.PhoneScreening ||
      app.currentStatus === ApplicationStatus.TechnicalInterview ||
      app.currentStatus === ApplicationStatus.FinalInterview ||
      app.currentStatus === ApplicationStatus.OfferMade ||
      app.currentStatus === ApplicationStatus.Accepted,
  ).length;
});

// Mock profile views for now (as requested)
const profileViews = computed(() => 15);

// Methods
const formatSalary = (salary?: number) => {
  if (!salary) return 'Non renseigné';
  return (
    new Intl.NumberFormat('fr-FR', {
      style: 'currency',
      currency: 'EUR',
      maximumFractionDigits: 0,
    }).format(salary) + ' / an'
  );
};

const downloadCV = () => {
  if (user.value?.cvPath) {
    // Open CV in a new tab/window for download
    window.open(user.value.cvPath, '_blank');
  }
};

// Experience editing methods
const startEditingExperiences = () => {
  isEditingExperiences.value = true;
  editableExperiences.value = user.value?.experiences ? [...user.value.experiences] : [];
};

const cancelEditingExperiences = () => {
  isEditingExperiences.value = false;
  editableExperiences.value = [];
};

const addExperience = () => {
  editableExperiences.value.push({
    title: '',
    company: '',
    location: '',
    date: '',
    description: '',
  });
};

const removeExperience = (index: number) => {
  editableExperiences.value.splice(index, 1);
};

const saveExperiences = async () => {
  if (!user.value?.id) return;

  const experienceDtos: CreateExperienceDto[] = editableExperiences.value.map((exp) => ({
    userId: user.value!.id,
    title: exp.title,
    description: exp.description,
    location: exp.location || '',
    company: exp.company,
    date: exp.date,
    isCurrent: false, // You can add a checkbox for this if needed
  }));

  const success = await experienceStore.replaceUserExperiences(user.value.id, experienceDtos);
  if (success) {
    // Update user experiences in auth store
    if (user.value) {
      user.value.experiences = editableExperiences.value;
    }
    isEditingExperiences.value = false;
    notification.showSuccessNotification('Expériences mises à jour avec succès');
  }
};

// Training editing methods
const startEditingTrainings = () => {
  isEditingTrainings.value = true;
  editableTrainings.value = user.value?.trainings ? [...user.value.trainings] : [];
};

const cancelEditingTrainings = () => {
  isEditingTrainings.value = false;
  editableTrainings.value = [];
};

const addTraining = () => {
  editableTrainings.value.push({
    school: '',
    level: '',
    period: '',
    field: '',
  });
};

const removeTraining = (index: number) => {
  editableTrainings.value.splice(index, 1);
};

const saveTrainings = async () => {
  if (!user.value?.id) return;

  // For now, we'll need to create a training service similar to experience service
  // Since it's not implemented yet, we'll just update locally
  if (user.value) {
    user.value.trainings = editableTrainings.value;
  }
  isEditingTrainings.value = false;
  notification.showSuccessNotification('Formations mises à jour avec succès');
};

// Skills editing methods
const startEditingSkills = () => {
  isEditingSkills.value = true;
  editableSkills.value = user.value?.skills || '';
};

const cancelEditingSkills = () => {
  isEditingSkills.value = false;
  editableSkills.value = '';
};

const saveSkills = async () => {
  if (!user.value?.id) return;

  // Show confirmation dialog
  const confirmed = await notification.showConfirmDialog(
    'Confirmer la modification',
    'Êtes-vous sûr de vouloir modifier vos compétences ?',
  );

  if (!confirmed) return;

  const success = await userStore.updateUserSkills(user.value.id, {
    skills: editableSkills.value,
  });

  if (success) {
    // Update user skills in auth store
    if (user.value) {
      user.value.skills = editableSkills.value;
    }
    isEditingSkills.value = false;
  }
};

// Handle profile update from dialog
const onProfileUpdated = (updatedUser: User) => {
  // Update the user in auth store
  authStore.setUser(updatedUser);
};
</script>

<style lang="scss" scoped>
.profile-page {
  min-height: 100vh;
  background-color: rgb(233, 233, 233);
}

.profile-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding-bottom: 80px;
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
}

.chips-container {
  display: flex;
  gap: 4px;
}

@media (max-width: 599px) {
  .profile-header-info .row {
    flex-direction: column;
    align-items: flex-start !important;
  }

  .profile-avatar {
    margin-bottom: 1rem;
  }

  .chips-container {
    margin-top: 0.5rem;
  }
}

.breadcrumb-container {
  background: white;
  border-radius: 22px;
  width: fit-content;
}

.profile-avatar {
  border: 4px solid white;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.profile-content-container {
  width: 100%;
  margin-top: -60px;
  position: relative;
  z-index: 1;
}

.profile-content {
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
}

.content-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;

  @media (min-width: 992px) {
    grid-template-columns: 2fr 1fr;
  }
}

.main-content {
  min-width: 0;
}

.sidebar {
  min-width: 0;

  @media (min-width: 992px) {
    position: sticky;
    top: 20px;
    height: fit-content;
  }
}

.info-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;

  @media (min-width: 768px) {
    grid-template-columns: 1fr 1fr;
  }
}

.info-item {
  .info-label {
    font-size: 0.85rem;
    color: #6c757d;
    margin-bottom: 4px;
  }

  .info-value {
    font-size: 1rem;
    color: #2c3e50;
    font-weight: 500;
  }
}

.skills-container {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.skills-display {
  .skill-category {
    .text-subtitle2 {
      border-bottom: 1px solid #e0e0e0;
      padding-bottom: 4px;
      margin-bottom: 8px;
    }

    &:first-child {
      margin-top: 0 !important;
    }
  }
}

.skill-chip {
  font-size: 0.85rem;
}

.training-item {
  padding: 16px 0;
  border-bottom: 1px solid #e0e0e0;

  &:last-child {
    border-bottom: none;
  }

  &:first-child {
    padding-top: 0;
  }
}

.stats-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 16px;

  .stat-info {
    flex: 1;
  }

  .stat-value {
    font-size: 1.5rem;
    font-weight: bold;
    color: #2c3e50;
  }

  .stat-label {
    font-size: 0.85rem;
    color: #6c757d;
  }
}

.completion-tips {
  ul {
    margin: 0;
    padding-left: 20px;
  }

  li {
    margin-bottom: 4px;
  }
}

q-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}
</style>
