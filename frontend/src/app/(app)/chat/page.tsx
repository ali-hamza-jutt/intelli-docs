"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Icon } from "@/components/ui/Icon";
import {
  AssistantMessage,
  ThinkingIndicator,
  UserMessage,
  type ChatTurn,
} from "@/components/chat/ChatMessage";
import { ChatComposer } from "@/components/chat/ChatComposer";
import { DocumentPicker } from "@/components/chat/DocumentPicker";
import { SourceRail, SourceSheet } from "@/components/chat/SourcePanel";
import { UploadDialog } from "@/components/documents/UploadDialog";
import {
  useGetApiDocuments,
  usePostApiDocumentsIdChat,
} from "@/lib/api/generated/documents/documents";
import { ApiError } from "@/lib/api/client";
import { SUGGESTIONS } from "@/lib/data";
import { useToast } from "@/components/ui/Toast";
import type { ChatCitationResponse } from "@/lib/api/model";

function ChatScreen() {
  const params = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();
  const toast = useToast();

  // Both the document and an opening question can arrive in the URL — from "Chat with document"
  // on a document, or from a suggestion on the dashboard — which keeps a conversation shareable.
  const documentId = params.get("documentId");
  const initialQuestion = params.get("q")?.trim() ?? "";

  const [turns, setTurns] = useState<ChatTurn[]>([]);
  const [draft, setDraft] = useState(initialQuestion);
  const [openCitation, setOpenCitation] = useState<ChatCitationResponse | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);
  const chat = usePostApiDocumentsIdChat();

  // Only a finished document can be asked about: one still processing has no passages to search.
  const documents = useGetApiDocuments();
  const ready = (documents.data ?? []).filter((document) => document.status === "Completed");

  const selectDocument = useCallback(
    (id: string) => {
      setTurns([]);
      setOpenCitation(null);
      router.replace(`${pathname}?documentId=${id}`);
    },
    [pathname, router],
  );

  const send = useCallback(
    (text: string) => {
      const question = text.trim();

      if (!question || chat.isPending) return;

      if (!documentId) {
        toast("Choose a document to ask about first", "warn");
        return;
      }

      setTurns((previous) => [
        ...previous,
        { id: `u${Date.now()}`, role: "user", text: question },
      ]);
      setDraft("");
      setOpenCitation(null);

      chat.mutate(
        { id: documentId, data: { question } },
        {
          onSuccess: (answer) =>
            setTurns((previous) => [
              ...previous,
              {
                id: `a${Date.now()}`,
                role: "assistant",
                text: answer.answer,
                citations: answer.citations,
                grounded: answer.grounded,
              },
            ]),
          onError: (error) => {
            // The turn that failed is dropped rather than left hanging, so "Ask again" is the
            // question itself rather than a retry of a half-finished exchange.
            setTurns((previous) => previous.slice(0, -1));
            setDraft(question);
            toast(
              error instanceof ApiError ? error.message : "Could not answer that question",
              "warn",
            );
          },
        },
      );
    },
    [chat, documentId, toast],
  );

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [turns, chat.isPending]);

  const lastQuestion = [...turns].reverse().find((turn) => turn.role === "user")?.text ?? "";

  return (
    <div className="flex h-[calc(100vh-140px)] overflow-hidden md:h-[calc(100vh-64px)]">
      <DocumentPicker
        documents={ready}
        loading={documents.isPending}
        activeId={documentId}
        onSelect={selectDocument}
        onNewChat={() => {
          setTurns([]);
          setOpenCitation(null);
        }}
      />

      <div className="flex min-w-[320px] flex-1 flex-col bg-canvas">
        <div ref={scrollRef} className="flex-1 overflow-auto px-5 py-6">
          <div className="mx-auto flex max-w-[720px] flex-col gap-5">
            {turns.length === 0 && !chat.isPending && (
              <div className="px-3 py-12 text-center animate-fade-up">
                <span className="inline-flex size-[58px] items-center justify-center rounded-panel border border-line bg-surface text-[26px] text-brand">
                  <Icon name={documentId ? "brand" : "fileText"} />
                </span>

                {documentId ? (
                  <>
                    <h2 className="mt-5 mb-2 text-[22px] font-bold tracking-[-0.02em]">
                      Ask about this document.
                    </h2>
                    <p className="mx-auto mb-6 max-w-[420px] text-lead leading-relaxed text-muted">
                      Every answer is written from passages of this document, with the pages it came
                      from.
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
                      Pick one from the list, or open a document and choose “Chat with document”.
                    </p>
                    <Link href="/documents" className="btn btn-secondary btn-md">
                      <Icon name="fileText" className="text-base" />
                      Browse documents
                    </Link>
                  </>
                )}
              </div>
            )}

            {turns.map((turn) => (
              <div key={turn.id} className="animate-fade-up">
                {turn.role === "user" ? (
                  <UserMessage text={turn.text} />
                ) : (
                  <AssistantMessage
                    turn={turn}
                    onOpenCitation={setOpenCitation}
                    onRegenerate={() => send(lastQuestion)}
                  />
                )}
              </div>
            ))}

            {chat.isPending && <ThinkingIndicator />}
          </div>
        </div>

        <ChatComposer
          value={draft}
          onChange={setDraft}
          onSubmit={() => send(draft)}
          onAttach={() => setUploadOpen(true)}
          pending={chat.isPending}
          documents={ready}
          documentId={documentId}
          onDocumentChange={selectDocument}
        />
      </div>

      {openCitation && (
        <>
          <SourceRail citation={openCitation} onClose={() => setOpenCitation(null)} />
          <SourceSheet citation={openCitation} onClose={() => setOpenCitation(null)} />
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
