<template>
  <q-card class="job-card cursor-pointer transition-all" @click="$emit('click')">
    <q-card-section>
      <div class="row items-start justify-between">
        <div class="col">
          <h6 class="job-title q-mt-none q-mb-sm text-weight-medium">
            {{ job.title }}
          </h6>
          <p class="company-name text-primary q-mb-sm">
            {{ job.organizationName }}
          </p>
        </div>
        <q-chip
          :color="statusConfig.color"
          :text-color="statusConfig.textColor"
          size="sm"
          :icon="statusConfig.icon"
        >
          {{ statusConfig.label }}
        </q-chip>
      </div>

      <div class="job-meta q-mb-md">
        <div class="row q-gutter-sm">
          <q-chip v-if="job.location" outline size="sm" icon="place" color="grey-7">
            {{ job.location }}
          </q-chip>

          <q-chip outline size="sm" icon="work" color="grey-7">
            {{ workModeLabels[job.workMode] }}
          </q-chip>

          <q-chip outline size="sm" icon="schedule" color="grey-7">
            {{ contractTypeLabels[job.contractType] }}
          </q-chip>
        </div>
      </div>

      <div v-if="job.salaryMin || job.salaryMax" class="job-salary q-mb-md">
        <q-icon name="euro" class="q-mr-xs" color="positive" />
        <span class="text-weight-medium text-positive">
          <template v-if="job.salaryMin && job.salaryMax">
            {{ formatSalary(job.salaryMin) }} - {{ formatSalary(job.salaryMax) }}
          </template>
          <template v-else-if="job.salaryMin">
            À partir de {{ formatSalary(job.salaryMin) }}
          </template>
          <template v-else-if="job.salaryMax"> Jusqu'à {{ formatSalary(job.salaryMax) }} </template>
          <span v-if="job.salaryCurrency" class="text-grey-7 q-ml-xs">
            {{ job.salaryCurrency }}
          </span>
        </span>
      </div>

      <p class="job-description text-grey-8 q-mb-md">
        {{ truncatedDescription }}
      </p>

      <div class="job-footer">
        <div class="row justify-between items-center">
          <small class="text-grey-6">
            <q-icon name="access_time" size="14px" class="q-mr-xs" />
            Publié {{ formatDate(job.publishedAt || job.createdAt) }}
          </small>

          <div class="row q-gutter-xs">
            <q-btn
              flat
              round
              size="sm"
              icon="visibility"
              color="primary"
              @click.stop="$emit('view', job.id)"
            >
              <q-tooltip>Voir les détails</q-tooltip>
            </q-btn>

            <q-btn
              v-if="!hasApplied"
              flat
              round
              size="sm"
              icon="send"
              color="positive"
              @click.stop="$emit('apply', job.id)"
            >
              <q-tooltip>Candidater</q-tooltip>
            </q-btn>

            <q-chip v-else size="sm" color="positive" text-color="white" icon="check">
              Candidaté
            </q-chip>
          </div>
        </div>
      </div>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { date } from 'quasar';
import type { JobOfferDto } from '../models/job';
import { jobOfferStatusConfig, workModeLabels, contractTypeLabels } from '../models/job';

interface Props {
  job: JobOfferDto;
  hasApplied?: boolean;
}

interface Emits {
  (e: 'click'): void;
  (e: 'view', jobId: string): void;
  (e: 'apply', jobId: string): void;
}

const props = withDefaults(defineProps<Props>(), {
  hasApplied: false,
});

defineEmits<Emits>();

// Computed
const statusConfig = computed(() => jobOfferStatusConfig[props.job.status]);

const truncatedDescription = computed(() => {
  if (props.job.description.length <= 150) {
    return props.job.description;
  }
  return props.job.description.substring(0, 150) + '...';
});

// Methods
const formatSalary = (amount: number): string => {
  return new Intl.NumberFormat('fr-FR').format(amount);
};

const formatDate = (dateString: string): string => {
  const jobDate = new Date(dateString);
  const now = new Date();
  const diffInDays = Math.floor((now.getTime() - jobDate.getTime()) / (1000 * 3600 * 24));

  if (diffInDays === 0) {
    return "aujourd'hui";
  } else if (diffInDays === 1) {
    return 'hier';
  } else if (diffInDays < 7) {
    return `il y a ${diffInDays} jours`;
  } else if (diffInDays < 30) {
    const weeks = Math.floor(diffInDays / 7);
    return `il y a ${weeks} semaine${weeks > 1 ? 's' : ''}`;
  } else {
    return date.formatDate(jobDate, 'DD/MM/YYYY');
  }
};
</script>

<style lang="scss" scoped>
.job-card {
  border: 1px solid #e0e0e0;
  border-radius: 12px;
  transition: all 0.2s ease;
  height: 100%;

  &:hover {
    border-color: var(--q-primary);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
  }
}

.job-title {
  font-size: 1.1rem;
  line-height: 1.3;
  color: #2c3e50;
}

.company-name {
  font-weight: 500;
}

.job-description {
  line-height: 1.4;
  font-size: 0.9rem;
}

.job-meta .q-chip {
  font-size: 0.75rem;
}

.job-footer {
  margin-top: auto;
  padding-top: 8px;
  border-top: 1px solid #f0f0f0;
}
</style>
