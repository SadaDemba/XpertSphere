export interface PaginatedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface Filter {
  pageNumber?: number;
  pageSize?: number;
  searchTerms?: string;
  sortBy?: string;
  sortDirection?: 'Ascending' | 'Descending';
}
