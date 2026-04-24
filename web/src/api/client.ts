export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;
  readonly correlationId: string | null;

  constructor(status: number, message: string, body: unknown, correlationId: string | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
    this.correlationId = correlationId;
  }
}

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `demo-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  signal?: AbortSignal;
}

export async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, signal } = opts;

  const headers: Record<string, string> = {
    Accept: 'application/json',
    'X-Correlation-Id': newCorrelationId(),
  };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const response = await fetch(path, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal,
  });

  const correlationId = response.headers.get('X-Correlation-Id');

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  let parsed: unknown = null;
  if (text.length > 0) {
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = text;
    }
  }

  if (!response.ok) {
    const msg =
      (typeof parsed === 'object' && parsed !== null && 'title' in parsed && typeof (parsed as { title: unknown }).title === 'string'
        ? (parsed as { title: string }).title
        : null) ?? `HTTP ${response.status} ${response.statusText}`;
    throw new ApiError(response.status, msg, parsed, correlationId);
  }

  return parsed as T;
}
