import type { IconName } from "@/components/ui/Icon";

/* ============================================================================
   What is left of the placeholder data.

   Collections is the one screen with no API behind it. The rest here is static
   by nature: the suggested questions and the document action list are copy, not
   data. Documents, conversations, answers, settings and the signed-in user all
   come from the backend.
   ========================================================================= */

export const SUGGESTIONS = [
  "Summarize my documents",
  "What is our leave policy?",
  "Find information about employee benefits",
  "Compare these two policies",
];

export type Collection = {
  id: string;
  name: string;
  count: number;
  updated: string;
  featured: boolean;
};

export const COLLECTIONS: Collection[] = [
  { id: "hr", name: "HR", count: 12, updated: "today", featured: true },
  { id: "engineering", name: "Engineering", count: 5, updated: "2 days ago", featured: false },
  { id: "finance", name: "Finance", count: 4, updated: "last week", featured: false },
  { id: "legal", name: "Legal", count: 2, updated: "Aug 12", featured: false },
  { id: "product", name: "Product", count: 1, updated: "Jul 30", featured: false },
];

export const DOCUMENT_ACTIONS: { label: string; icon: IconName }[] = [
  { label: "Summarize", icon: "sparkles" },
  { label: "Extract key points", icon: "layers" },
  { label: "Find important sections", icon: "search" },
];



