<template>
  <q-drawer
    id="navigation-drawer"
    v-model="isOpen"
    show-if-above
    bordered
    class="bg-grey-1"
    :width="280"
    role="navigation"
    aria-label="Main navigation"
  >
    <q-scroll-area class="fit">
      <nav role="navigation" aria-label="Main navigation">
        <q-list>
          <q-item-label
            header
            class="text-grey-8 text-weight-bold q-pa-md"
            role="heading"
            aria-level="2"
          >
            Recherche d'emploi
          </q-item-label>

          <nav-item
            v-for="item in jobSearchItems"
            :key="item.name"
            v-bind="item"
            @navigate="handleNavigation"
          />
        </q-list>

        <q-separator class="q-my-md" />

        <q-list>
          <q-item-label
            header
            class="text-grey-8 text-weight-bold q-pa-md"
            role="heading"
            aria-level="2"
          >
            Mon profil
          </q-item-label>

          <nav-item
            v-for="item in profileItems"
            :key="item.name"
            v-bind="item"
            @navigate="handleNavigation"
          />
        </q-list>
      </nav>
    </q-scroll-area>
  </q-drawer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import NavItem from './NavItem.vue';

interface Props {
  modelValue: boolean;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();
const router = useRouter();

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

const jobSearchItems = [
  {
    name: 'browse-jobs',
    title: "Offres d'emploi",
    icon: 'work',
    route: '/',
    description: "Rechercher et parcourir les opportunités d'emploi",
  },
  {
    name: 'applications',
    title: 'Mes candidatures',
    icon: 'assignment',
    route: '/applications',
    description: 'Suivre le statut de vos candidatures',
    notificationCount: 3,
  },
];

const profileItems = [
  {
    name: 'profile',
    title: 'Mon profil',
    icon: 'person',
    route: '/profile',
    description: 'Modifier votre profil et CV',
  },
  {
    name: 'help',
    title: 'Aide & Support',
    icon: 'help',
    route: '/help',
    description: "Obtenir de l'aide et du support",
  },
];

function handleNavigation(routePath: string) {
  router.push(routePath);
}
</script>
