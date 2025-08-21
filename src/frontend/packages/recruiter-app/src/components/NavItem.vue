<template>
  <q-item
    clickable
    :active="isActive"
    active-class="bg-primary text-white"
    role="menuitem"
    :aria-current="isActive ? 'page' : undefined"
    :aria-label="description"
    tabindex="0"
    @click="handleClick"
    @keydown.enter="handleClick"
    @keydown.space.prevent="handleClick"
  >
    <q-item-section avatar>
      <q-icon :name="icon" :aria-hidden="true" />
    </q-item-section>

    <q-item-section>
      <q-item-label>{{ title }}</q-item-label>
      <q-item-label caption class="text-grey-7" :aria-hidden="true">
        {{ description }}
      </q-item-label>
    </q-item-section>

    <q-item-section v-if="hasNotification" side>
      <q-badge color="red" rounded :aria-label="`${notificationCount} new items`">
        {{ notificationCount }}
      </q-badge>
    </q-item-section>
  </q-item>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

interface Props {
  name: string;
  title: string;
  icon: string;
  route?: string;
  description: string;
  notificationCount?: number;
}

interface Emits {
  (e: 'navigate', route?: string): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();
const currentRoute = useRoute();

const isActive = computed(() => {
  if (!props.route) return false;
  if (props.route === '/') {
    return currentRoute.path === '/';
  }
  return currentRoute.path.startsWith(props.route);
});

const hasNotification = computed(
  () => props.notificationCount !== undefined && props.notificationCount > 0,
);

function handleClick() {
  if (props.route) {
    emit('navigate', props.route);
  } else {
    emit('navigate');
  }
}
</script>

<style scoped>
.q-item {
  border-radius: 8px;
  margin: 4px 8px;
}

.q-item:focus {
  outline: 2px solid var(--q-primary);
  outline-offset: 2px;
}

.q-item:hover:not(.q-item--active) {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
