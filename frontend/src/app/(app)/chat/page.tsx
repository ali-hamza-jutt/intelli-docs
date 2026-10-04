"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Icon } from "@/components/ui/Icon";
import {
  AssistantMessage,
  StreamingMessage,
  ThinkingIndicator,
  UserMessage,
} from "@/components/chat/ChatMessage";
import { ChatComposer } from "@/components/chat/ChatComposer";
import { ConversationPane } from "@/components/chat/ConversationPane";
import { SourceRail, SourceSheet } from "@/components/chat/SourcePanel";
import { UploadDialog } from "@/components/documents/UploadDialog";
import { useGetApiDocuments } from "@/lib/api/generated/documents/documents";
import {
  useGetApiConversations,
  useGetApiConversationsId,
  usePostApiConversations,
  useDeleteApiConversationsId,
  getGetApiConversationsQueryKey,
  getGetApiConversationsIdQueryKey,
} from "@/lib/api/generated/conversations/conversations";
import { ApiError } from "@/lib/api/client";
import { streamAnswer } from "@/lib/api/stream";
import { SUGGESTIONS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";
import { Button } from "@/components/ui/Button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { claimChatLaunch, latestConversation, shouldResumeLatest } from "@/lib/chatEntry";
import type { ConversationSummaryResponse, MessageSourceResponse } from "@/lib/api/model";

function ChatScreen() {
  const params = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const toast = useToast();
  const queryClient = useQueryClient();
  const { user } = useAuth();

  // Which thread is open, or which document a new one would be about. Both live in the URL, so a
  // reload — the thing this module exists to survive — lands back in the same place.
  const requestedConversationId = params.get("c");
  const documentId = params.get("documentId");
  const incomingQuestion = params.get("q") ?? "";
  const launchId = params.get("ask");
  const resumeLatest = shouldResumeLatest(params);

  const [draft, setDraft] = useState(incomingQuestion);
  const incomingKey = incomingQuestion ? `${launchId ?? "draft"}:${incomingQuestion}` : null;
  const [loadedQuestion, setLoadedQuestion] = useState(incomingKey);

  // Search-parameter navigation can reuse this screen. Adopt a new dashboard question once,
  // while keeping subsequent edits when a document is selected or the launch URL is cleared.
  if (incomingKey !== loadedQuestion) {
    setLoadedQuestion(incomingKey);
    if (incomingKey) setDraft(incomingQuestion);
  }
  const [openSource, setOpenSource] = useState<MessageSourceResponse | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);

  // The question is shown immediately while the server works, then dropped when the saved thread
  // comes back holding it. Nothing is kept in two places for longer than one round trip.
  const [pendingQuestion, setPendingQuestion] = useState<string | null>(null);

  // The answer as it arrives. Null until the first piece of text, so the wait before it reads as
  // "searching" rather than as an empty answer.
  const [streamingAnswer, setStreamingAnswer] = useState<string | null>(null);

  const scrollRef = useRef<HTMLDivElement>(null);

  /** Held so Stop can abort the request the answer is arriving on. */
  const streamRef = useRef<{
    controller: AbortController;
    threadId: string | null;
    documentId: string | null;
  } | null>(null);

  const conversations = useGetApiConversations();
  const conversationId = requestedConversationId ??
    (resumeLatest ? latestConversation(conversations.data ?? [])?.id ?? null : null);
  const documents = useGetApiDocuments();
  const ready = (documents.data ?? []).filter((document) => document.status === "Completed");

  const conversation = useGetApiConversationsId(conversationId ?? "", {
    query: { enabled: Boolean(conversationId) },
  });

  const start = usePostApiConversations();
  const remove = useDeleteApiConversationsId();

  // Pending covers the whole exchange: creating the thread, the wait before the first word, and the
  // answer still arriving.
  const pending = pendingQuestion !== null;
  const messages = conversation.data?.messages ?? [];
  const activeDocumentId = conversation.data?.documentId ?? documentId;

  const clearExchange = useCallback(() => {
    const active = streamRef.current;
    streamRef.current = null;
    active?.controller.abort();
    setOpenSource(null);
    setPendingQuestion(null);
    setStreamingAnswer(null);
  }, []);

  useEffect(() => {
    if (resumeLatest && conversationId) {
      router.replace(`${pathname}?c=${encodeURIComponent(conversationId)}`);
    }
  }, [resumeLatest, conversationId, pathname, router]);

  // A newly created thread may change its own URL without stopping its answer. Other route
  // changes must detach the old stream so it cannot write into the newly opened conversation.
  useEffect(() => {
    const active = streamRef.current;
    if (active && (conversationId ? conversationId !== active.threadId : documentId !== active.documentId)) {
      queueMicrotask(() => {
        if (streamRef.current === active) clearExchange();
      });
    }
  }, [conversationId, documentId, clearExchange]);

  useEffect(() => () => {
    streamRef.current?.controller.abort();
    streamRef.current = null;
  }, []);

  const openConversation = useCallback(
    (id: string) => {
      clearExchange();
      setDraft("");
      router.replace(`${pathname}?c=${encodeURIComponent(id)}`);
    },
    [clearExchange, pathname, router],
  );

  const refreshThread = useCallback(
    (id: string) => {
      queryClient.invalidateQueries({ queryKey: getGetApiConversationsIdQueryKey(id) });
      queryClient.invalidateQueries({ queryKey: getGetApiConversationsQueryKey() });
    },
    [queryClient],
  );

  const failed = useCallback(
    (error: unknown, question: string) => {
      setPendingQuestion(null);
      setDraft(question);
      toast(error instanceof ApiError ? error.message : "Could not answer that question", "warn");
    },
    [toast],
  );

  const send = useCallback(
    async (text: string) => {
      const question = text.trim();

      if (!question || pending || streamRef.current) return;

      if (!conversationId && !documentId) {
        toast("Choose a document to ask about first", "warn");
        return;
      }

      setDraft("");
      setOpenSource(null);
      setPendingQuestion(question);
      setStreamingAnswer(null);

      const controller = new AbortController();
      const exchange = { controller, threadId: conversationId, documentId };
      let receivedAnswer = false;
      streamRef.current = exchange;

      // Consume the dashboard handoff before creating a thread. Refreshing this URL then opens
      // an ordinary new chat, and a failed request leaves the question available for retry.
      if (incomingQuestion || launchId) {
        const target = conversationId
          ? new URLSearchParams({ c: conversationId })
          : new URLSearchParams({ new: "1", ...(documentId ? { documentId } : {}) });
        router.replace(`${pathname}?${target}`);
      }

      try {
        // A thread has to exist before an answer can be streamed into it, so the first question
        // creates an empty one. It is created without a question so that this answer streams too,
        // rather than the first one arriving all at once.
        const threadId =
          conversationId ?? (await start.mutateAsync({ data: { documentId: documentId! } })).id;
        exchange.threadId = threadId;
        refreshThread(threadId);
        if (streamRef.current !== exchange) return;
        if (controller.signal.aborted) {
          setPendingQuestion(null);
          setStreamingAnswer(null);
          setDraft(question);
          return;
        }

        if (!conversationId) {
          router.replace(`${pathname}?c=${threadId}`);
        }

        await streamAnswer({
          conversationId: threadId,
          question,
          signal: controller.signal,
          onDelta: (piece) => {
            receivedAnswer = true;
            if (streamRef.current === exchange) setStreamingAnswer((sofar) => (sofar ?? "") + piece);
          },
          onFinal: () => undefined,
        });

        // The stored thread is the source of truth; what was rendered while streaming is dropped
        // in favour of it, sources and all.
        refreshThread(threadId);
        if (streamRef.current !== exchange) return;
        setPendingQuestion(null);
        setStreamingAnswer(null);
      } catch (error) {
        if (exchange.threadId) refreshThread(exchange.threadId);
        if (streamRef.current !== exchange) return;
        if (error instanceof DOMException && error.name === "AbortError") {
          // Stopped on purpose. The server keeps what it had written, so the thread is reloaded
          // rather than the partial text being left on screen unsaved.
          setPendingQuestion(null);
          setStreamingAnswer(null);

          // No words means the server drops the whole turn, so keep the question for retry. Once
          // words arrived, the server stores the stopped answer and reloading shows that saved turn.
          if (!receivedAnswer) setDraft(question);

          return;
        }

        failed(error, question);
        setStreamingAnswer(null);
      } finally {
        if (streamRef.current === exchange) streamRef.current = null;
      }
    },
    [conversationId, documentId, failed, incomingQuestion, launchId, pathname, pending, refreshThread, router, start, toast],
  );

  useEffect(() => {
    if (!launchId || !incomingQuestion.trim() || !user || conversationId ||
        !documents.isSuccess || !ready.some((document) => document.id === documentId)) return;

    // Defer until effect setup settles, so Strict Mode's setup/cleanup replay cannot cancel the
    // first submission and then accidentally send it a second time.
    let cancelled = false;
    queueMicrotask(() => {
      if (!cancelled && claimChatLaunch(user.id, launchId)) void send(incomingQuestion);
    });
    return () => { cancelled = true; };
  }, [launchId, incomingQuestion, user, conversationId, documents.isSuccess, ready, documentId, send]);

  const stop = useCallback(() => {
    streamRef.current?.controller.abort();
  }, []);

  const newChat = useCallback(() => {
    clearExchange();
    setDraft("");
    router.replace(`${pathname}?new=1`);
  }, [clearExchange, pathname, router]);

  const deleteConversation = useCallback(
    (id: string) => {
      remove.mutate(
        { id },
        {
          onSuccess: () => {
            toast("Conversation deleted");
            queryClient.invalidateQueries({ queryKey: getGetApiConversationsQueryKey() });

            if (id === conversationId) {
              clearExchange();
              setDraft("");
              queryClient.setQueryData<ConversationSummaryResponse[]>(getGetApiConversationsQueryKey(),
                (previous) => previous?.filter((item) => item.id !== id));
              router.replace(pathname);
            }
          },
        },
      );
    },
    [clearExchange, conversationId, pathname, queryClient, remove, router, toast],
  );

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages.length, pendingQuestion]);

  const lastQuestion =
    [...messages].reverse().find((message) => message.role === "User")?.content ?? "";
  const loadingChat = (resumeLatest && conversations.isPending) || (Boolean(conversationId) && conversation.isPending);
  const isEmpty = messages.length === 0 && !pending && !loadingChat &&
    !conversation.isError && !(resumeLatest && conversations.isError);

  return (
    <div className="flex h-[calc(100vh-140px)] overflow-hidden md:h-[calc(100vh-64px)]">
      <ConversationPane
        conversations={conversations.data ?? []}
        loading={conversations.isPending}
        activeId={conversationId}
        onSelect={openConversation}
        onDelete={deleteConversation}
        onNewChat={newChat}
      />

      <div className="flex min-w-[320px] flex-1 flex-col bg-canvas">
        <div className="flex items-center justify-between border-b border-line bg-surface px-5 py-2.5">
          <span className="truncate text-small font-medium">{conversation.data?.title ?? "New conversation"}</span>
          <Button variant="ghost" size="sm" icon="plus" className="xl:hidden" onClick={newChat}>New Chat</Button>
        </div>
        <div ref={scrollRef} className="flex-1 overflow-auto px-5 py-6">
          <div className="mx-auto flex max-w-[720px] flex-col gap-5">
            {loadingChat && <p role="status" className="text-small text-muted">Opening your conversation…</p>}
            {(conversation.isError || (resumeLatest && conversations.isError)) && (
              <p role="alert" className="text-small text-muted">
                Could not open this conversation. <button className="link-action" onClick={() => conversationId ? conversation.refetch() : conversations.refetch()}>Try again</button> or <button className="link-action" onClick={newChat}>start a new chat</button>.
              </p>
            )}
            {conversation.data?.documentName === null && (
              <p className="m-0 flex items-center gap-2 rounded-control border border-line bg-surface px-3.5 py-2.5 text-small text-muted">
                <Icon name="alert" className="text-base text-warning" />
                The document this conversation was about has been deleted. Its answers and sources
                are kept, but new questions have nothing left to search.
              </p>
            )}

            {isEmpty && (
              <div className="px-3 py-12 text-center animate-fade-up">
                <span className="inline-flex size-[58px] items-center justify-center rounded-panel border border-line bg-surface text-[26px] text-brand">
                  <Icon name={activeDocumentId ? "brand" : "fileText"} />
                </span>

                {activeDocumentId ? (
                  <>
                    <h2 className="mt-5 mb-2 text-[22px] font-bold tracking-[-0.02em]">
                      Ask about this document.
                    </h2>
                    <p className="mx-auto mb-6 max-w-[420px] text-lead leading-relaxed text-muted">
                      Every answer is written from passages of this document, with the pages it came
                      from. The conversation is saved as you go.
                    </p>
                    <div className="mx-auto grid max-w-[520px] gap-2.5 [grid-template-columns:repeat(auto-fit,minmax(220px,1fr))]">
                      {SUGGESTIONS.map((text) => (
                        <button
                          key={text}
                          onClick={() => send(text)}
                          className="cursor-pointer rounded-[11px] border border-line bg-surface px-4 py-3.5 text-left text-body transition-colors hover:border-brand-border hover:bg-brand-soft hover:text-brand"
                        >
                          {text}
                        </button>
                      ))}
                    </div>
                  </>
                ) : (
                  <>
                    <h2 className="mt-5 mb-2 text-[22px] font-bold tracking-[-0.02em]">
                      Choose a document to ask about.
                    </h2>
                    <p className="mx-auto mb-6 max-w-[440px] text-lead leading-relaxed text-muted">
                      Pick one below, open an earlier conversation, or choose “Chat with document”
                      from a document.
                    </p>
                    <Link href="/documents" className="btn btn-secondary btn-md">
                      <Icon name="fileText" className="text-base" />
                      Browse documents
                    </Link>
                  </>
                )}
              </div>
            )}

            {messages.map((message) =>
              message.role === "User" ? (
                <div key={message.id} className="animate-fade-up">
                  <UserMessage text={message.content} />
                </div>
              ) : (
                <div key={message.id} className="animate-fade-up">
                  <AssistantMessage
                    message={message}
                    onOpenSource={setOpenSource}
                    onRegenerate={() => send(lastQuestion)}
                  />
                </div>
              ),
            )}

            {pendingQuestion && (
              <div className="animate-fade-up">
                <UserMessage text={pendingQuestion} />
              </div>
            )}

            {streamingAnswer !== null ? (
              <div className="animate-fade-up">
                <StreamingMessage text={streamingAnswer} />
              </div>
            ) : (
              pending && <ThinkingIndicator />
            )}
          </div>
        </div>

        <ChatComposer
          value={draft}
          onChange={setDraft}
          onSubmit={() => send(draft)}
          onAttach={() => setUploadOpen(true)}
          onStop={stop}
          pending={pending}
          documents={ready}
          documentId={activeDocumentId ?? null}
          documentName={conversation.data?.documentName}
          locked={Boolean(conversationId)}
          onDocumentChange={(id) => {
            const next = new URLSearchParams(params.toString());
            next.delete("c");
            next.set("new", "1");
            next.set("documentId", id);
            router.replace(`${pathname}?${next}`);
          }}
        />
      </div>

      {openSource && (
        <>
          <SourceRail source={openSource} onClose={() => setOpenSource(null)} />
          <SourceSheet source={openSource} onClose={() => setOpenSource(null)} />
        </>
      )}

      <UploadDialog open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  );
}

export default function ChatPage() {
  return (
    <Suspense fallback={null}>
      <ChatScreen />
    </Suspense>
  );
}
