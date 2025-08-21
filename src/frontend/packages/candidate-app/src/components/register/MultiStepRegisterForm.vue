<template>
  <div class="multi-step-form">
    <!-- Timeline -->
    <q-stepper
      ref="stepper"
      v-model="currentStep"
      color="primary"
      animated
      flat
      alternative-labels
      :bordered="false"
      class="bg-transparent"
    >
      <!-- Step 1: CV Upload -->
      <q-step
        :name="1"
        title="CV (Optionnel)"
        icon="description"
        :done="currentStep > 1"
        :error="stepErrors[1]"
      >
        <div class="step-content">
          <div class="text-center q-mb-lg">
            <q-icon name="upload_file" size="64px" color="primary" />
            <h6 class="q-mt-md q-mb-sm">Téléchargement de CV</h6>
            <p class="text-grey-7">Téléchargez votre CV (obligatoire)</p>
          </div>

          <q-file
            v-model="cvFile"
            accept=".pdf"
            max-file-size="5242880"
            filled
            label="Sélectionner un CV (PDF uniquement) *"
            :loading="isAnalyzing"
            :disable="isAnalyzing"
            :rules="[(val) => !!val || 'CV requis']"
          >
            <template #prepend>
              <q-icon name="attach_file" />
            </template>
            <template v-if="cvFile" #append>
              <q-btn flat round dense icon="close" @click="cvFile = null" />
            </template>
          </q-file>

          <q-toggle
            v-model="shouldAnalyzeCV"
            label="Analyser automatiquement le CV pour pré-remplir le formulaire"
            class="q-mt-md"
            :disable="!cvFile"
          />

          <q-banner v-if="cvAnalyzed" class="bg-positive text-white q-mt-md" rounded>
            <template #avatar>
              <q-icon name="check_circle" />
            </template>
            CV analysé avec succès ! Les informations seront pré-remplies.
          </q-banner>

          <div v-if="isAnalyzing" class="text-center q-mt-lg">
            <q-spinner color="primary" size="32px" />
            <p class="q-mt-md text-grey-7">Analyse du CV en cours...</p>
          </div>
        </div>
      </q-step>

      <!-- Step 2: Personal Info -->
      <q-step
        :name="2"
        title="Informations personnelles"
        icon="person"
        :done="currentStep > 2"
        :error="stepErrors[2]"
      >
        <div class="step-content">
          <h6 class="q-mt-none q-mb-md">Informations personnelles</h6>

          <div class="row q-col-gutter-md">
            <div class="col-12 col-sm-6">
              <q-input
                v-model="formData.firstName"
                label="Prénom *"
                filled
                :rules="[(val) => !!val || 'Prénom requis']"
              />
            </div>
            <div class="col-12 col-sm-6">
              <q-input
                v-model="formData.lastName"
                label="Nom *"
                filled
                :rules="[(val) => !!val || 'Nom requis']"
              />
            </div>
          </div>

          <q-input v-model="formData.phoneNumber" label="Téléphone" filled class="q-mt-md" />

          <h6 class="q-mt-lg q-mb-md">Adresse</h6>

          <div class="row q-col-gutter-md">
            <div class="col-12 col-sm-3">
              <q-input v-model="formData.streetNumber" label="N°" filled />
            </div>
            <div class="col-12 col-sm-9">
              <q-input v-model="formData.street" label="Rue" filled />
            </div>
          </div>

          <div class="row q-col-gutter-md q-mt-sm">
            <div class="col-12 col-sm-6">
              <q-input v-model="formData.city" label="Ville" filled />
            </div>
            <div class="col-12 col-sm-6">
              <q-input v-model="formData.postalCode" label="Code postal" filled />
            </div>
          </div>

          <div class="row q-col-gutter-md q-mt-sm">
            <div class="col-12 col-sm-6">
              <q-input v-model="formData.region" label="Région" filled />
            </div>
            <div class="col-12 col-sm-6">
              <q-input v-model="formData.country" label="Pays" filled />
            </div>
          </div>
        </div>
      </q-step>

      <!-- Step 3: Professional Info -->
      <q-step
        :name="3"
        title="Informations professionnelles"
        icon="work"
        :done="currentStep > 3"
        :error="stepErrors[3]"
      >
        <div class="step-content">
          <h6 class="q-mt-none q-mb-md">Informations professionnelles</h6>

          <div class="row q-col-gutter-md">
            <div class="col-12 col-sm-6">
              <q-input
                v-model.number="formData.yearsOfExperience"
                type="number"
                label="Années d'expérience"
                filled
                min="0"
                max="50"
              />
            </div>
            <div class="col-12 col-sm-6">
              <q-input
                v-model.number="formData.desiredSalary"
                type="number"
                label="Salaire souhaité (€/an)"
                filled
                min="0"
              />
            </div>
          </div>

          <q-input
            v-model="formData.availability"
            label="Disponibilité"
            filled
            class="q-mt-md"
            hint="Ex: Immédiate, 1 mois, 2 mois..."
          />

          <q-input
            v-model="formData.linkedInProfile"
            label="Profil LinkedIn"
            filled
            class="q-mt-md"
            placeholder="https://linkedin.com/in/votre-profil"
          />

          <q-input
            v-model="formData.skills"
            type="textarea"
            label="Compétences"
            filled
            rows="4"
            class="q-mt-md"
            placeholder="Ex: JavaScript, React, Node.js, Python..."
          />
        </div>
      </q-step>

      <!-- Step 4: Training -->
      <q-step
        :name="4"
        title="Formations"
        icon="school"
        :done="currentStep > 4"
        :error="stepErrors[4]"
      >
        <div class="step-content">
          <h6 class="q-mt-none q-mb-md">Formations</h6>

          <div
            v-for="(training, index) in formData.trainings"
            :key="index"
            class="training-card q-mb-md"
          >
            <q-card>
              <q-card-section>
                <div class="row items-center q-mb-md">
                  <div class="col">
                    <div class="text-h6">Formation {{ index + 1 }}</div>
                  </div>
                  <div class="col-auto">
                    <q-btn
                      flat
                      round
                      dense
                      icon="delete"
                      color="negative"
                      @click="removeTraining(index)"
                    />
                  </div>
                </div>

                <div class="row q-col-gutter-md">
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="training.school"
                      label="École/Université *"
                      filled
                      :rules="[(val) => !!val || 'École requise']"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="training.level"
                      label="Niveau *"
                      filled
                      :rules="[(val) => !!val || 'Niveau requis']"
                      hint="Ex: Licence, Master, Doctorat..."
                    />
                  </div>
                </div>

                <div class="row q-col-gutter-md q-mt-sm">
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="training.field"
                      label="Domaine *"
                      filled
                      :rules="[(val) => !!val || 'Domaine requis']"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="training.period"
                      label="Période *"
                      filled
                      :rules="[(val) => !!val || 'Période requise']"
                      hint="Ex: 2020-2022"
                    />
                  </div>
                </div>
              </q-card-section>
            </q-card>
          </div>

          <q-btn
            flat
            icon="add"
            label="Ajouter une formation"
            color="primary"
            :disable="!canAddTraining"
            @click="addTraining"
          />
        </div>
      </q-step>

      <!-- Step 5: Experience -->
      <q-step
        :name="5"
        title="Expériences"
        icon="business_center"
        :done="currentStep > 5"
        :error="stepErrors[5]"
      >
        <div class="step-content">
          <h6 class="q-mt-none q-mb-md">Expériences professionnelles</h6>

          <div
            v-for="(experience, index) in formData.experiences"
            :key="index"
            class="experience-card q-mb-md"
          >
            <q-card>
              <q-card-section>
                <div class="row items-center q-mb-md">
                  <div class="col">
                    <div class="text-h6">Expérience {{ index + 1 }}</div>
                  </div>
                  <div class="col-auto">
                    <q-btn
                      flat
                      round
                      dense
                      icon="delete"
                      color="negative"
                      @click="removeExperience(index)"
                    />
                  </div>
                </div>

                <div class="row q-col-gutter-md">
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="experience.title"
                      label="Poste *"
                      filled
                      :rules="[(val) => !!val || 'Poste requis']"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="experience.company"
                      label="Entreprise *"
                      filled
                      :rules="[(val) => !!val || 'Entreprise requise']"
                    />
                  </div>
                </div>

                <div class="row q-col-gutter-md q-mt-sm">
                  <div class="col-12 col-sm-6">
                    <q-input v-model="experience.location" label="Lieu" filled />
                  </div>
                  <div class="col-12 col-sm-6">
                    <q-input
                      v-model="experience.date"
                      label="Période *"
                      filled
                      :rules="[(val) => !!val || 'Période requise']"
                      hint="Ex: 01/2020 - 12/2022"
                    />
                  </div>
                </div>

                <q-input
                  v-model="experience.description"
                  type="textarea"
                  label="Description *"
                  filled
                  rows="3"
                  class="q-mt-md"
                  :rules="[(val) => !!val || 'Description requise']"
                />
              </q-card-section>
            </q-card>
          </div>

          <q-btn
            flat
            icon="add"
            label="Ajouter une expérience"
            color="primary"
            :disable="!canAddExperience"
            @click="addExperience"
          />
        </div>
      </q-step>

      <!-- Step 6: Account -->
      <q-step :name="6" title="Compte" icon="lock" :done="currentStep > 6" :error="stepErrors[6]">
        <div class="step-content">
          <h6 class="q-mt-none q-mb-md">Informations de compte</h6>

          <q-input
            v-model="formData.email"
            type="email"
            label="Email *"
            filled
            :rules="[(val) => !!val || 'Email requis', validateEmail]"
          />

          <div class="row q-col-gutter-md q-mt-md">
            <div class="col-12 col-sm-6">
              <q-input
                v-model="formData.password"
                :type="showPassword ? 'text' : 'password'"
                label="Mot de passe *"
                filled
                :rules="passwordRules"
              >
                <template #append>
                  <q-icon
                    :name="showPassword ? 'visibility_off' : 'visibility'"
                    class="cursor-pointer"
                    @click="showPassword = !showPassword"
                  />
                </template>
              </q-input>
            </div>
            <div class="col-12 col-sm-6">
              <q-input
                v-model="formData.confirmPassword"
                :type="showConfirmPassword ? 'text' : 'password'"
                label="Confirmer le mot de passe *"
                filled
                :rules="[
                  (val) => !!val || 'Confirmation requise',
                  (val) => val === formData.password || 'Les mots de passe ne correspondent pas',
                ]"
              >
                <template #append>
                  <q-icon
                    :name="showConfirmPassword ? 'visibility_off' : 'visibility'"
                    class="cursor-pointer"
                    @click="showConfirmPassword = !showConfirmPassword"
                  />
                </template>
              </q-input>
            </div>
          </div>

          <div class="q-mt-lg">
            <q-checkbox
              v-model="formData.acceptTerms"
              label="J'accepte les conditions d'utilisation"
              :rules="[(val: any) => val || 'Vous devez accepter les conditions']"
            />
            <q-checkbox
              v-model="formData.acceptPrivacyPolicy"
              label="J'accepte la politique de confidentialité"
              :rules="[(val: any) => val || 'Vous devez accepter la politique']"
            />
          </div>
        </div>
      </q-step>

      <!-- Navigation buttons -->
      <template #navigation>
        <q-stepper-navigation class="flex justify-between q-mt-lg">
          <q-btn
            v-if="currentStep > 1"
            flat
            color="primary"
            label="Précédent"
            @click="previousStep"
          />
          <q-space v-if="currentStep === 1" />

          <q-btn
            v-if="currentStep < 6"
            color="primary"
            :label="
              currentStep === 1 && cvFile && shouldAnalyzeCV ? 'Analyser et continuer' : 'Suivant'
            "
            :loading="isAnalyzing || isLoading"
            :disable="!canProceed"
            @click="nextStep"
          />

          <q-btn
            v-if="currentStep === 6"
            color="primary"
            label="Créer mon compte"
            :loading="isLoading"
            :disable="!canSubmit"
            @click="submitForm"
          />
        </q-stepper-navigation>
      </template>
    </q-stepper>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '../../stores/authStore';
