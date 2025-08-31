<template>
  <q-dialog v-model="isOpen" persistent max-width="900px">
    <q-card class="candidate-dialog">
      <q-card-section class="row items-center">
        <div class="text-h6">
          {{ isEditing ? 'Modifier le candidat' : 'Nouveau candidat' }}
        </div>
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

      <q-card-section class="q-pa-md">
        <q-form class="q-gutter-md" @submit="saveCandidate">
          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.firstName"
                outlined
                label="Prénom *"
                :rules="[(val) => !!val || 'Le prénom est obligatoire']"
                aria-label="Prénom du candidat"
                required
              />
            </div>
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.lastName"
                outlined
                label="Nom *"
                :rules="[(val) => !!val || 'Le nom est obligatoire']"
                aria-label="Nom du candidat"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.email"
                outlined
                type="email"
                label="Email *"
                :rules="[
                  (val) => !!val || 'L\'email est obligatoire',
                  (val) => /.+@.+\..+/.test(val) || 'Email invalide',
                ]"
                aria-label="Adresse email"
                required
              />
            </div>
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.phone"
                outlined
                label="Téléphone *"
                :rules="[(val) => !!val || 'Le téléphone est obligatoire']"
                aria-label="Numéro de téléphone"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.position"
                outlined
                label="Poste recherché *"
                :rules="[(val) => !!val || 'Le poste recherché est obligatoire']"
                aria-label="Poste recherché"
                required
              />
            </div>
            <div class="col-12 col-md-5">
              <q-select
                v-model="formData.experience"
                outlined
                label="Niveau d'expérience *"
                :options="experienceOptions"
                :rules="[(val) => !!val || 'Le niveau d\'expérience est obligatoire']"
                aria-label="Niveau d'expérience"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-select
                v-model="formData.location"
                outlined
                label="Localisation *"
                :options="locationOptions"
                :rules="[(val) => !!val || 'La localisation est obligatoire']"
                aria-label="Localisation"
                required
              />
            </div>
            <div class="col-12 col-md-5">
              <q-select
                v-model="formData.status"
                outlined
                label="Statut *"
                :options="statusOptions"
                :rules="[(val) => !!val || 'Le statut est obligatoire']"
                aria-label="Statut du candidat"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-select
                v-model="formData.skills"
                outlined
                multiple
                use-chips
                label="Compétences"
                :options="skillsOptions"
                aria-label="Compétences du candidat"
              >
                <template #prepend>
                  <q-icon name="psychology" />
                </template>
              </q-select>
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.summary"
                outlined
                type="textarea"
                label="Résumé professionnel"
                rows="4"
                aria-label="Résumé professionnel du candidat"
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.linkedinUrl"
                outlined
                label="LinkedIn"
                placeholder="https://linkedin.com/in/..."
                aria-label="Profil LinkedIn"
              />
            </div>
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.portfolioUrl"
                outlined
                label="Portfolio/Site web"
                placeholder="https://..."
                aria-label="Portfolio ou site web"
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.notes"
                outlined
                type="textarea"
                label="Notes internes"
                rows="3"
                aria-label="Notes internes sur le candidat"
              />
            </div>
          </div>
        </q-form>
      </q-card-section>

      <q-separator />

      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="Annuler" aria-label="Annuler et fermer" @click="closeDialog" />
        <q-btn
          color="primary"
          label="Enregistrer"
          :loading="saving"
          :aria-label="isEditing ? 'Enregistrer les modifications' : 'Créer le candidat'"
          @click="saveCandidate"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';

interface Candidate {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  position: string;
  experience: string;
  location: string;
  skills: string[];
  status: 'active' | 'inactive' | 'blacklisted';
  createdAt: string;
  updatedAt: string;
  lastActivity: string;
  summary?: string;
  linkedinUrl?: string;
  portfolioUrl?: string;
  notes?: string;
}

interface CandidateFormData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  position: string;
  experience: string;
  location: string;
  skills: string[];
  status: string;
  summary: string;
  linkedinUrl: string;
  portfolioUrl: string;
  notes: string;
}

interface Props {
  modelValue: boolean;
  candidate?: Candidate | null;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'saved'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const saving = ref(false);

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

const isEditing = computed(() => !!props.candidate);

const formData = ref<CandidateFormData>({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  position: '',
  experience: '',
  location: '',
  skills: [],
  status: 'active',
  summary: '',
  linkedinUrl: '',
  portfolioUrl: '',
  notes: '',
});

const experienceOptions = [
  'Débutant (0-1 an)',
  'Junior (1-3 ans)',
  'Confirmé (3-5 ans)',
  'Senior (5-10 ans)',
  'Expert (10+ ans)',
];

const locationOptions = [
  'Paris',
  'Lyon',
  'Marseille',
  'Toulouse',
  'Nantes',
  'Bordeaux',
  'Télétravail',
];

const statusOptions = [
  { label: 'Actif', value: 'active' },
  { label: 'Inactif', value: 'inactive' },
  { label: 'Liste noire', value: 'blacklisted' },
];

const skillsOptions = [
  'JavaScript',
  'TypeScript',
  'Vue.js',
  'React',
  'Angular',
  'Node.js',
  'Python',
  'Java',
  'C#',
  'PHP',
  'Ruby',
  'Go',
  'PostgreSQL',
  'MySQL',
  'MongoDB',
  'Redis',
  'AWS',
  'Azure',
  'Docker',
  'Kubernetes',
  'Git',
  'Figma',
  'Adobe Creative Suite',
  'Sketch',
  'Prototyping',
  'User Research',
  'UI/UX Design',
  'Marketing Digital',
  'SEO/SEM',
  'Analytics',
  'Project Management',
  'Agile/Scrum',
];

watch(
  () => props.candidate,
  (newCandidate) => {
    if (newCandidate) {
      formData.value = {
        firstName: newCandidate.firstName || '',
        lastName: newCandidate.lastName || '',
        email: newCandidate.email || '',
        phone: newCandidate.phone || '',
        position: newCandidate.position || '',
        experience: newCandidate.experience || '',
        location: newCandidate.location || '',
        skills: newCandidate.skills || [],
        status: newCandidate.status || 'active',
        summary: newCandidate.summary || '',
        linkedinUrl: newCandidate.linkedinUrl || '',
        portfolioUrl: newCandidate.portfolioUrl || '',
        notes: newCandidate.notes || '',
      };
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
  formData.value = {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    position: '',
    experience: '',
    location: '',
    skills: [],
    status: 'active',
    summary: '',
    linkedinUrl: '',
    portfolioUrl: '',
    notes: '',
  };
}

function closeDialog() {
  isOpen.value = false;
  resetForm();
}

async function saveCandidate() {
  saving.value = true;
  try {
    await new Promise((resolve) => setTimeout(resolve, 1000));

    console.log('Saving candidate:', formData.value);

    emit('saved');
    closeDialog();
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.candidate-dialog {
  width: 100%;
  max-width: 900px;
}

@media (max-width: 768px) {
  .candidate-dialog {
    margin: 16px;
    max-width: calc(100vw - 32px);
  }

  .row .col-12 {
    padding: 0;
  }
}
</style>
