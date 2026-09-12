/**
 * Formatting helpers shared by the document views.
 *
 * `fileSize` arrives as `number | string` because .NET serialises Int64 that way in OpenAPI —
 * these helpers absorb that so components never deal with it.
 */

const UNITS = ["B", "KB", "MB", "GB"] as const;

export function formatBytes(bytes: number | string): string {
  const value = typeof bytes === "string" ? Number(bytes) : bytes;

  if (!Number.isFinite(value) || value <= 0) return "0 B";

  const exponent = Math.min(Math.floor(Math.log(value) / Math.log(1024)), UNITS.length - 1);
  const scaled = value / 1024 ** exponent;

  // Whole numbers for bytes, one decimal above that.
  return `${exponent === 0 ? scaled : scaled.toFixed(1)} ${UNITS[exponent]}`;
}

/** "Aug 31, 2026" — matches the dates in the original design. */
export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

/** The file extension, upper-cased, for the type column: "PDF". */
export function fileTypeOf(fileName: string): string {
  const extension = fileName.split(".").pop();
  return extension ? extension.toUpperCase() : "FILE";
}
