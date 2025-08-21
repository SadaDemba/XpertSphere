<template>
  <div v-if="shouldShowPagination" class="app-pagination">
    <q-pagination
      v-model="internalCurrentPage"
      :max="totalPages"
      :max-pages="maxPages"
      :boundary-links="boundaryLinks"
      :direction-links="directionLinks"
      :size="size"
      :color="color"
      :flat="flat"
      :outline="outline"
      :unelevated="unelevated"
      :rounded="rounded"
      :glossy="glossy"
      :dense="dense"
      :disable="disable"
      @update:model-value="handlePageChange"
    />

    <div v-if="showInfo" class="pagination-info q-mt-sm text-center text-caption text-grey-6">
      {{ paginationText }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

interface Props {
  currentPage: number;
  totalPages: number;
  totalItems: number;

  showInfo?: boolean;
  itemName?: string;
  itemNamePlural?: string;

  maxPages?: number;
  boundaryLinks?: boolean;
  directionLinks?: boolean;
  size?: string;
  color?: string;
  flat?: boolean;
  outline?: boolean;
  unelevated?: boolean;
  rounded?: boolean;
  glossy?: boolean;
  dense?: boolean;
  disable?: boolean;

  hideOnSinglePage?: boolean;
  minItemsToShow?: number;
}

interface Emits {
  (e: 'page-change', page: number): void;
  (e: 'update:current-page', page: number): void;
}

const props = withDefaults(defineProps<Props>(), {
  showInfo: true,
  itemName: 'élément',
  itemNamePlural: 'éléments',
  maxPages: 7,
  boundaryLinks: true,
  directionLinks: true,
  size: 'md',
  color: 'primary',
  flat: false,
  outline: false,
  unelevated: false,
  rounded: false,
  glossy: false,
  dense: false,
  disable: false,
  hideOnSinglePage: true,
  minItemsToShow: 1,
});

const emit = defineEmits<Emits>();

const internalCurrentPage = computed({
  get: () => props.currentPage,
  set: (value) => {
    emit('update:current-page', value);
    emit('page-change', value);
  },
});

const shouldShowPagination = computed(() => {
  if (props.totalPages <= 0) return false;
  if (props.hideOnSinglePage && props.totalPages <= 1) return false;
  if (props.totalItems !== undefined && props.totalItems < props.minItemsToShow) return false;
  return true;
});

const paginationText = computed(() => {
  if (!props.totalItems) {
    return `Page ${props.currentPage} sur ${props.totalPages}`;
  }

  const itemName = props.totalItems > 1 ? props.itemNamePlural : props.itemName;
  return `Page ${props.currentPage} sur ${props.totalPages} (${props.totalItems} ${itemName} au total)`;
});

const handlePageChange = (page: number) => {
  emit('page-change', page);
  emit('update:current-page', page);
};
</script>

<style scoped>
.app-pagination {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.pagination-info {
  font-size: 0.75rem;
  line-height: 1.2;
}

@media (max-width: 599px) {
  .app-pagination {
    margin-top: 24px;
  }

  .pagination-info {
    font-size: 0.7rem;
  }
}

/* Responsive pour petits écrans */
@media (max-width: 480px) {
  .app-pagination :deep(.q-pagination) {
    font-size: 0.875rem;
  }

  .app-pagination :deep(.q-pagination .q-btn) {
    min-width: 36px;
    min-height: 36px;
  }
}
</style>
