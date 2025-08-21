<template>
  <q-card class="candidate-filters">
    <q-card-section>
      <div class="row q-gutter-md">
        <div class="col-12 col-md-3">
          <q-input
            v-model="localFilters.search"
            outlined
            dense
            placeholder="Rechercher un candidat..."
            clearable
            aria-label="Rechercher parmi les candidats"
            @keydown.enter="emitSearch"
          >
            <template #prepend>
              <q-icon name="search" />
            </template>
          </q-input>
        </div>

        <div class="col-12 col-md-2">
          <q-input
            v-model="localFilters.position"
            outlined
            dense
            placeholder="Poste recherché"
            clearable
            aria-label="Filtrer par poste recherché"
            @keydown.enter="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.experience"
            outlined
            dense
            placeholder="Expérience"
            clearable
            :options="experienceOptions"
            aria-label="Filtrer par niveau d'expérience"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.location"
            outlined
            dense
            placeholder="Localisation"
            clearable
            :options="locationOptions"
            aria-label="Filtrer par localisation"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-2">
          <q-select
            v-model="localFilters.status"
            outlined
            dense
            placeholder="Statut"
            clearable
            :options="statusOptions"
            aria-label="Filtrer par statut"
            @update:model-value="emitSearch"
          />
        </div>

        <div class="col-12 col-md-1">
          <q-btn
            flat
            dense
            color="grey-7"
            icon="clear"
            label="Effacer"
            aria-label="Effacer tous les filtres"
            @click="clearAllFilters"
          />
        </div>
      </div>

      <div class="row q-gutter-md q-mt-sm">
        <div class="col-12">
          <q-select
            v-model="localFilters.skills"
            outlined
            dense
            multiple
            use-chips
            placeholder="Compétences"
            :options="skillsOptions"
            aria-label="Filtrer par compétences"
            @update:model-value="emitSearch"
          >
            <template #prepend>
              <q-icon name="psychology" />
            </template>
          </q-select>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';

interface CandidateFilters {
  search: string;
  position: string;
  experience: string;
  location: string;
  skills: string[];
  status: string;
}

interface Props {
  filters: CandidateFilters;
}

interface Emits {
  (e: 'update:filters', filters: CandidateFilters): void;
  (e: 'search'): void;
  (e: 'clear'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const localFilters = ref<CandidateFilters>({ ...props.filters });

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
  localFilters,
  (newFilters) => {
    emit('update:filters', { ...newFilters });
  },
  { deep: true },
);

function emitSearch() {
  emit('search');
}

function clearAllFilters() {
  localFilters.value = {
    search: '',
    position: '',
    experience: '',
    location: '',
    skills: [],
    status: '',
  };
  emit('clear');
}
</script>

<style scoped>
.candidate-filters {
  border: 1px solid #e0e0e0;
}

@media (max-width: 599px) {
  .row {
    flex-direction: column;
  }

  .col-12 {
    margin-bottom: 8px;
  }
}
</style>
