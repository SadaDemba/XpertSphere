<template>
  <q-footer class="bg-grey-8 text-white" role="contentinfo">
    <div class="q-pa-md">
      <div class="row q-gutter-md">
        <div class="col-12 col-md-4">
          <div class="footer-brand q-mb-md">
            <app-logo variant="full" size="large" :clickable="true" class="footer-logo" />
          </div>
          <p class="text-body2 q-mb-none">
            Plateforme professionnelle de gestion du recrutement pour les équipes RH modernes.
          </p>
        </div>

        <div class="col-12 col-md-2">
          <nav role="navigation" aria-label="Footer navigation">
            <div class="text-h6 q-mb-sm">Plateforme</div>
            <ul class="list-none q-pa-none">
              <li class="q-mb-xs">
                <a
                  href="/jobs"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/jobs')"
                >
                  Gestion des emplois
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/candidates"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/candidates')"
                >
                  Viviers de candidats
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/reports"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/reports')"
                >
                  Analyses
                </a>
              </li>
            </ul>
          </nav>
        </div>

        <div class="col-12 col-md-2">
          <nav role="navigation" aria-label="Support navigation">
            <div class="text-h6 q-mb-sm">Support</div>
            <ul class="list-none q-pa-none">
              <li class="q-mb-xs">
                <a
                  href="/help"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/help')"
                >
                  Centre d'aide
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/contact"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/contact')"
                >
                  Nous contacter
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/status"
                  class="text-white text-decoration-none"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="System status (opens in new window)"
                >
                  État du système
                </a>
              </li>
            </ul>
          </nav>
        </div>

        <div class="col-12 col-md-3">
          <div class="text-h6 q-mb-sm">Légal</div>
          <nav role="navigation" aria-label="Legal navigation">
            <ul class="list-none q-pa-none q-mb-md">
              <li class="q-mb-xs">
                <a
                  href="/privacy"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/privacy')"
                >
                  Politique de confidentialité
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/terms"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/terms')"
                >
                  Conditions d'utilisation
                </a>
              </li>
              <li class="q-mb-xs">
                <a
                  href="/accessibility"
                  class="text-white text-decoration-none"
                  @click.prevent="navigate('/accessibility')"
                >
                  Accessibilité
                </a>
              </li>
            </ul>
          </nav>
        </div>
      </div>

      <q-separator class="q-my-md" />

      <div class="row items-center justify-between">
        <div class="text-body2">© {{ currentYear }} XpertSphere. Tous droits réservés.</div>
        <div class="q-gutter-sm">
          <span class="text-body2">Version {{ appVersion }}</span>
        </div>
      </div>
    </div>
  </q-footer>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useQuasar } from 'quasar';
import AppLogo from './AppLogo.vue';

const router = useRouter();
const $q = useQuasar();

const currentYear = computed(() => new Date().getFullYear());
const appVersion = '1.0.0';

function navigate(path: string) {
  // Check if it's a functional page or coming soon
  if (path === '/jobs' || path === '/candidates' || path === '/reports') {
    router.push(path);
  } else {
    showComingSoon();
  }
}

function showComingSoon() {
  $q.notify({
    type: 'info',
    message: 'Cette fonctionnalité sera bientôt disponible',
    position: 'top',
    timeout: 3000,
    actions: [{ label: 'OK', color: 'white', handler: () => {} }],
  });
}
</script>

<style scoped>
.list-none {
  list-style: none;
}

.text-decoration-none {
  text-decoration: none;
}

.text-decoration-none:hover {
  text-decoration: underline;
}

a:focus {
  outline: 2px solid white;
  outline-offset: 2px;
  border-radius: 2px;
}

.footer-brand {
  display: flex;
  align-items: center;
}

.footer-logo {
  max-width: 250px;
  filter: brightness(0.9);
}

.footer-logo:hover {
  filter: brightness(1);
  transition: filter 0.2s ease;
}

@media (max-width: 599px) {
  .footer-logo {
    max-width: 200px;
  }
}
</style>
