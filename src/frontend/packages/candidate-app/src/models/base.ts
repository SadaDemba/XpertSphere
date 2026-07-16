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

/**
 * Shape of an ASP.NET Core `ValidationProblemDetails` response (RFC 9110),
 * returned automatically when `[ApiController]` model binding/validation fails
 * (e.g. `[Required]` DataAnnotations on a DTO such as `CreateTrainingDto`).
 */
export interface ValidationProblemDetails {
  type?: string;
  title: string;
  status: number;
  errors: Record<string, string[]>;
  traceId?: string;
}