import { useNotification } from '../../composables/notification';
import type {
  RegisterCandidateDto,
  Training,
  Experience,
  ResumeAnalysisResponse,
} from '../../models/auth';

// Emits
const emit = defineEmits<{
  (e: 'submit', data: RegisterCandidateDto, cvFile: File | null): void;
}>();

// Composables
const authStore = useAuthStore();
const { isLoading } = storeToRefs(authStore);
const notification = useNotification();

// State
const currentStep = ref(1);
const cvFile = ref<File | null>(null);
const cvAnalyzed = ref(false);
const shouldAnalyzeCV = ref(true);
const isAnalyzing = ref(false);
const showPassword = ref(false);
const showConfirmPassword = ref(false);
const stepErrors = ref<Record<number, boolean>>({});

// Form data
const formData = reactive<RegisterCandidateDto>({
  // Account
  email: '',
  password: '',
  confirmPassword: '',

  // Personal
  firstName: '',
  lastName: '',
  phoneNumber: '',

  // Address
  streetNumber: '',
  street: '',
  city: '',
  postalCode: '',
  region: '',
  country: 'France',
  addressLine2: '',

  // Professional
  skills: '',
  yearsOfExperience: 0,
  desiredSalary: 0,
  availability: '',
  linkedInProfile: '',

  // Collections
  trainings: [],
  experiences: [],

  // Preferences
  emailNotificationsEnabled: true,
  smsNotificationsEnabled: false,
  preferredLanguage: 'fr',
  timeZone: 'Europe/Paris',

  // Legal
  acceptTerms: false,
  acceptPrivacyPolicy: false,

  // Not used for candidates
  forceLocalRegistration: true,
});

