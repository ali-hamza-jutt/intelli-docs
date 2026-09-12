"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Card, ListCard } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Skeleton, SkeletonRows } from "@/components/ui/Skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { DocumentListItem } from "@/components/documents/DocumentListItem";
import { UploadDialog } from "@/components/documents/UploadDialog";
import { useGetApiDocuments } from "@/lib/api/generated/documents/documents";
import { isInProgress, shouldPoll } from "@/lib/documentStatus";
import { useAuth } from "@/lib/auth/AuthProvider";
import { RECENT_CONVERSATIONS, SUGGESTIONS } from "@/lib/data";

const POLL_MS = 3000;

export default function DashboardPage() {
  const [question, setQuestion] = useState("");
  const [uploadOpen, setUploadOpen] = useState(false);
  const router = useRouter();
  const { user } = useAuth();

  const documents = useGetApiDocuments({
    query: {
      refetchInterval: (query) =>
        query.state.data?.some(shouldPoll) ? POLL_MS : false,
    },
  });

  // Counts are derived from the list rather than a separate endpoint — at this scale one
  // request is cheaper than two, and the numbers can never disagree with the list below.
  const stats = useMemo(() => {
    const docs = documents.data ?? [];

    return [
      { label: "Documents", value: docs.length },
      { label: "Processed", value: docs.filter((d) => d.status === "Completed").length },
      { label: "Processing", value: docs.filter((d) => isInProgress(d.status)).length },
      { label: "Failed", value: docs.filter((d) => d.status === "Failed").length },
    ];
  }, [documents.data]);

  const recent = (documents.data ?? []).slice(0, 4);
  const firstName = user?.name.split(" ")[0] ?? "there";

  const ask = (text: string) => {
    if (!text.trim()) return;
    router.push(`/chat?q=${encodeURIComponent(text.trim())}`);
  };

  return (
    <div className="page">
      <h2 className="m-0 text-3xl font-bold tracking-[-0.025em]">Good morning, {firstName}</h2>
      <p className="mt-1.5 mb-6 text-lead text-muted">
        Here&apos;s what&apos;s happening with your knowledge base.
      </p>

      {/* Ask box */}
      <Card className="p-[22px] shadow-card">
        <div className="mb-3.5 flex items-center gap-2.5">
          <Icon name="sparkles" className="text-[17px] text-brand" />
          <h3 className="card-title">Ask your knowledge base</h3>
        </div>

        <form
          className="flex flex-wrap gap-2.5"
          onSubmit={(e) => {
            e.preventDefault();
            ask(question);
          }}
        >
          <input
            value={question}
            onChange={(e) => setQuestion(e.target.value)}
            aria-label="Ask anything about your documents"
            placeholder="Ask anything about your documents…"
            className="field min-w-[220px] flex-1 px-[15px] py-[13px] text-lead"
          />
          <Button type="submit" size="lg" trailingIcon="arrowRight" className="px-5 py-[13px]">
            Ask AI
          </Button>
        </form>

        <div className="mt-3.5 flex flex-wrap gap-2">
          {SUGGESTIONS.map((text) => (
            <button
              key={text}
              onClick={() => ask(text)}
              className="cursor-pointer rounded-control border border-line bg-canvas px-3 py-1.5 text-caption text-muted transition-colors hover:border-brand-border hover:bg-brand-soft hover:text-brand"
            >
              {text}
            </button>
          ))}
        </div>
      </Card>

      {/* Stats */}
      <div className="grid-fit-sm mt-5">
        {stats.map((stat) => (
          <div key={stat.label} className="card rounded-xl px-[18px] py-4">
            <p className="m-0 text-caption font-medium text-muted">{stat.label}</p>
            {documents.isPending ? (
              <Skeleton className="mt-2.5 h-6 w-16" />
            ) : (
              <p className="mt-1.5 text-3xl font-bold tracking-[-0.02em] tabular-nums">
                {stat.value}
              </p>
            )}
          </div>
        ))}
      </div>

      {/* Recent activity */}
      <div className="grid-fit-lg mt-5 items-start">
        <ListCard
          title="Recent documents"
          action={
            <Link href="/documents" className="link-action">
              View all
            </Link>
          }
        >
          {documents.isPending && (
            <div className="p-[18px]">
              <SkeletonRows count={4} />
            </div>
          )}

          {documents.isSuccess && recent.length === 0 && (
            <EmptyState
              icon="fileText"
              title="No documents yet"
              body="Upload a PDF to start building your knowledge base."
              action={<Button onClick={() => setUploadOpen(true)}>Upload Document</Button>}
            />
          )}

          {recent.map((doc) => (
            <DocumentListItem key={doc.id} doc={doc} />
          ))}
        </ListCard>

        <ListCard
          title="Recent conversations"
          action={
            <Link href="/chat" className="link-action">
              Open chat
            </Link>
          }
        >
          {RECENT_CONVERSATIONS.map((convo) => (
            <Link
              key={convo.id}
              href="/chat"
              className="flex w-full items-center gap-3 border-b border-line-soft px-[18px] py-3.5 text-left transition-colors last:border-b-0 hover:bg-canvas"
            >
              <Icon name="message" className="text-md text-subtle" />
              <span className="flex-1 text-body font-medium">{convo.title}</span>
              <span className="text-tiny text-subtle">{convo.when}</span>
            </Link>
          ))}
        </ListCard>
      </div>

      <UploadDialog open={uploadOpen} onClose={() => setUploadOpen(false)} />
    </div>
  );
}
