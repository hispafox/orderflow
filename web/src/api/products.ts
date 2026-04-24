import { request } from './client';
import type { PagedProductResult, ProductResponse } from './types';

export interface ListProductsParams {
  page?: number;
  pageSize?: number;
}

export function listProducts(params: ListProductsParams = {}, signal?: AbortSignal) {
  const search = new URLSearchParams();
  if (params.page !== undefined) search.set('page', String(params.page));
  if (params.pageSize !== undefined) search.set('pageSize', String(params.pageSize));
  const qs = search.toString();
  return request<PagedProductResult>(`/api/products${qs ? `?${qs}` : ''}`, { signal });
}

export function getProduct(id: string, signal?: AbortSignal) {
  return request<ProductResponse>(`/api/products/${id}`, { signal });
}
