import { Tile } from "@/components/ui/Tile";
import type { DocumentStatus } from "@/lib/data";

/** File glyph whose tone reflects the document's processing state. */
export function DocumentTile({
  status,
  className,
}: {
  status: DocumentStatus;
  className?: string;
}) {
  return <Tile icon="fileText" tone={status === "failed" ? "danger" : "neutral"} className={className} />;
}