// Validation rules
const passwordRules = [
  (val: string) => !!val || 'Mot de passe requis',
  (val: string) => val.length >= 6 || 'Au moins 6 caractères',
  (val: string) => /[A-Z]/.test(val) || 'Au moins une majuscule',
  (val: string) => /[a-z]/.test(val) || 'Au moins une minuscule',
  (val: string) => /[0-9]/.test(val) || 'Au moins un chiffre',
];

const validateEmail = (email: string) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email) || 'Email invalide';
};

// Computed
const canAddTraining = computed(() => {
  if (!formData.trainings || formData.trainings.length === 0) return true;
  const lastTraining = formData.trainings[formData.trainings.length - 1];
  return !!(
    lastTraining?.school &&
    lastTraining.level &&
    lastTraining.field &&
    lastTraining.period
  );
});

const canAddExperience = computed(() => {
  if (!formData.experiences || formData.experiences.length === 0) return true;
  const lastExperience = formData.experiences[formData.experiences.length - 1];
  return !!(
    lastExperience?.title &&
    lastExperience.company &&
    lastExperience.date &&
    lastExperience.description
  );
});

const canProceed = computed(() => {
  switch (currentStep.value) {
    case 1:
      return !!cvFile.value; // CV is now required
    case 2:
      return !!(formData.firstName && formData.lastName);
    case 3:
      return true; // Professional info is optional
    case 4:
      return true; // Training is optional
    case 5:
      return true; // Experience is optional
    case 6:
      return !!(
        formData.email &&
        formData.password &&
        formData.confirmPassword &&
        formData.password === formData.confirmPassword &&
        formData.acceptTerms &&
        formData.acceptPrivacyPolicy
      );
    default:
      return false;
  }
});

