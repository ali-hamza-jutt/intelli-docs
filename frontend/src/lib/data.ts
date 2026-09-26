import type { IconName } from "@/components/ui/Icon";

/* ============================================================================
   Copy, not data.

   What is left here is written text that happens to live in an array: the
   suggested questions and the document action buttons. Every screen now reads
   its actual content from the API.
   ========================================================================= */

export const SUGGESTIONS = [
  "Summarize my documents",
  "What is our leave policy?",
  "Find information about employee benefits",
  "Compare these two policies",
];

export const DOCUMENT_ACTIONS: { label: string; icon: IconName }[] = [
  { label: "Summarize", icon: "sparkles" },
  { label: "Extract key points", icon: "layers" },
  { label: "Find important sections", icon: "search" },
];
