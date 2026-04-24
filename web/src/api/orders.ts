import { request } from './client';
import type {
  CancelOrderRequest,
  CreateOrderRequest,
  OrderDto,
  OrderStatus,
  OrderSummaryDto,
  OutboxMessageDto,
  PagedResult,
  SagaState,
} from './types';

export interface ListOrdersParams {
  status?: OrderStatus;
  page?: number;
  pageSize?: number;
}

export function listOrders(params: ListOrdersParams = {}, signal?: AbortSignal) {
  const search = new URLSearchParams();
  if (params.status) search.set('status', params.status);
  if (params.page !== undefined) search.set('page', String(params.page));
  if (params.pageSize !== undefined) search.set('pageSize', String(params.pageSize));
  const qs = search.toString();
  return request<PagedResult<OrderSummaryDto>>(`/api/orders${qs ? `?${qs}` : ''}`, { signal });
}

export function getOrder(id: string, signal?: AbortSignal) {
  return request<OrderDto>(`/api/orders/${id}`, { signal });
}

export function createOrder(body: CreateOrderRequest) {
  return request<OrderDto>('/api/orders', { method: 'POST', body });
}

export function confirmOrder(id: string) {
  return request<void>(`/api/orders/${id}/confirm`, { method: 'POST' });
}

export function cancelOrder(id: string, body: CancelOrderRequest) {
  return request<void>(`/api/orders/${id}/cancel`, { method: 'POST', body });
}

export function getSagaState(id: string, signal?: AbortSignal) {
  return request<SagaState>(`/api/orders/${id}/saga-state`, { signal });
}

export function getRecentOutbox(limit = 30, signal?: AbortSignal) {
  return request<OutboxMessageDto[]>(`/api/orders/outbox/recent?limit=${limit}`, { signal });
}
