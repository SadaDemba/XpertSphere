<template>
  <q-page class="cv-view-page">
    <!-- Breadcrumbs -->
    <div class="breadcrumb-container q-pa-md q-mb-lg">
      <div class="breadcrumb-content">
        <q-breadcrumbs class="text-grey-7" active-color="primary">
          <q-breadcrumbs-el label="Accueil" icon="home" to="/" />
          <q-breadcrumbs-el label="Candidats" icon="group" to="/candidates" />
          <q-breadcrumbs-el
            v-if="candidate"
            :label="candidate.fullName"
            icon="person"
            :to="`/candidates/${candidate.id}`"
          />
          <q-breadcrumbs-el label="CV" icon="description" />
        </q-breadcrumbs>
      </div>
    </div>

    <div v-if="userStore.isLoading" class="row justify-center q-mt-lg">
      <q-spinner-dots size="50px" color="primary" />
    </div>

    <div v-else-if="userStore.hasError" class="row justify-center q-mt-lg">
      <q-card class="q-pa-md">
        <q-card-section>
          <div class="text-h6 text-negative">Erreur</div>
          <p>{{ userStore.errorMessage }}</p>
        </q-card-section>
      </q-card>
    </div>

    <div v-else-if="candidate" class="cv-content q-pa-lg">
      <!-- Header -->
      <q-card class="cv-header-card q-mb-lg">
        <q-card-section>
          <div class="row items-center justify-between">
            <div class="row items-center q-gutter-md">
              <q-avatar size="60px" color="primary" text-color="white">
                <span class="text-h5">{{
                  candidate.fullName ? candidate.fullName.charAt(0).toUpperCase() : '?'
                }}</span>
              </q-avatar>
              <div>
                <h5 class="q-my-none">CV - {{ candidate.fullName }}</h5>
                <p class="text-grey-6 q-my-none">{{ candidate.email }}</p>
              </div>
            </div>

            <div class="row q-gutter-sm">
              <q-btn outline color="primary" icon="arrow_back" label="Retour" @click="goBack" />
              <q-btn
                unelevated
                color="primary"
                icon="download"
                label="Télécharger"
                @click="downloadCV"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>

      <!-- CV Viewer -->
      <q-card class="cv-viewer-card">
        <q-card-section class="q-pa-none">
          <div v-if="cvObjectUrl" class="cv-viewer">
            <iframe
              ref="cvIframe"
              :src="cvObjectUrl"
              class="cv-iframe"
              title="CV Viewer"
              @load="adjustIframeHeight"
            />
          </div>
          <div v-else class="cv-not-available full-height flex flex-center q-pa-xl">
            <div class="text-center">
              <q-icon name="description" size="120px" color="grey-4" />
              <div class="text-h5 text-grey-6 q-mt-lg q-mb-md">CV non disponible</div>
              <p class="text-grey-6">
                Aucun CV n'a été téléchargé pour ce candidat ou le fichier n'est plus accessible.
              </p>
              <q-btn
                outline
                color="primary"
                icon="arrow_back"
                label="Retour au profil"
                class="q-mt-md"
                @click="goBack"
              />
            </div>
          </div>
        </q-card-section>
      </q-card>
    </div>

    <div v-else class="text-center q-pa-lg">
      <q-icon name="search_off" size="xl" color="grey-5" />
      <div class="text-h6 text-grey-6 q-mt-md">Candidat non trouvé</div>
      <p class="text-grey-6">Le candidat demandé n'existe pas ou n'est plus disponible.</p>
      <q-btn
        outline
        color="primary"
        icon="arrow_back"
        label="Retour aux candidats"
        to="/candidates"
        class="q-mt-md"
      />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useUserStore } from '../../stores/userStore';
import userService from '../../services/userService';
import type { UserDto } from '../../models/user';

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();

const candidate = ref<UserDto | null>(null);
const cvIframe = ref<HTMLIFrameElement | null>(null);
const cvObjectUrl = ref<string | null>(null);

const goBack = () => {
  router.push(`/candidates/${candidate.value?.id}`);
};

const adjustIframeHeight = () => {
  setTimeout(() => {
    if (cvIframe.value) {
      try {
        // Set initial height to allow content to load
        cvIframe.value.style.height = 'auto';

        // Calculate optimal height based on content
        const iframeDoc = cvIframe.value.contentDocument || cvIframe.value.contentWindow?.document;
        if (iframeDoc) {
          const contentHeight = Math.max(
            iframeDoc.body?.scrollHeight || 0,
            iframeDoc.documentElement?.scrollHeight || 0,
          );

          // Set minimum height and adjust based on content
          const minHeight = 1000;
          const optimalHeight = Math.max(minHeight, contentHeight + 50);

          cvIframe.value.style.height = `${optimalHeight}px`;
        } else {
          // Fallback for cross-origin or PDF content
          cvIframe.value.style.height = '1400px';
        }
      } catch (err) {
        // Fallback for cross-origin restrictions
        console.error(err);
        cvIframe.value.style.height = '1400px';
      }
    }
  }, 500);
};

const downloadCV = () => {
  if (cvObjectUrl.value && candidate.value?.cvPath) {
    // Derive the real file extension from cvPath (never hardcode it: a CV can
    // be .pdf, .doc or .docx).
    const extension = candidate.value.cvPath.split('.').pop() || 'pdf';

    const link = document.createElement('a');
    link.href = cvObjectUrl.value;
    link.download = `CV_${candidate.value.fullName}.${extension}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
};

const loadCvObjectUrl = async (candidateId: string) => {
  try {
    const blob = await userService.downloadCv(candidateId);
    cvObjectUrl.value = URL.createObjectURL(blob);
  } catch (error) {
    // No CV uploaded, or the file is no longer available in storage (404):
    // fall back to the "CV non disponible" state below.
    console.error('Error downloading CV:', error);
    cvObjectUrl.value = null;
  }
};

onMounted(async () => {
  const candidateId = route.params.id as string;
  if (candidateId) {
    try {
      candidate.value = (await userStore.fetchUserById(candidateId)) || null;
    } catch (error) {
      console.error('Error fetching candidate:', error);
    }

    if (candidate.value?.cvPath) {
      await loadCvObjectUrl(candidateId);
    }
  }
});

onUnmounted(() => {
  if (cvObjectUrl.value) {
    URL.revokeObjectURL(cvObjectUrl.value);
  }
});
</script>

<style scoped>
.cv-view-page {
  background-color: #f5f5f5;
}

.breadcrumb-container {
  background: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.breadcrumb-content {
  max-width: 1200px;
  margin: 0 auto;
}

.cv-content {
  max-width: 1200px;
  margin: 0 auto;
}

.cv-header-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.cv-viewer-card {
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.cv-viewer {
  border-radius: 12px;
  overflow: hidden;
  width: 100%;
}

.cv-iframe {
  width: 100%;
  border: none;
  border-radius: 12px;
  min-height: 1000px;
  display: block;
}

.cv-not-available {
  min-height: 400px;
}
</style>
