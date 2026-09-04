import type { IconName } from "@/components/ui/Icon";

/* ============================================================================
   Dummy data. Everything the UI renders comes from here, so swapping in the
   real API later means replacing this module — not touching components.
   ========================================================================= */

export type DocumentStatus = "ready" | "processing" | "failed";

export type Doc = {
  id: string;
  name: string;
  type: "PDF" | "DOCX" | "TXT";
  size: string;
  status: DocumentStatus;
  chunks: number;
  date: string;
};

export const CURRENT_USER = {
  name: "Hamza Ali",
  email: "hamza@northstar.co",
  initials: "HA",
};

export const DOCUMENTS: Doc[] = [
  { id: "handbook", name: "Employee Handbook.pdf", type: "PDF", size: "2.4 MB", status: "ready", chunks: 124, date: "Aug 31, 2026" },
  { id: "leave", name: "Leave Policy 2026.pdf", type: "PDF", size: "840 KB", status: "ready", chunks: 48, date: "Aug 28, 2026" },
  { id: "benefits", name: "Benefits Overview.docx", type: "DOCX", size: "1.1 MB", status: "ready", chunks: 62, date: "Aug 24, 2026" },
  { id: "remote", name: "Remote Work Guidelines.pdf", type: "PDF", size: "620 KB", status: "processing", chunks: 0, date: "Sep 1, 2026" },
  { id: "conduct", name: "Code of Conduct.txt", type: "TXT", size: "96 KB", status: "ready", chunks: 31, date: "Aug 12, 2026" },
  { id: "payroll", name: "Payroll Schedule.pdf", type: "PDF", size: "310 KB", status: "failed", chunks: 0, date: "Sep 2, 2026" },
];

export const STATUS_LABELS: Record<DocumentStatus, string> = {
  ready: "Ready",
  processing: "Processing",
  failed: "Failed",
};

export const DASHBOARD_STATS = [
  { label: "Documents", value: "24" },
  { label: "Conversations", value: "128" },
  { label: "Knowledge Chunks", value: "4,820" },
  { label: "Questions Asked", value: "356" },
];

export const RECENT_CONVERSATIONS = [
  { id: "leave", title: "Employee leave policy", when: "2h ago" },
  { id: "q3", title: "Q3 financial summary", when: "Yesterday" },
  { id: "eng", title: "Engineering guidelines", when: "2d ago" },
  { id: "refund", title: "Customer refund policy", when: "5d ago" },
];

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

export type Source = {
  doc: string;
  page: string;
  before: string;
  quote: string;
  after: string;
};

export const SOURCES: Source[] = [
  {
    doc: "Employee Handbook",
    page: "Page 12",
    before: "4.1 Annual leave. Leave is administered by the People team and tracked in the HR system.",
    quote: "“…employees are entitled to 20 annual leave days per year, accrued monthly from the start date…”",
    after: "Requests should be submitted at least two weeks in advance where possible.",
  },
  {
    doc: "Leave Policy 2026",
    page: "Page 4",
    before: "Section 2 — Accrual and carry-over.",
    quote: "“…unused annual leave carries over for one quarter into the following year and expires thereafter…”",
    after: "Managers are notified of expiring balances one month in advance.",
  },
  {
    doc: "Benefits Overview",
    page: "Page 7",
    before: "Benefits are reviewed annually each January.",
    quote: "“…private health insurance begins on day one, with pension contributions matched up to 5%…”",
    after: "A learning budget of $1,200 per employee is available on request.",
  },
];

/** Canned answers keyed loosely off the question, so the demo chat feels real. */
export const ANSWERS = {
  leave: {
    text: "Employees are entitled to 20 annual leave days per year, accrued monthly from their start date. Unused days carry over for one quarter into the following year. Sick leave is granted separately, up to 10 paid days per year.",
    sources: [0, 1],
  },
  benefits: {
    text: "The benefits package covers private health insurance from day one, a matched pension contribution of up to 5%, and an annual learning budget of $1,200 per employee.",
    sources: [1, 2],
  },
  summary: {
    text: "Your knowledge base covers HR policy end to end: the Employee Handbook sets working hours, conduct, and leave; the Leave Policy details accrual and carry-over rules; and the Benefits Overview lists health, pension, and learning entitlements.",
    sources: [0, 2],
  },
} as const;

export function answerFor(question: string) {
  const q = question.toLowerCase();
  if (q.includes("benefit")) return ANSWERS.benefits;
  if (q.includes("summar") || q.includes("compare")) return ANSWERS.summary;
  return ANSWERS.leave;
}

export const CONVERSATION_GROUPS = [
  { label: "Today", items: [{ id: "leave", title: "Leave policy question" }, { id: "eng", title: "Engineering guidelines" }] },
  { label: "Yesterday", items: [{ id: "benefits", title: "Benefits overview" }, { id: "refund", title: "Refund policy" }] },
  { label: "Older", items: [{ id: "onboarding", title: "Employee onboarding" }] },
];

export const THINKING_STEPS = [
  "Searching your knowledge…",
  "Finding relevant documents…",
  "Reviewing sources…",
  "Generating answer…",
];

export const UPLOAD_STEPS = [
  "Uploading",
  "Reading document",
  "Extracting text",
  "Creating knowledge chunks",
  "Generating embeddings",
  "Indexing knowledge",
  "Ready",
];

export const DOCUMENT_ACTIONS: { label: string; icon: IconName }[] = [
  { label: "Summarize", icon: "sparkles" },
  { label: "Extract key points", icon: "layers" },
  { label: "Find important sections", icon: "search" },
];

export const DOCUMENT_PREVIEW = [
  { heading: true, text: "Section 4 — Leave and Time Off" },
  { heading: false, text: "4.1 Annual leave. Employees are entitled to 20 annual leave days per year, accrued monthly from the start date. Unused days carry over for one quarter into the following year." },
  { heading: false, text: "4.2 Sick leave. Up to 10 paid sick days are available each year. A medical note is required for absences longer than three consecutive days." },
  { heading: false, text: "4.3 Personal leave. Two personal days may be taken each year with a manager's approval, and do not carry over." },
];

export const PREFERENCE_TOGGLES = [
  { label: "Always show sources with answers", on: true },
  { label: "Prefer concise answers", on: false },
  { label: "Stream responses as they are generated", on: true },
];

export const SESSIONS = [
  { device: "MacBook Pro — Berlin", meta: "Current session · Chrome", action: "This device", current: true },
  { device: "iPhone 15 — Berlin", meta: "Last active 3 hours ago", action: "Revoke", current: false },
];
