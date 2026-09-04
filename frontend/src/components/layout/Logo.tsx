import Link from "next/link";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

const MARK_SIZES = {
  sm: "size-7 rounded-control text-lg",
  md: "size-8 rounded-field text-[18px]",
};

/** The dark rounded brand mark, optionally followed by the wordmark. */
export function Logo({
  href = "/",
  showWord = true,
  size = "md",
  className,
}: {
  href?: string;
  showWord?: boolean;
  size?: keyof typeof MARK_SIZES;
  className?: string;
}) {
  return (
    <Link href={href} aria-label="DocuMind AI home" className={cn("flex items-center gap-2.5", className)}>
      <LogoMark size={size} />
      {showWord && <span className="text-lg font-bold tracking-[-0.01em]">DocuMind AI</span>}
    </Link>
  );
}

export function LogoMark({ size = "md" }: { size?: keyof typeof MARK_SIZES }) {
  return (
    <span
      className={cn(
        "inline-flex flex-none items-center justify-center bg-inverse text-white",
        MARK_SIZES[size],
      )}
    >
      <Icon name="brand" />
    </span>
  );
}
