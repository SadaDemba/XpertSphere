<template>
  <div class="app-logo" :class="logoClasses">
    <router-link to="/" class="logo-link" :aria-label="ariaLabel">
      <img :src="logoSrc" :alt="altText" :class="imageClasses" loading="lazy" />
    </router-link>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import logoIcon from '../assets/logos/logo-icon.png';
import logoFull from '../assets/logos/logo-full.png';

interface Props {
  variant?: 'full' | 'icon';
  size?: 'small' | 'medium' | 'large';
  clickable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'full',
  size: 'medium',
  clickable: true,
});

const logoSrc = computed(() => {
  return props.variant === 'icon' ? logoIcon : logoFull;
});

const altText = computed(() => {
  return props.variant === 'icon' ? 'XpertSphere ATS' : 'XpertSphere ATS - Sphere of Experts';
});

const ariaLabel = computed(() => {
  return `${altText.value} - Return to homepage`;
});

const logoClasses = computed(() => {
  return {
    'logo-clickable': props.clickable,
    [`logo-${props.size}`]: true,
  };
});

const imageClasses = computed(() => {
  const baseClasses = ['logo-image'];

  if (props.variant === 'icon') {
    baseClasses.push('logo-icon');
  } else {
    baseClasses.push('logo-full');
  }

  return baseClasses.join(' ');
});
</script>

<style scoped>
.app-logo {
  display: flex;
  align-items: center;
}

.logo-link {
  display: flex;
  align-items: center;
  text-decoration: none;
  border-radius: 4px;
  transition: all 0.2s ease;
}

.logo-link:focus {
  outline: 2px solid white;
  outline-offset: 2px;
}

.logo-clickable .logo-link:hover {
  opacity: 0.8;
  transform: scale(1.02);
}

.logo-image {
  display: block;
  max-width: 100%;
  height: auto;
}

/* Size variants */
.logo-small .logo-icon {
  height: 32px;
  width: 32px;
}

.logo-small .logo-full {
  height: 32px;
  width: auto;
}

.logo-medium .logo-icon {
  height: 40px;
  width: 40px;
}

.logo-medium .logo-full {
  height: 120px;
  width: auto;
}

.logo-large .logo-icon {
  height: 48px;
  width: 48px;
}

.logo-large .logo-full {
  height: 120px;
  width: auto;
}

/* Responsive behavior */
@media (max-width: 599px) {
  .logo-full {
    display: none;
  }

  .logo-icon {
    display: block;
  }
}

@media (min-width: 600px) {
  .app-logo[data-mobile-only] .logo-full {
    display: block;
  }
}
</style>
