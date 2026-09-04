"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Card, ListCard } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { DocumentListItem } from "@/components/documents/DocumentListItem";
import {
  CURRENT_USER,
  DASHBOARD_STATS,
  DOCUMENTS,
  RECENT_CONVERSATIONS,
  SUGGESTIONS,
} from "@/lib/data";

export default function DashboardPage() {
  const [question, setQuestion] = useState("");
  const router = useRouter();

  const ask = (text: string) => {
    if (!text.trim()) return;
    router.push(`/chat?q=${encodeURIComponent(text.trim())}`);
  };

  return (
    <div className="page">
      <h2 className="m-0 text-3xl font-bold tracking-[-0.025em]">
        Good morning, {CURRENT_USER.name.split(" ")[0]}
      </h2>
      <p className="mt-1.5 mb-6.5 text-lead text-muted">
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
        {DASHBOARD_STATS.map((stat) => (
          <div key={stat.label} className="card rounded-xl px-[18px] py-4">
            <p className="m-0 text-caption font-medium text-muted">{stat.label}</p>
            <p className="mt-1.5 text-3xl font-bold tracking-[-0.02em]">{stat.value}</p>
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
          {DOCUMENTS.slice(0, 4).map((doc) => (
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
    </div>
  );
}
