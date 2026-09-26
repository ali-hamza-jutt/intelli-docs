/**
 * The streaming answer call, written by hand rather than generated.
 *
 * Two reasons it is not an `EventSource`: that can only issue GETs, so it cannot carry the question
 * in a body, and it cannot set an Authorization header. `fetch` with a readable body does both, and
 * an AbortController gives the Stop button something real to cancel — aborting the fetch closes the
 * connection, which is what the server watches to stop the answer and save what it had.
 */

import {
  API_BASE_URL,
  ApiError,
  getAccessToken,
  refreshAccessToken,
  setAccessToken,
} from "@/lib/api/client";
import type { ChatMessageResponse } from "@/lib/api/model";

type StreamOptions = {
  conversationId: string;
  question: string;
  signal: AbortSignal;
  /** The next piece of the answer. Called many times, in order. */
  onDelta: (text: string) => void;
  /** The stored message, once the answer is complete. Not called if the stream is stopped. */
  onFinal: (message: ChatMessageResponse) => void;
};

export async function streamAnswer({
  conversationId,
  question,
  signal,
  onDelta,
  onFinal,
}: StreamOptions): Promise<void> {
  const send = () =>
    fetch(`${API_BASE_URL}/api/Conversations/${conversationId}/messages/stream`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(getAccessToken() ? { Authorization: `Bearer ${getAccessToken()}` } : {}),
      },
      body: JSON.stringify({ question }),
      credentials: "include",
      signal,
    });

  let response = await send();

  // The same one-shot refresh the generated calls get, for a token that expired mid-conversation.
  if (response.status === 401) {
    if (await refreshAccessToken()) {
      response = await send();
    } else {
      setAccessToken(null);
    }
  }

  if (!response.ok || !response.body) {
    throw await toApiError(response);
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();

    if (done) break;

    buffer += decoder.decode(value, { stream: true });

    // Frames are separated by a blank line. A partial frame stays in the buffer until the rest of
    // it arrives, which is the whole reason this is parsed rather than split once at the end.
    let boundary = buffer.indexOf("\n\n");

    while (boundary !== -1) {
      handleFrame(buffer.slice(0, boundary), onDelta, onFinal);
      buffer = buffer.slice(boundary + 2);
      boundary = buffer.indexOf("\n\n");
    }
  }
}

function handleFrame(
  frame: string,
  onDelta: (text: string) => void,
  onFinal: (message: ChatMessageResponse) => void,
) {
  const name = frame.match(/^event: (.+)$/m)?.[1];
  const data = frame.match(/^data: (.+)$/m)?.[1];

  if (!name || !data) return;

  if (name === "delta") {
    onDelta((JSON.parse(data) as { text: string }).text);
    return;
  }

  if (name === "final") {
    onFinal(JSON.parse(data) as ChatMessageResponse);
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as { message?: string; errorCode?: string };

    return new ApiError(
      response.status,
      body.message ?? "Could not answer that question.",
      body.errorCode ?? "UNKNOWN_ERROR",
    );
  } catch {
    return new ApiError(response.status, "Could not answer that question.", "UNKNOWN_ERROR");
  }
}
