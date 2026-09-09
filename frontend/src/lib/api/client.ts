/**
 * The single fetch path for every generated API hook.
 *
 * Two things happen here that the generated code should not have to know about:
 *  - the access token is attached from memory, never read from storage;
 *  - a 401 triggers one silent refresh against the httpOnly cookie, then one retry.
 *
 * The config-object signature is the shape orval generates calls for. It throws on failure so
 * TanStack Query reports errors normally, and resolves to the response body on success.
 */

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5100";

/** Shape the API returns for handled failures. */
export type ApiErrorBody = {
  success: false;
  message: string;
  errorCode: string;
};

export class ApiError extends Error {
  readonly status: number;
  readonly errorCode: string;

  constructor(status: number, message: string, errorCode: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errorCode = errorCode;
  }
}

/*
 * The access token lives in module scope rather than localStorage: a script injected into the
 * page cannot read it, and it disappears when the tab closes. The AuthProvider re-acquires one
 * on load by calling /refresh against the cookie.
 */
let accessToken: string | null = null;

/** Called by the auth layer whenever a token is issued or cleared. */
export function setAccessToken(token: string | null) {
  accessToken = token;
}

export function getAccessToken() {
  return accessToken;
}

/** Invoked when a refresh fails, so the app can drop back to the signed-out state. */
let onAuthExpired: (() => void) | null = null;

export function setAuthExpiredHandler(handler: (() => void) | null) {
  onAuthExpired = handler;
}

/** Endpoints that must never trigger the refresh-and-retry loop. */
const AUTH_PATHS = ["/api/auth/refresh", "/api/auth/login", "/api/auth/register"];

/**
 * De-duplicates concurrent refreshes. Without this, five queries failing at once would fire five
 * refresh calls — and because refresh tokens rotate, four would be rejected as reuse and revoke
 * the whole chain, logging the user out.
 */
let refreshInFlight: Promise<boolean> | null = null;

export async function refreshAccessToken(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: "POST",
        credentials: "include",
      });

      if (!response.ok) return false;

      const body = (await response.json()) as { accessToken: string };
      setAccessToken(body.accessToken);
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();

  return refreshInFlight;
}

/** The call shape orval generates. */
export type RequestConfig = {
  url: string;
  method: string;
  params?: Record<string, unknown>;
  data?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
};

/** The mutator orval calls for every generated operation. */
export async function apiRequest<T>(config: RequestConfig): Promise<T> {
  const query = config.params
    ? `?${new URLSearchParams(
        Object.entries(config.params)
          .filter(([, value]) => value !== undefined && value !== null)
          .map(([key, value]) => [key, String(value)]),
      )}`
    : "";

  const send = () => {
    const headers = new Headers(config.headers);

    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    return fetch(`${API_BASE_URL}${config.url}${query}`, {
      method: config.method.toUpperCase(),
      headers,
      body: config.data === undefined ? undefined : JSON.stringify(config.data),
      // Sends the refresh cookie on auth calls; harmless elsewhere.
      credentials: "include",
      signal: config.signal,
    });
  };

  let response = await send();

  // One retry after a silent refresh, and never on the auth endpoints themselves.
  if (response.status === 401 && !AUTH_PATHS.includes(config.url)) {
    if (await refreshAccessToken()) {
      response = await send();
    } else {
      setAccessToken(null);
      onAuthExpired?.();
    }
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  // 204 and other empty bodies would fail JSON.parse.
  if (response.status === 204 || response.headers.get("content-length") === "0") {
    return undefined as T;
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as Partial<ApiErrorBody> & { title?: string };

    return new ApiError(
      response.status,
      body.message ?? body.title ?? "Something went wrong.",
      body.errorCode ?? "UNKNOWN_ERROR",
    );
  } catch {
    return new ApiError(response.status, "Something went wrong.", "UNKNOWN_ERROR");
  }
}
