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

/**
 * What the API returns for a failure: RFC-9457 ProblemDetails, with two additions of its own.
 * `detail` is the sentence written for the reader; `errorCode` is the stable string to branch on.
 * `traceId` appears in the server's log for the same request, so it is worth showing in a support
 * message and never worth showing in place of `detail`.
 */
export type ApiErrorBody = {
  title?: string;
  detail?: string;
  status?: number;
  errorCode?: string;
  traceId?: string;
  /** Per-field messages when a request was rejected for its contents. */
  errors?: Record<string, string[]>;
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

  const isFormData = typeof FormData !== "undefined" && config.data instanceof FormData;

  const send = () => {
    const headers = new Headers(config.headers);

    if (accessToken) {
      headers.set("Authorization", `Bearer ${accessToken}`);
    }

    // The browser must set multipart Content-Type itself so it can append the boundary;
    // the generated code sets a boundary-less value that would make the body unparseable.
    if (isFormData) {
      headers.delete("Content-Type");
    }

    return fetch(`${API_BASE_URL}${config.url}${query}`, {
      method: config.method.toUpperCase(),
      headers,
      body: config.data === undefined
        ? undefined
        : isFormData
          ? (config.data as FormData)
          : JSON.stringify(config.data),
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

export async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as ApiErrorBody;

    // detail first: it is the sentence written for this failure. title is a category ("Not found")
    // and makes a poor message on its own, so it is only the fallback.
    return new ApiError(
      response.status,
      body.detail ?? body.title ?? "Something went wrong.",
      body.errorCode ?? "UNKNOWN_ERROR",
    );
  } catch {
    return new ApiError(response.status, "Something went wrong.", "UNKNOWN_ERROR");
  }
}
