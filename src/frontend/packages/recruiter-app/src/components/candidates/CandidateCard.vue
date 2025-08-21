<template>
  <q-card class="candidate-card" tabindex="0" @keydown.enter="$emit('viewProfile', candidate)">
    <q-card-section>
      <div class="row items-start justify-between">
        <div class="candidate-info flex-1">
          <div class="candidate-header row items-center q-mb-sm">
            <q-avatar size="40px" color="primary" text-color="white" class="q-mr-sm">
              {{ candidate.firstName[0] }}{{ candidate.lastName[0] }}
            </q-avatar>
            <div>
              <h3 class="candidate-name text-h6 q-mb-none">
                <button
                  class="name-link"
                  :aria-label="`Voir le profil de ${candidate.firstName} ${candidate.lastName}`"
                  @click="$emit('viewProfile', candidate)"
                >
                  {{ candidate.firstName }} {{ candidate.lastName }}
                </button>
              </h3>
              <div class="candidate-position text-body2 text-grey-7">
                {{ candidate.position }}
              </div>
            </div>
          </div>

          <div class="candidate-meta text-body2 text-grey-7 q-mb-sm">
            <div class="meta-item">
              <q-icon name="email" size="16px" />
              {{ candidate.email }}
            </div>
            <div class="meta-item">
              <q-icon name="phone" size="16px" />
              {{ candidate.phone }}
            </div>
            <div class="meta-item">
              <q-icon name="location_on" size="16px" />
              {{ candidate.location }}
            </div>
            <div class="meta-item">
              <q-icon name="work_history" size="16px" />
              {{ candidate.experience }}
            </div>
          </div>

          <div class="candidate-skills q-mb-sm">
            <q-chip
              v-for="skill in candidate.skills.slice(0, 3)"
              :key="skill"
              dense
              outline
              color="primary"
              size="sm"
              class="q-mr-xs q-mb-xs"
            >
              {{ skill }}
            </q-chip>
            <q-chip
              v-if="candidate.skills.length > 3"
              dense
              outline
              color="grey-6"
              size="sm"
              :label="`+${candidate.skills.length - 3}`"
              class="q-mr-xs q-mb-xs"
            />
          </div>
        </div>

        <q-btn
          flat
          dense
          round
          icon="more_vert"
          :aria-label="`Actions pour ${candidate.firstName} ${candidate.lastName}`"
        >
          <q-menu>
            <q-list style="min-width: 120px">
              <q-item clickable @click="$emit('viewProfile', candidate)">
                <q-item-section avatar>
                  <q-icon name="person" />
                </q-item-section>
                <q-item-section>Voir profil</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('edit', candidate)">
                <q-item-section avatar>
                  <q-icon name="edit" />
                </q-item-section>
                <q-item-section>Modifier</q-item-section>
              </q-item>
              <q-item clickable @click="$emit('viewApplications', candidate)">
                <q-item-section avatar>
                  <q-icon name="assignment" />
                </q-item-section>
                <q-item-section>Candidatures</q-item-section>
              </q-item>
              <q-separator />
              <q-item clickable class="text-negative" @click="$emit('delete', candidate)">
                <q-item-section avatar>
                  <q-icon name="delete" />
                </q-item-section>
                <q-item-section>Supprimer</q-item-section>
              </q-item>
            </q-list>
          </q-menu>
        </q-btn>
      </div>
    </q-card-section>

    <q-card-section class="card-footer">
      <div class="row items-center justify-between">
        <div class="status-info">
          <q-chip
            :color="statusConfig[candidate.status].color"
            :text-color="statusConfig[candidate.status].textColor"
            dense
            :aria-label="`Statut: ${statusConfig[candidate.status].label}`"
          >
            <q-icon :name="statusConfig[candidate.status].icon" size="14px" class="q-mr-xs" />
            {{ statusConfig[candidate.status].label }}
          </q-chip>
        </div>

        <div class="activity-info text-caption text-grey-6">
          Dernière activité: {{ formatDate(candidate.lastActivity) }}
        </div>
      </div>

      <div class="dates text-caption text-grey-6 q-mt-sm">
        Ajouté le {{ formatDate(candidate.createdAt) }} • Modifié le
        {{ formatDate(candidate.updatedAt) }}
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
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
}

interface Props {
  candidate: Candidate;
}

interface Emits {
  (e: 'edit', candidate: Candidate): void;
  (e: 'delete', candidate: Candidate): void;
  (e: 'viewProfile', candidate: Candidate): void;
  (e: 'viewApplications', candidate: Candidate): void;
}

type StatusConfig = {
  label: string;
  color: string;
  textColor: string;
  icon: string;
};

defineProps<Props>();
defineEmits<Emits>();

const statusConfig: Record<Candidate['status'], StatusConfig> = {
  active: {
    label: 'Actif',
    color: 'positive',
    textColor: 'white',
    icon: 'check_circle',
  },
  inactive: {
    label: 'Inactif',
    color: 'grey-4',
    textColor: 'grey-8',
    icon: 'radio_button_unchecked',
  },
  blacklisted: {
    label: 'Liste noire',
    color: 'negative',
    textColor: 'white',
    icon: 'block',
  },
};

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}
</script>

<style scoped>
.candidate-card {
  border: 1px solid #e0e0e0;
  transition: all 0.2s ease;
  cursor: pointer;
}

.candidate-card:hover {
  border-color: var(--q-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.candidate-card:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
}

.name-link {
  background: none;
  border: none;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  padding: 0;
  margin: 0;
  text-decoration: none;
}

.name-link:hover {
  color: var(--q-primary);
}

.name-link:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
  border-radius: 4px;
}

.candidate-meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.candidate-skills {
  max-height: 60px;
  overflow: hidden;
}

.card-footer {
  background-color: #f8f9fa;
  border-top: 1px solid #e0e0e0;
}

@media (max-width: 599px) {
  .candidate-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .candidate-meta {
    font-size: 12px;
  }

  .card-footer .row {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
}
</style>
