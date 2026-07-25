<template>
  <q-page padding>
    <div class="row items-center justify-between q-mb-lg">
      <div>
        <h4 class="q-my-none">Paramètres de l'organisation</h4>
        <p class="text-grey-6 q-mb-none">
          Devise appliquée aux offres d'emploi de votre organisation
        </p>
      </div>
    </div>

    <q-card style="max-width: 500px">
      <q-card-section>
        <q-banner
          v-if="!isLoading && currency === null"
          class="bg-warning text-white q-mb-md"
          rounded
        >
          Votre organisation n'a pas encore configuré de devise. Tant qu'aucune devise n'est
          configurée, toute nouvelle offre d'emploi créée est stampée en Franc CFA (XOF) par défaut.
          Configurez la devise ci-dessous pour éviter ce comportement.
        </q-banner>

        <q-form @submit.prevent="onSave">
          <q-select
            v-model="selectedCurrency"
            :options="currencyOptions"
            option-label="label"
            option-value="value"
            emit-value
            map-options
            label="Devise de l'organisation"
            filled
            :loading="isLoading"
            :disable="isLoading"
            :rules="[(val) => !!val || 'La devise est requise']"
          />

          <div class="row justify-end q-mt-md">
            <q-btn
              type="submit"
              color="primary"
              label="Enregistrer"
              :loading="isSaving"
              :disable="isLoading"
            />
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Currency, currencyOptions } from '../../enums';
import { organizationService } from '../../services/organizationService';
import { useNotification } from '../../composables/notification';

const notification = useNotification();

const currency = ref<Currency | null>(null);
const selectedCurrency = ref<Currency | null>(null);
const isLoading = ref(false);
const isSaving = ref(false);

const fetchCurrency = async () => {
  isLoading.value = true;
  try {
    const result = await organizationService.getMyOrganizationCurrency();
    if (result?.isSuccess) {
      currency.value = result.data?.currency ?? null;
      selectedCurrency.value = currency.value;
    } else {
      notification.showErrorNotification(
        result?.message || 'Erreur lors de la récupération de la devise',
      );
    }
  } catch {
    notification.showErrorNotification('Erreur lors de la récupération de la devise');
  } finally {
    isLoading.value = false;
  }
};

const onSave = async () => {
  if (!selectedCurrency.value) {
    return;
  }

  isSaving.value = true;
  try {
    const result = await organizationService.updateMyOrganizationCurrency({
      currency: selectedCurrency.value,
    });

    if (result?.isSuccess) {
      currency.value = result.data?.currency ?? selectedCurrency.value;
      notification.showSuccessNotification("Devise de l'organisation mise à jour avec succès");
    } else {
      notification.showErrorNotification(
        result?.message || 'Erreur lors de la mise à jour de la devise',
      );
    }
  } catch {
    notification.showErrorNotification('Erreur lors de la mise à jour de la devise');
  } finally {
    isSaving.value = false;
  }
};

onMounted(fetchCurrency);
</script>
