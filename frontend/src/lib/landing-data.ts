import type { IconName } from "@/components/ui/Icon";

/* Content for the marketing page, kept apart from product data. */

export const LANDING_NAV = [
  { label: "Product", href: "#product" },
  { label: "How it Works", href: "#how" },
  { label: "Features", href: "#features" },
  { label: "Pricing", href: "#features" },
  { label: "FAQ", href: "#faq" },
];

export const HERO_STATS = [
  { value: "20MB", label: "Per document" },
  { value: "3 formats", label: "PDF, DOCX, TXT" },
  { value: "Every answer", label: "Cited to a page" },
];

export const HERO_CHIPS: { label: string; icon: IconName }[] = [
  { label: "Semantic search", icon: "search" },
  { label: "Cited answers", icon: "quote" },
  { label: "Private by workspace", icon: "lock" },
];

export const HERO_TURN = {
  question: "How many annual leave days do employees receive?",
  answer: "Employees receive 20 annual leave days per year.",
  sources: [
    { doc: "Employee Handbook", page: "Page 12" },
    { doc: "Leave Policy 2026", page: "Page 4" },
  ],
};

export const BRANDS = ["Acme", "Vertex", "Nova", "Atlas", "Northstar"];

export const PROBLEMS: { icon: IconName; title: string; body: string }[] = [
  { icon: "database", title: "Too much information", body: "Knowledge is buried inside PDFs and internal files." },
  { icon: "search", title: "Slow searching", body: "Keyword search fails when you don't know the wording." },
  { icon: "clock", title: "Lost context", body: "One answer means reading several documents." },
];

export const STEPS: { num: string; title: string; body: string; icon: IconName }[] = [
  { num: "01 — UPLOAD", title: "Upload", body: "PDF, DOCX, and TXT files up to 20MB.", icon: "upload" },
  { num: "02 — UNDERSTAND", title: "Understand", body: "Documents are read and indexed by meaning.", icon: "sparkles" },
  { num: "03 — ASK", title: "Ask", body: "Answers arrive with the page they came from.", icon: "message" },
];

export const PIPELINE: { icon: IconName; label: string }[] = [
  { icon: "fileText", label: "Document" },
  { icon: "sparkles", label: "AI Processing" },
  { icon: "database", label: "Knowledge Base" },
  { icon: "message", label: "Answer" },
];

export const FEATURES: { icon: IconName; title: string; body: string }[] = [
  { icon: "search", title: "AI-Powered Search", body: "Search by meaning, not exact keywords." },
  { icon: "sparkles", title: "Grounded Answers", body: "Every answer comes from your own documents." },
  { icon: "quote", title: "Source Citations", body: "See the document and page behind each answer." },
  { icon: "layersAlt", title: "Multiple Documents", body: "Search your entire knowledge base at once." },
  { icon: "message", title: "Conversation Memory", body: "Follow up without repeating context." },
  { icon: "shield", title: "Secure Knowledge", body: "Documents stay isolated to your workspace." },
  { icon: "zap", title: "Fast Responses", body: "Answers stream in as they are written." },
  { icon: "fileText", title: "Smart Processing", body: "Text is extracted and indexed automatically." },
];

export const FAQS = [
  ["What types of documents can I upload?", "PDF, DOCX, and TXT files up to 20MB each. Scanned PDFs with a text layer work too."],
  ["How does DocuMind AI find answers?", "Your documents are split into passages and indexed by meaning. When you ask a question, the closest passages are retrieved and used to write the answer."],
  ["What is RAG?", "Retrieval-Augmented Generation. The assistant retrieves relevant passages from your own documents before answering, instead of relying on general knowledge."],
  ["Does DocuMind AI provide sources?", "Yes. Every answer lists the documents and pages it drew from, and you can open the exact passage."],
  ["Are my documents private?", "Documents live in your workspace and are retrieved only for people you invite to it."],
  ["Can I upload multiple documents?", "Yes. Questions are answered across your whole knowledge base, or you can scope them to a collection."],
  ["Can I delete my documents?", "Yes. Deleting a document also removes its indexed passages from retrieval."],
  ["Does DocuMind AI remember conversations?", "Within a conversation, yes. You can ask follow-up questions without repeating context."],
] as const;

export const FOOTER_LINKS = [
  { title: "Product", links: [["Features", "#features"], ["How It Works", "#how"], ["Pricing", "#features"], ["FAQ", "#faq"]] },
  { title: "Company", links: [["About", "#product"], ["Contact", "#faq"]] },
  { title: "Legal", links: [["Privacy", "#faq"], ["Terms", "#faq"]] },
] as const;

export const SOCIALS: { label: string; icon: IconName }[] = [
  { label: "Email", icon: "mail" },
  { label: "Website", icon: "globe" },
  { label: "Slack", icon: "slack" },
];

export const TESTIMONIAL = {
  quote: "“We stopped forwarding the handbook around. People just ask.”",
  author: "Operations lead, Northstar",
};
