<template>
  <footer class="compact-footer bg-grey-8 text-white">
    <div class="footer-content q-pa-md">
      <!-- Main footer links -->
      <div class="row items-center justify-center q-gutter-lg q-mb-sm">
        <router-link to="/" class="footer-link text-white"> Offres d'emploi </router-link>
        <router-link to="/applications" class="footer-link text-white">
          Mes candidatures
        </router-link>
        <router-link to="/help" class="footer-link text-white"> Aide </router-link>
        <router-link to="/privacy" class="footer-link text-white"> Confidentialité </router-link>
        <router-link to="/terms" class="footer-link text-white"> CGU </router-link>
      </div>

      <!-- Copyright -->
      <div class="text-center">
        <div class="text-body2 text-grey-4">
          © {{ currentYear }} XpertSphere - Plateforme candidat
        </div>
      </div>
    </div>
  </footer>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const currentYear = computed(() => new Date().getFullYear());
</script>

<style scoped>
.compact-footer {
  margin-top: auto;
}

.footer-content {
  padding: 16px;
}

.footer-link {
  text-decoration: none;
  font-size: 14px;
  transition: opacity 0.2s ease;
}

.footer-link:hover {
  opacity: 0.8;
  text-decoration: underline;
}

.footer-link:focus {
  outline: 2px solid white;
  outline-offset: 2px;
  border-radius: 2px;
}

/* Mobile responsive */
@media (max-width: 599px) {
  .footer-content {
    padding: 12px;
  }

  .row {
    flex-direction: column;
    gap: 8px;
  }

  .footer-link {
    font-size: 13px;
  }
}
</style>
