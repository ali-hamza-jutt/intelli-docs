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
import type { MessageSourceResponse } from "@/lib/api/model";

function ChatScreen() {
  const params = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const toast = useToast();
  const queryClient = useQueryClient();

  // Which thread is open, or which document a new one would be about. Both live in the URL, so a
  // reload — the thing this module exists to survive — lands back in the same place.
  const conversationId = params.get("c");
  const documentId = params.get("documentId");

  const [draft, setDraft] = useState("");
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
  const streamRef = useRef<AbortController | null>(null);

  const conversations = useGetApiConversations();
  const documents = useGetApiDocuments();
  const ready = (documents.data ?? []).filter((document) => document.status === "Completed");

  const conversation = useGetApiConversationsId(conversationId ?? "", {
    query: { enabled: Boolean(conversationId) },
  });

  const start = usePostApiConversations();
  const remove = useDeleteApiConversationsId();

  // Pending covers the whole exchange: creating the thread, the wait before the first word, and the
  // answer still arriving.
  const pending = start.isPending || pendingQuestion !== null;
  const messages = conversation.data?.messages ?? [];
  const activeDocumentId = conversation.data?.documentId ?? documentId;

  const openConversation = useCallback(
    (id: string) => {
      setOpenSource(null);
      setPendingQuestion(null);
      setStreamingAnswer(null);
      router.replace(`${pathname}?c=${id}`);
    },
    [pathname, router],
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

      if (!question || pending) return;

      if (!conversationId && !documentId) {
        toast("Choose a document to ask about first", "warn");
        return;
      }

      setDraft("");
      setOpenSource(null);
      setPendingQuestion(question);
      setStreamingAnswer(null);

      const controller = new AbortController();
      streamRef.current = controller;

      try {
        // A thread has to exist before an answer can be streamed into it, so the first question
        // creates an empty one. It is created without a question so that this answer streams too,
        // rather than the first one arriving all at once.
        const threadId =
          conversationId ?? (await start.mutateAsync({ data: { documentId: documentId! } })).id;

        if (!conversationId) {
          router.replace(`${pathname}?c=${threadId}`);
        }

        await streamAnswer({
          conversationId: threadId,
          question,
          signal: controller.signal,
          onDelta: (piece) => setStreamingAnswer((sofar) => (sofar ?? "") + piece),
          onFinal: () => undefined,
        });

        // The stored thread is the source of truth; what was rendered while streaming is dropped
        // in favour of it, sources and all.
        setPendingQuestion(null);
        setStreamingAnswer(null);
        refreshThread(threadId);
      } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError") {
          // Stopped on purpose. The server keeps what it had written, so the thread is reloaded
          // rather than the partial text being left on screen unsaved.
          setPendingQuestion(null);
          setStreamingAnswer(null);

          if (conversationId) refreshThread(conversationId);

          return;
        }

        failed(error, question);
        setStreamingAnswer(null);
      } finally {
        streamRef.current = null;
      }
    },
    [conversationId, documentId, failed, pathname, pending, refreshThread, router, start, toast],
  );

  const stop = useCallback(() => {
    streamRef.current?.abort();
  }, []);

  const deleteConversation = useCallback(
    (id: string) => {
      remove.mutate(
        { id },
        {
          onSuccess: () => {
            toast("Conversation deleted");
            queryClient.invalidateQueries({ queryKey: getGetApiConversationsQueryKey() });

            if (id === conversationId) {
              router.replace(pathname);
            }
          },
        },
      );
    },
    [conversationId, pathname, queryClient, remove, router, toast],
  );

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages.length, pendingQuestion]);

  const lastQuestion =
    [...messages].reverse().find((message) => message.role === "User")?.content ?? "";
  const isEmpty = messages.length === 0 && pendingQuestion === null && !conversation.isPending;

  return (
    <div className="flex h-[calc(100vh-140px)] overflow-hidden md:h-[calc(100vh-64px)]">
      <ConversationPane
        conversations={conversations.data ?? []}
        loading={conversations.isPending}
        activeId={conversationId}
        onSelect={openConversation}
        onDelete={deleteConversation}
        onNewChat={() => {
          setOpenSource(null);
          setPendingQuestion(null);
          router.replace(pathname);
        }}
      />

      <div className="flex min-w-[320px] flex-1 flex-col bg-canvas">
        <div ref={scrollRef} className="flex-1 overflow-auto px-5 py-6">
          <div className="mx-auto flex max-w-[720px] flex-col gap-5">
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
          onDocumentChange={(id) => router.replace(`${pathname}?documentId=${id}`)}
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
