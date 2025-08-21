<template>
  <q-dialog v-model="isOpen" persistent max-width="600px">
    <q-card class="invite-candidate-dialog">
      <q-card-section class="row items-center">
        <div class="text-h6">Inviter un candidat</div>
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
        <q-form class="q-gutter-md" @submit="sendInvitation">
          <div class="row q-gutter-md">
            <div class="col-12">
              <q-select
                v-model="formData.jobId"
                outlined
                label="Offre d'emploi *"
                :options="jobOptions"
                option-label="title"
                option-value="id"
                emit-value
                map-options
                :rules="[(val) => !!val || 'Veuillez sélectionner une offre']"
                aria-label="Sélectionner l'offre d'emploi"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.candidateEmail"
                outlined
                type="email"
                label="Email du candidat *"
                :rules="[
                  (val) => !!val || 'L\'email est obligatoire',
                  (val) => /.+@.+\..+/.test(val) || 'Email invalide',
                ]"
                aria-label="Adresse email du candidat"
                required
              />
            </div>
            <div class="col-12 col-md-5">
              <q-input
                v-model="formData.candidateName"
                outlined
                label="Nom du candidat"
                aria-label="Nom du candidat (optionnel)"
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.subject"
                outlined
                label="Objet de l'email *"
                :rules="[(val) => !!val || 'L\'objet est obligatoire']"
                aria-label="Objet de l'email d'invitation"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-input
                v-model="formData.message"
                outlined
                type="textarea"
                label="Message personnalisé *"
                rows="8"
                :rules="[(val) => !!val || 'Le message est obligatoire']"
                aria-label="Message personnalisé de l'invitation"
                required
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-checkbox
                v-model="formData.includeJobDescription"
                label="Inclure la description du poste dans l'email"
                aria-label="Inclure automatiquement la description du poste"
              />
            </div>
          </div>

          <div class="row q-gutter-md">
            <div class="col-12">
              <q-checkbox
                v-model="formData.trackOpening"
                label="Suivre l'ouverture de l'email"
                aria-label="Activer le suivi d'ouverture de l'email"
              />
            </div>
          </div>
        </q-form>
      </q-card-section>

      <q-separator />

      <q-card-actions align="right" class="q-pa-md">
        <q-btn flat label="Annuler" aria-label="Annuler l'invitation" @click="closeDialog" />
        <q-btn
          color="primary"
          label="Envoyer l'invitation"
          :loading="sending"
          aria-label="Envoyer l'invitation par email"
          @click="sendInvitation"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

interface InvitationFormData {
  jobId: string;
  candidateEmail: string;
  candidateName: string;
  subject: string;
  message: string;
  includeJobDescription: boolean;
  trackOpening: boolean;
}

interface Props {
  modelValue: boolean;
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void;
  (e: 'sent'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const sending = ref(false);

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value),
});

const formData = ref<InvitationFormData>({
  jobId: '',
  candidateEmail: '',
  candidateName: '',
  subject: "Opportunité d'emploi chez XpertSphere",
  message: `Bonjour,

Nous avons une opportunité qui pourrait vous intéresser chez XpertSphere.

Votre profil correspond parfaitement aux exigences de ce poste et nous serions ravis de discuter avec vous de cette opportunité.

Si vous êtes intéressé(e), n'hésitez pas à postuler directement via le lien ci-dessous ou à me contacter pour plus d'informations.

Cordialement,
L'équipe de recrutement XpertSphere`,
  includeJobDescription: true,
  trackOpening: true,
});

const jobOptions = [
  { id: '1', title: 'Développeur Full Stack Senior' },
  { id: '2', title: 'Designer UX/UI' },
  { id: '3', title: 'Product Manager' },
  { id: '4', title: 'Data Scientist' },
];

function closeDialog() {
  isOpen.value = false;
  resetForm();
}

function resetForm() {
  formData.value = {
    jobId: '',
    candidateEmail: '',
    candidateName: '',
    subject: "Opportunité d'emploi chez XpertSphere",
    message: `Bonjour,

Nous avons une opportunité qui pourrait vous intéresser chez XpertSphere.

Votre profil correspond parfaitement aux exigences de ce poste et nous serions ravis de discuter avec vous de cette opportunité.

Si vous êtes intéressé(e), n'hésitez pas à postuler directement via le lien ci-dessous ou à me contacter pour plus d'informations.

Cordialement,
L'équipe de recrutement XpertSphere`,
    includeJobDescription: true,
    trackOpening: true,
  };
}

async function sendInvitation() {
  sending.value = true;
  try {
    await new Promise((resolve) => setTimeout(resolve, 2000));

    console.log('Sending invitation:', formData.value);

    emit('sent');
    closeDialog();
  } finally {
    sending.value = false;
  }
}
</script>

<style scoped>
.invite-candidate-dialog {
  width: 100%;
  max-width: 600px;
}

@media (max-width: 768px) {
  .invite-candidate-dialog {
    margin: 16px;
    max-width: calc(100vw - 32px);
  }

  .row .col-12 {
    padding: 0;
  }
}
</style>
