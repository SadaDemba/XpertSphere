<template>
  <q-form class="q-gutter-md" @submit="onSubmit">
    <div class="row q-gutter-md">
      <q-input
        v-model="form.name"
        label="Nom de l'organisation *"
        outlined
        class="col"
        :rules="[(val) => !!val || 'Le nom est requis']"
      />
      <q-input v-model="form.email" label="Email" type="email" outlined class="col" />
    </div>

    <q-input
      v-model="form.description"
      label="Description *"
      type="textarea"
      outlined
      rows="3"
      :rules="[(val) => !!val || 'La description est requise']"
    />

    <div class="row q-gutter-md">
      <q-input
        v-model="form.website"
        label="Site Web"
        outlined
        class="col"
        hint="Ex: https://www.exemple.com"
      />
      <q-input v-model="form.phone" label="Téléphone" outlined class="col" />
    </div>

    <q-separator class="q-my-md" />
    <div class="text-subtitle2 q-mb-md">Adresse</div>

    <q-input v-model="form.address.street" label="Rue" outlined />

    <q-input
      v-model="form.address.complement"
      label="Complément d'adresse"
      outlined
      hint="Bâtiment, étage, appartement..."
    />

    <div class="row q-gutter-md">
      <q-input v-model="form.address.city" label="Ville" outlined class="col" />
      <q-input v-model="form.address.postalCode" label="Code Postal" outlined class="col-3" />
    </div>

    <div class="row q-gutter-md">
      <q-input v-model="form.address.region" label="Région" outlined class="col" />
      <q-input v-model="form.address.country" label="Pays" outlined class="col" />
    </div>

    <div class="row justify-end q-gutter-sm q-mt-lg">
      <q-btn flat label="Annuler" @click="$emit('cancel')" />
      <q-btn
        type="submit"
        color="primary"
        :label="isEditing ? 'Mettre à jour' : 'Créer'"
        :loading="loading"
      />
    </div>
  </q-form>
</template>

<script setup lang="ts">
import { reactive, watch, computed } from 'vue';
import type {
  CreateOrganizationDto,
  UpdateOrganizationDto,
  OrganizationDto,
} from '../../models/organization';
import type { CreateAddressDto, UpdateAddressDto } from '../../models/address';

interface Props {
  organization?: OrganizationDto | null;
  loading?: boolean;
}

interface Emits {
  (e: 'submit', data: CreateOrganizationDto | UpdateOrganizationDto): void;
  (e: 'cancel'): void;
}

const props = withDefaults(defineProps<Props>(), {
  organization: null,
  loading: false,
});

const emit = defineEmits<Emits>();

const form = reactive({
  name: '',
  description: '',
  website: '',
  email: '',
  phone: '',
  address: {
    street: '',
    complement: '',
    city: '',
    postalCode: '',
    region: '',
    country: '',
  },
});

const isEditing = computed(() => !!props.organization);

const resetForm = () => {
  Object.assign(form, {
    name: '',
    description: '',
    website: '',
    email: '',
    phone: '',
    address: {
      street: '',
      complement: '',
      city: '',
      postalCode: '',
      region: '',
      country: '',
    },
  });
};

const loadOrganization = (organization: OrganizationDto) => {
  Object.assign(form, {
    name: organization.name,
    description: organization.description,
    website: organization.website || '',
    email: organization.contactEmail || '',
    phone: organization.contactPhone || '',
    address: {
      street: organization.address?.streetName || '',
      complement: organization.address?.addressLine2 || '',
      city: organization.address?.city || '',
      postalCode: organization.address?.postalCode || '',
      region: organization.address?.region || '',
      country: organization.address?.country || '',
    },
  });
};

const onSubmit = () => {
  const hasAddressData =
    form.address.street ||
    form.address.city ||
    form.address.postalCode ||
    form.address.country ||
    form.address.region ||
    form.address.complement;

  const addressData: CreateAddressDto | UpdateAddressDto | undefined = hasAddressData
    ? {
        streetName: form.address.street,
        addressLine2: form.address.complement,
        city: form.address.city,
        postalCode: form.address.postalCode,
        region: form.address.region,
        country: form.address.country,
      }
    : undefined;

  const data: CreateOrganizationDto | UpdateOrganizationDto = {
    name: form.name,
    description: form.description,
    website: form.website,
    contactEmail: form.email,
    contactPhone: form.phone,
    address: addressData!,
  };

  emit('submit', data);
};

watch(
  () => props.organization,
  (newOrg) => {
    if (newOrg) {
      loadOrganization(newOrg);
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

defineExpose({
  resetForm,
});
</script>
