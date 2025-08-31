export interface PaginatedResult<T> extends Omit<ResponseResult<T>, 'data'> {
  data: T[];
  pagination: Pagination;
}

export interface Pagination {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface ResponseResult<T> {
  data?: T;
  isSuccess: boolean;
  message: string;
  statusCode: number;
  errors: string[];
}

export interface VoidResponseResult {
  isSuccess: boolean;
  message: string;
  statusCode: number;
  errors: string[];
}

export interface Filter {
  pageNumber?: number;
  pageSize?: number;
  searchTerms?: string;
  sortBy?: string;
  sortDirection?: 'Ascending' | 'Descending';
}
