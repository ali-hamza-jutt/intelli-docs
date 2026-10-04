import type { ConversationSummaryResponse, DocumentResponse } from "@/lib/api/model";

export function latestConversation(conversations: ConversationSummaryResponse[]) {
  return conversations.reduce<ConversationSummaryResponse | undefined>(
    (latest, conversation) =>
      !latest || Date.parse(conversation.updatedAt) > Date.parse(latest.updatedAt)
        ? conversation
        : latest,
    undefined,
  );
}

export function selectedReadyDocument(documents: DocumentResponse[], selectedId: string) {
  const ready = documents.filter((document) => document.status === "Completed");
  return ready.find((document) => document.id === selectedId) ?? (ready.length === 1 ? ready[0] : undefined);
}

export function dashboardChatUrl(documentId: string, question: string, launchId: string) {
  return `/chat?${new URLSearchParams({ new: "1", documentId, q: question.trim(), ask: launchId })}`;
}

export function shouldResumeLatest(params: Pick<URLSearchParams, "has">) {
  return !["c", "documentId", "new", "q", "ask"].some((key) => params.has(key));
}

const claimed = new Set<string>();

/** Claim before making a request: effects, Back/Forward and reload must not send it again. */
export function claimChatLaunch(userId: string, launchId: string, storage?: Storage) {
  const key = `documind:chat-launch:${userId}:${launchId}`;
  if (claimed.has(key)) return false;

  try {
    const session = storage ?? window.sessionStorage;
    if (session.getItem(key)) return false;
    session.setItem(key, "sent");
  } catch {
    // Navigation still works when browser storage is unavailable. The in-memory claim and
    // removal of the launch URL prevent repeated sends during this page's lifetime.
  }

  claimed.add(key);
  return true;
}
