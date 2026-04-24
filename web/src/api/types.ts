export type OrderStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ProductSummaryResponse {
  id: string;
  name: string;
  price: number;
  currency: string;
  stock: number;
  isActive: boolean;
  categoryId: string;
}

export interface ProductResponse extends ProductSummaryResponse {
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface PagedProductResult {
  items: ProductSummaryResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface OrderLineDto {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderAddressDto {
  street: string;
  city: string;
  zipCode: string;
  country: string;
}

export interface OrderDto {
  id: string;
  customerId: string;
  status: OrderStatus;
  total: number;
  currency: string;
  createdAt: string;
  confirmedAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
  lines: OrderLineDto[];
  shippingAddress: OrderAddressDto;
}

export interface OrderSummaryDto {
  id: string;
  customerId: string;
  customerEmail: string;
  status: OrderStatus;
  total: number;
  currency: string;
  lineCount: number;
  firstItemName: string | null;
  createdAt: string;
  confirmedAt: string | null;
  shippingCity: string | null;
}

export interface CreateOrderItemRequest {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  currency: string;
}

export interface CreateOrderShippingRequest {
  street: string;
  city: string;
  zipCode: string;
  country: string;
}

export interface CreateOrderRequest {
  customerId: string;
  customerEmail: string;
  items: CreateOrderItemRequest[];
  shippingAddress: CreateOrderShippingRequest;
}

export interface CancelOrderRequest {
  reason: string;
}

export type SagaCurrentState =
  | 'Initial'
  | 'AwaitingStock'
  | 'AwaitingPayment'
  | 'Confirmed'
  | 'Cancelled'
  | 'Failed'
  | string;

export interface SagaState {
  orderId: string;
  state: SagaCurrentState;
  paymentId: string | null;
  failureReason: string | null;
  createdAt: string;
  completedAt: string | null;
}
