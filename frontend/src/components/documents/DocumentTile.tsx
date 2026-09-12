import { Tile } from "@/components/ui/Tile";

/** File glyph whose tone reflects the document's processing state. */
export function DocumentTile({
  status,
  className,
}: {
  status: string;
  className?: string;
}) {
  return (
    <Tile icon="fileText" tone={status === "Failed" ? "danger" : "neutral"} className={className} />
  );
}
