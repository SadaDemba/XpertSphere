export interface PaginatedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface JobPaginatedResult<T> {
  items: T[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPrevious: boolean;
    hasNext: boolean;
  };
  isSuccess: boolean;
  message: string;
  errors: [];
}

export interface Filter {
  pageNumber?: number;
  pageSize?: number;
  searchTerms?: string;
  sortBy?: string;
  sortDirection?: 'Ascending' | 'Descending';
}
