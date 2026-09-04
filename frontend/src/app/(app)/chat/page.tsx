"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Icon } from "@/components/ui/Icon";
import {
  AssistantMessage,
  ThinkingIndicator,
  UserMessage,
  type ChatTurn,
} from "@/components/chat/ChatMessage";
import { ChatComposer } from "@/components/chat/ChatComposer";
import { ConversationPane } from "@/components/chat/ConversationPane";
import { SourceRail, SourceSheet } from "@/components/chat/SourcePanel";
import { UploadDialog } from "@/components/documents/UploadDialog";
import { SOURCES, SUGGESTIONS, THINKING_STEPS, answerFor } from "@/lib/data";

const THINK_MS = 480;
const STREAM_MS = 55;
const WORDS_PER_TICK = 2;

function ChatScreen() {
  // A question handed over from the dashboard arrives as ?q= and seeds the
  // first turn directly, so no effect is needed to kick the conversation off.
  const initialQuestion = useSearchParams().get("q")?.trim() ?? "";

  const [turns, setTurns] = useState<ChatTurn[]>(() =>
    initialQuestion ? [{ id: "u0", role: "user", text: initialQuestion, done: true }] : [],
  );
  const [thinkingStep, setThinkingStep] = useState<number | null>(initialQuestion ? 0 : null);
  const [draft, setDraft] = useState("");
  const [streaming, setStreaming] = useState(false);
  const [activeConvo, setActiveConvo] = useState("leave");
  const [openSource, setOpenSource] = useState<number | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);

  const pendingQuestion = useRef<string>(initialQuestion);
  const streamTimer = useRef<ReturnType<typeof setInterval> | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  const send = useCallback(
    (text: string) => {
      const question = text.trim();
      if (!question || streaming) return;

      setTurns((prev) => [
        ...prev,
        { id: `u${Date.now()}`, role: "user", text: question, done: true },
      ]);
      setDraft("");
      setOpenSource(null);
      pendingQuestion.current = question;
      setThinkingStep(0);
    },
    [streaming],
  );

  // Walk the retrieval steps, then hand off to streaming. Both transitions happen
  // inside the timeout callback rather than the effect body.
  useEffect(() => {
    if (thinkingStep === null) return;

    const timer = setTimeout(() => {
      if (thinkingStep >= THINKING_STEPS.length - 1) {
        const answer = answerFor(pendingQuestion.current);
        setThinkingStep(null);
        setTurns((prev) => [
          ...prev,
          {
            id: `a${Date.now()}`,
            role: "assistant",
            text: "",
            sources: [...answer.sources],
            done: false,
          },
        ]);
        setStreaming(true);
      } else {
        setThinkingStep((step) => (step ?? 0) + 1);
      }
    }, THINK_MS);

    return () => clearTimeout(timer);
  }, [thinkingStep]);

  // Reveal the answer a couple of words at a time.
  useEffect(() => {
    if (!streaming) return;

    const words = answerFor(pendingQuestion.current).text.split(" ");
    let shown = 0;

    const timer = setInterval(() => {
      shown += WORDS_PER_TICK;
      const done = shown >= words.length;

      setTurns((prev) => {
        const next = [...prev];
        const last = next.length - 1;
        next[last] = { ...next[last], text: words.slice(0, shown).join(" "), done };
        return next;
      });

      if (done) {
        clearInterval(timer);
        setStreaming(false);
      }
    }, STREAM_MS);

    streamTimer.current = timer;
    return () => clearInterval(timer);
  }, [streaming]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [turns, thinkingStep]);

  const stop = () => {
    if (streamTimer.current) clearInterval(streamTimer.current);
    setStreaming(false);
    setTurns((prev) => {
      const next = [...prev];
      if (next.length) next[next.length - 1] = { ...next[next.length - 1], done: true };
      return next;
    });
  };

  const lastQuestion = [...turns].reverse().find((t) => t.role === "user")?.text ?? "";
  const isEmpty = turns.length === 0 && thinkingStep === null;

  return (
    <div className="flex h-[calc(100vh-140px)] overflow-hidden md:h-[calc(100vh-64px)]">
      <ConversationPane
        activeId={activeConvo}
        onSelect={(id) => {
          setActiveConvo(id);
          send(id === "benefits" ? "What benefits do we offer?" : "What is our annual leave policy?");
        }}
        onNewChat={() => {
          setTurns([]);
          setOpenSource(null);
        }}
      />

      <div className="flex min-w-[320px] flex-1 flex-col bg-canvas">
        <div ref={scrollRef} className="flex-1 overflow-auto px-5 py-6">
          <div className="mx-auto flex max-w-[720px] flex-col gap-5">
            {isEmpty && (
              <div className="px-3 py-12 text-center animate-fade-up">
                <span className="inline-flex size-[58px] items-center justify-center rounded-panel border border-line bg-surface text-[26px] text-brand">
                  <Icon name="brand" />
                </span>
                <h2 className="mt-5 mb-2 text-[22px] font-bold tracking-[-0.02em]">
                  Ask your knowledge anything.
                </h2>
                <p className="mx-auto mb-6 max-w-[420px] text-lead leading-relaxed text-muted">
                  Ask questions about your documents and get answers grounded in your knowledge base.
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
              </div>
            )}

            {turns.map((turn) => (
              <div key={turn.id} className="animate-fade-up">
                {turn.role === "user" ? (
                  <UserMessage text={turn.text} />
                ) : (
                  <AssistantMessage
                    turn={turn}
                    onOpenSource={setOpenSource}
                    onRegenerate={() => send(lastQuestion)}
                  />
                )}
              </div>
            ))}

            {thinkingStep !== null && <ThinkingIndicator step={thinkingStep} />}
          </div>
        </div>

        <ChatComposer
          value={draft}
          onChange={setDraft}
          onSubmit={() => send(draft)}
          onAttach={() => setUploadOpen(true)}
          onStop={stop}
          streaming={streaming}
        />
      </div>

      {openSource !== null && (
        <>
          <SourceRail source={SOURCES[openSource]} onClose={() => setOpenSource(null)} />
          <SourceSheet source={SOURCES[openSource]} onClose={() => setOpenSource(null)} />
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