const canSubmit = computed(() => {
  return canProceed.value && currentStep.value === 6;
});

// Methods
const addTraining = () => {
  if (!formData.trainings) formData.trainings = [];
  formData.trainings.push({
    school: '',
    level: '',
    field: '',
    period: '',
  });
};

const removeTraining = (index: number) => {
  formData.trainings?.splice(index, 1);
};

const addExperience = () => {
  if (!formData.experiences) formData.experiences = [];
  formData.experiences.push({
    title: '',
    company: '',
    location: '',
    date: '',
    description: '',
  });
};

const removeExperience = (index: number) => {
  formData.experiences?.splice(index, 1);
};

const fillFromCVAnalysis = (analysis: ResumeAnalysisResponse) => {
  const data = analysis.extracted_data;

  // Personal info
  if (data.first_name) formData.firstName = data.first_name;
  if (data.last_name) formData.lastName = data.last_name;
  if (data.email) formData.email = data.email;
  if (data.phone_number) formData.phoneNumber = data.phone_number;

  // Address info (if available)
  if (data.address) {
    // Try to parse address if it's a string
    // For now, just put it in the street field
    formData.street = data.address;
  }

  // Skills - join array into text with line breaks for better readability
  if (data.skills && data.skills.length > 0) {
    formData.skills = data.skills.join('\n');
  }

  // Training - directly assign the array
  if (data.trainings && data.trainings.length > 0) {
    formData.trainings = [...data.trainings];
  }

  // Experience - directly assign the array
  if (data.experiences && data.experiences.length > 0) {
    formData.experiences = [...data.experiences];
  }

  // Languages info could be used later for additional fields
  console.log('CV Analysis - Languages detected:', data.languages);
  console.log('CV Analysis - Profession detected:', data.profession);

  cvAnalyzed.value = true;
};

const nextStep = async () => {
  if (currentStep.value === 1 && cvFile.value && shouldAnalyzeCV.value) {
    isAnalyzing.value = true;
    try {
      const result = await authStore.analyzeResume(cvFile.value);
      console.log(result);
      if (result) {
        notification.showSuccessNotification('CV analysé avec succès !');
        fillFromCVAnalysis(result);
      }
    } catch (error) {
      console.error('CV analysis failed:', error);
      notification.showErrorNotification("Erreur lors de l'analyse du CV");
    } finally {
      isAnalyzing.value = false;
    }
  }

  if (canProceed.value) {
    currentStep.value++;
  }
};

const previousStep = () => {
  if (currentStep.value > 1) {
    currentStep.value--;
  }
};

const submitForm = () => {
  if (canSubmit.value) {
    // Set consent timestamp
    formData.consentGivenAt = new Date().toISOString();
    emit('submit', formData, cvFile.value);
  }
};
</script>

<style lang="scss" scoped>
.multi-step-form {
  .step-content {
    min-height: 400px;
    padding: 20px 0;
  }

  .training-card,
  .experience-card {
    .q-card {
      border: 1px solid #e0e0e0;
    }
  }
}

:deep(.q-stepper__step-inner) {
  padding: 0;
}

:deep(.q-stepper__nav) {
  padding-top: 0;
}
</style>
