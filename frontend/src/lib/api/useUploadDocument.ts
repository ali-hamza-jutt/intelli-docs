"use client";

import { useCallback, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  API_BASE_URL,
  ApiError,
  getAccessToken,
  refreshAccessToken,
} from "@/lib/api/client";
import { getGetApiDocumentsQueryKey } from "@/lib/api/generated/documents/documents";
import type { DocumentResponse, UploadTicketResponse } from "@/lib/api/model";

/**
 * Direct-to-provider upload, in three steps:
 *
 *   1. ask our API for a signed ticket — it decides where the file may go
 *   2. POST the file straight to the provider, never through our API
 *   3. tell our API the upload finished, so it can verify and register the document
 *
 * The bytes never touch our server, which is the point: no request-size limits, no bandwidth
 * cost, no worker tied up for the duration of a 20 MB upload.
 *
 * XMLHttpRequest rather than fetch, because only XHR reports upload progress.
 */
export function useUploadDocument() {
  const [progress, setProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const requestRef = useRef<XMLHttpRequest | null>(null);
  const queryClient = useQueryClient();

  const cancel = useCallback(() => {
    requestRef.current?.abort();
    requestRef.current = null;
    setIsUploading(false);
    setProgress(0);
  }, []);

  const upload = useCallback(
    async (file: File): Promise<DocumentResponse> => {
      setIsUploading(true);
      setProgress(0);

      try {
        let document: DocumentResponse;

        // ---- 1. signed ticket -------------------------------------------------
        let ticket: UploadTicketResponse | null = null;

        try {
          ticket = await callApi<UploadTicketResponse>("/api/Documents/upload-ticket", {
            fileName: file.name,
            fileSize: file.size,
          });
        } catch (error) {
          // The server is on local disk, which has no provider to upload to. Anything else —
          // a file too large, a rejected extension — is a real failure and must surface.
          if (
            !(error instanceof ApiError) ||
            error.errorCode !== "DIRECT_UPLOAD_UNAVAILABLE"
          ) {
            throw error;
          }
        }

        if (ticket) {
          // ---- 2. straight to the provider ------------------------------------
          await sendToProvider(file, ticket, requestRef, setProgress);

          // ---- 3. register it -------------------------------------------------
          document = await callApi<DocumentResponse>("/api/Documents/confirm", {
            publicId: ticket.publicId,
            fileName: file.name,
          });
        } else {
          // Local provider: the file goes through our own API instead.
          document = await sendToApi(file, requestRef, setProgress);
        }

        await queryClient.invalidateQueries({ queryKey: getGetApiDocumentsQueryKey() });

        return document;
      } finally {
        requestRef.current = null;
        setIsUploading(false);
      }
    },
    [queryClient],
  );

  return { upload, cancel, progress, isUploading };
}

/** POSTs JSON to our own API, refreshing the access token once on 401. */
async function callApi<T>(path: string, body: unknown): Promise<T> {
  const send = () =>
    fetch(`${API_BASE_URL}${path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(getAccessToken() ? { Authorization: `Bearer ${getAccessToken()}` } : {}),
      },
      credentials: "include",
      body: JSON.stringify(body),
    });

  let response = await send();

  if (response.status === 401 && (await refreshAccessToken())) {
    response = await send();
  }

  if (!response.ok) {
    const text = await response.text();

    try {
      const parsed = JSON.parse(text) as { message?: string; title?: string; errorCode?: string };
      throw new ApiError(
        response.status,
        parsed.message ?? parsed.title ?? "Upload failed.",
        parsed.errorCode ?? "UPLOAD_FAILED",
      );
    } catch (error) {
      if (error instanceof ApiError) throw error;
      throw new ApiError(response.status, "Upload failed.", "UPLOAD_FAILED");
    }
  }

  return (await response.json()) as T;
}

/**
 * Uploads to the provider using the signed ticket. The signature covers public_id and timestamp,
 * so those fields must be sent exactly as issued — changing either invalidates it.
 */
function sendToProvider(
  file: File,
  ticket: UploadTicketResponse,
  requestRef: React.RefObject<XMLHttpRequest | null>,
  onProgress: (percent: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    requestRef.current = request;

    request.open("POST", ticket.uploadUrl);

    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    };

    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        resolve();
        return;
      }

      reject(
        new ApiError(
          request.status,
          readProviderError(request.responseText),
          "PROVIDER_UPLOAD_FAILED",
        ),
      );
    };

    request.onerror = () =>
      reject(new ApiError(0, "Network error during upload.", "NETWORK_ERROR"));
    request.onabort = () => reject(new ApiError(0, "Upload cancelled.", "UPLOAD_CANCELLED"));

    const form = new FormData();
    form.append("file", file);
    form.append("api_key", ticket.apiKey);
    form.append("timestamp", String(ticket.timestamp));
    form.append("public_id", ticket.publicId);
    form.append("signature", ticket.signature);

    request.send(form);
  });
}

function readProviderError(body: string): string {
  try {
    const parsed = JSON.parse(body) as { error?: { message?: string } };
    return parsed.error?.message ?? "The storage provider rejected the upload.";
  } catch {
    return "The storage provider rejected the upload.";
  }
}

/**
 * Multipart upload through our own API, used when the server stores files locally. Same progress
 * reporting and the same one-shot token refresh as the direct path.
 */
function sendToApi(
  file: File,
  requestRef: React.RefObject<XMLHttpRequest | null>,
  onProgress: (percent: number) => void,
): Promise<DocumentResponse> {
  const attempt = (token: string | null) =>
    new Promise<{ status: number; body: string }>((resolve, reject) => {
      const request = new XMLHttpRequest();
      requestRef.current = request;

      request.open("POST", `${API_BASE_URL}/api/Documents/upload`);
      request.withCredentials = true;

      if (token) {
        request.setRequestHeader("Authorization", `Bearer ${token}`);
      }

      // No Content-Type header — the browser adds the multipart boundary.
      request.upload.onprogress = (event) => {
        if (event.lengthComputable) {
          onProgress(Math.round((event.loaded / event.total) * 100));
        }
      };

      request.onload = () => resolve({ status: request.status, body: request.responseText });
      request.onerror = () =>
        reject(new ApiError(0, "Network error during upload.", "NETWORK_ERROR"));
      request.onabort = () => reject(new ApiError(0, "Upload cancelled.", "UPLOAD_CANCELLED"));

      const form = new FormData();
      form.append("file", file);
      request.send(form);
    });

  return (async () => {
    let response = await attempt(getAccessToken());

    if (response.status === 401 && (await refreshAccessToken())) {
      response = await attempt(getAccessToken());
    }

    if (response.status < 200 || response.status >= 300) {
      try {
        const parsed = JSON.parse(response.body) as { message?: string; errorCode?: string };
        throw new ApiError(
          response.status,
          parsed.message ?? "Upload failed.",
          parsed.errorCode ?? "UPLOAD_FAILED",
        );
      } catch (error) {
        if (error instanceof ApiError) throw error;
        throw new ApiError(response.status, "Upload failed.", "UPLOAD_FAILED");
      }
    }

    return JSON.parse(response.body) as DocumentResponse;
  })();
}
